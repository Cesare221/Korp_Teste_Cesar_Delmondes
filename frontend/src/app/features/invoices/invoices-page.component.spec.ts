import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { InvoiceService } from './invoice.service';
import { InvoicesPageComponent } from './invoices-page.component';

describe('InvoicesPageComponent', () => {
  let fixture: ComponentFixture<InvoicesPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoicesPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: InvoiceService,
          useValue: {
            getInvoices: () => of([{
              id: '22222222-2222-2222-2222-222222222222',
              number: 7,
              status: 'Open',
              createdAt: '2026-08-15T10:00:00Z',
              itemCount: 2
            }])
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoicesPageComponent);
    fixture.detectChanges();
  });

  it('renders loaded invoices with a view action', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Nota 7');
    expect(text).toContain('Aberta');
    expect(text).toContain('2');
    expect(text).toContain('Visualizar');
  });
});
