import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductService } from './product.service';
import { ProductsPageComponent } from './products-page.component';

describe('ProductsPageComponent', () => {
  let fixture: ComponentFixture<ProductsPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductsPageComponent],
      providers: [
        {
          provide: ProductService,
          useValue: {
            getProducts: () => of([{
              id: '11111111-1111-1111-1111-111111111111',
              code: 'PROD-001',
              description: 'Produto de teste',
              balance: 10,
              createdAt: '2026-08-14T10:00:00Z',
              updatedAt: '2026-08-14T10:00:00Z'
            }]),
            createProduct: vi.fn()
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductsPageComponent);
    fixture.detectChanges();
  });

  it('renders loaded products in the table', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('PROD-001');
    expect(text).toContain('Produto de teste');
    expect(text).toContain('10');
  });
});
