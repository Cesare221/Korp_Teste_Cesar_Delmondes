import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProductService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('loads products from inventory API', () => {
    const expected = [{
      id: '11111111-1111-1111-1111-111111111111',
      code: 'PROD-001',
      description: 'Produto de teste',
      balance: 10,
      createdAt: '2026-08-14T10:00:00Z',
      updatedAt: '2026-08-14T10:00:00Z'
    }];

    service.getProducts().subscribe(products => {
      expect(products).toEqual(expected);
    });

    const request = http.expectOne('http://localhost:5001/api/products');
    expect(request.request.method).toBe('GET');
    request.flush(expected);
  });

  it('creates product through inventory API', () => {
    const payload = {
      code: 'PROD-001',
      description: 'Produto de teste',
      balance: 10
    };
    const response = {
      id: '11111111-1111-1111-1111-111111111111',
      ...payload,
      createdAt: '2026-08-14T10:00:00Z',
      updatedAt: '2026-08-14T10:00:00Z'
    };

    service.createProduct(payload).subscribe(product => {
      expect(product).toEqual(response);
    });

    const request = http.expectOne('http://localhost:5001/api/products');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush(response);
  });
});
