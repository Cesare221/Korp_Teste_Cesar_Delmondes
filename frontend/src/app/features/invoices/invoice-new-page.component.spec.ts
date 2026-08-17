import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProductService } from '../products/product.service';
import { InvoiceService } from './invoice.service';
import { InvoiceNewPageComponent } from './invoice-new-page.component';

describe('InvoiceNewPageComponent', () => {
  let fixture: ComponentFixture<InvoiceNewPageComponent>;
  let component: InvoiceNewPageComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceNewPageComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        {
          provide: ProductService,
          useValue: {
            getProducts: () => of([
              {
                id: '11111111-1111-1111-1111-111111111111',
                code: 'PROD-001',
                description: 'Produto A',
                balance: 10,
                createdAt: '2026-08-15T10:00:00Z',
                updatedAt: '2026-08-15T10:00:00Z'
              },
              {
                id: '22222222-2222-2222-2222-222222222222',
                code: 'PROD-002',
                description: 'Produto B',
                balance: 20,
                createdAt: '2026-08-15T10:00:00Z',
                updatedAt: '2026-08-15T10:00:00Z'
              }
            ])
          }
        },
        {
          provide: InvoiceService,
          useValue: {
            createInvoice: vi.fn()
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceNewPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('is invalid without items', () => {
    component.removeItem(0);

    expect(component.items.length).toBe(0);
    expect(component.form.valid).toBe(false);
  });

  it('requires a product', () => {
    component.items.at(0).patchValue({ productId: '', quantity: 1 });

    expect(component.items.at(0).get('productId')?.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('rejects zero quantity', () => {
    component.items.at(0).patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 0
    });

    expect(component.items.at(0).get('quantity')?.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('rejects negative quantity', () => {
    component.items.at(0).patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: -1
    });

    expect(component.items.at(0).get('quantity')?.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('is valid with two different products', () => {
    component.items.at(0).patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 2
    });
    component.addItem();
    component.items.at(1).patchValue({
      productId: '22222222-2222-2222-2222-222222222222',
      quantity: 3
    });

    expect(component.form.valid).toBe(true);
  });

  it('rejects duplicate products', () => {
    component.items.at(0).patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 2
    });
    component.addItem();
    component.items.at(1).patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 3
    });

    component.items.updateValueAndValidity();

    expect(component.items.hasError('duplicateProduct')).toBe(true);
    expect(component.form.valid).toBe(false);
  });
});
