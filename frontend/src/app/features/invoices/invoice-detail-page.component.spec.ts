import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { Invoice } from '../../core/models/invoice.model';
import { InvoiceService } from './invoice.service';
import { InvoiceDetailPageComponent } from './invoice-detail-page.component';

describe('InvoiceDetailPageComponent', () => {
  const openInvoice: Invoice = {
    id: '22222222-2222-2222-2222-222222222222',
    number: 7,
    status: 'Open',
    createdAt: '2026-08-15T10:00:00Z',
    closedAt: null,
    items: [{
      id: '33333333-3333-3333-3333-333333333333',
      productId: '11111111-1111-1111-1111-111111111111',
      productCode: 'PROD-001',
      productDescription: 'Produto A',
      quantity: 2
    }]
  };

  const closedInvoice: Invoice = {
    ...openInvoice,
    status: 'Closed',
    closedAt: '2026-08-15T10:05:00Z'
  };

  let fixture: ComponentFixture<InvoiceDetailPageComponent>;
  let printInvoice: ReturnType<typeof vi.fn>;

  async function configure(invoice: Invoice, printResult: Observable<Invoice> = of(closedInvoice)): Promise<void> {
    printInvoice = vi.fn(() => printResult);

    await TestBed.configureTestingModule({
      imports: [InvoiceDetailPageComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: invoice.id }))
          }
        },
        {
          provide: InvoiceService,
          useValue: {
            getInvoice: () => of(invoice),
            printInvoice
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceDetailPageComponent);
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
  });

  it('renders invoice snapshot details', async () => {
    await configure(openInvoice);

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Nota Fiscal Nº 7');
    expect(text).toContain('Aberta');
    expect(text).toContain('PROD-001');
    expect(text).toContain('Produto A');
    expect(text).toContain('2');
  });

  it('shows print button for open invoice', async () => {
    await configure(openInvoice);

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Finalizar e imprimir');
  });

  it('does not show print button for closed invoice', async () => {
    await configure(closedInvoice);

    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('Finalizar e imprimir');
  });

  it('shows processing state while printing', async () => {
    const pending = new Subject<Invoice>();
    await configure(openInvoice, pending.asObservable());

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Processando nota fiscal...');
  });

  it('updates status and calls browser print after successful backend confirmation', async () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    await configure(openInvoice, of(closedInvoice));

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Fechada');
    expect(text).not.toContain('Finalizar e imprimir');
    expect(printSpy).toHaveBeenCalledOnce();
  });

  it('shows friendly message for insufficient stock', async () => {
    await configure(openInvoice, throwError(() => new HttpErrorResponse({ status: 409 })));

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Não há saldo suficiente para um ou mais produtos desta nota.');
  });

  it('shows friendly message when inventory service is unavailable', async () => {
    await configure(openInvoice, throwError(() => new HttpErrorResponse({ status: 503 })));

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Serviço de estoque temporariamente indisponível. Tente novamente.');
  });

  it('keeps invoice open and print button available without browser print after inventory service unavailable', async () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    await configure(openInvoice, throwError(() => new HttpErrorResponse({ status: 503 })));

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Aberta');
    expect(text).toContain('Finalizar e imprimir');
    expect(text).not.toContain('Processando nota fiscal...');
    expect(printSpy).not.toHaveBeenCalled();
  });

  it('allows manual retry after inventory service unavailable and prints only after retry succeeds', async () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    const firstFailure = throwError(() => new HttpErrorResponse({ status: 503 }));
    printInvoice = vi.fn()
      .mockReturnValueOnce(firstFailure)
      .mockReturnValueOnce(of(closedInvoice));

    await TestBed.configureTestingModule({
      imports: [InvoiceDetailPageComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: openInvoice.id }))
          }
        },
        {
          provide: InvoiceService,
          useValue: {
            getInvoice: () => of(openInvoice),
            printInvoice
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceDetailPageComponent);
    fixture.detectChanges();

    fixture.componentInstance.printInvoice();
    fixture.detectChanges();
    fixture.componentInstance.printInvoice();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Fechada');
    expect(text).not.toContain('Finalizar e imprimir');
    expect(printInvoice).toHaveBeenCalledTimes(2);
    expect(printSpy).toHaveBeenCalledOnce();
  });
});
