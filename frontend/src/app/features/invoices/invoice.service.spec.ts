import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InvoiceService } from './invoice.service';

describe('InvoiceService', () => {
  let service: InvoiceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        InvoiceService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(InvoiceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('creates invoices through billing API', () => {
    const payload = {
      items: [{ productId: '11111111-1111-1111-1111-111111111111', quantity: 2 }]
    };
    const response = {
      id: '22222222-2222-2222-2222-222222222222',
      number: 1,
      status: 'Open' as const,
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

    service.createInvoice(payload).subscribe(invoice => {
      expect(invoice).toEqual(response);
    });

    const request = http.expectOne('http://localhost:5002/api/invoices');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush(response);
  });

  it('loads invoice list from billing API', () => {
    const expected = [{
      id: '22222222-2222-2222-2222-222222222222',
      number: 2,
      status: 'Open' as const,
      createdAt: '2026-08-15T10:00:00Z',
      itemCount: 2
    }];

    service.getInvoices().subscribe(invoices => {
      expect(invoices).toEqual(expected);
    });

    const request = http.expectOne('http://localhost:5002/api/invoices');
    expect(request.request.method).toBe('GET');
    request.flush(expected);
  });

  it('loads invoice detail from billing API', () => {
    const expected = {
      id: '22222222-2222-2222-2222-222222222222',
      number: 1,
      status: 'Open' as const,
      createdAt: '2026-08-15T10:00:00Z',
      closedAt: null,
      items: []
    };

    service.getInvoice(expected.id).subscribe(invoice => {
      expect(invoice).toEqual(expected);
    });

    const request = http.expectOne(`http://localhost:5002/api/invoices/${expected.id}`);
    expect(request.request.method).toBe('GET');
    request.flush(expected);
  });

  it('prints invoice through billing API', () => {
    const invoiceId = '22222222-2222-2222-2222-222222222222';
    const response = {
      id: invoiceId,
      number: 1,
      status: 'Closed' as const,
      createdAt: '2026-08-15T10:00:00Z',
      closedAt: '2026-08-15T10:05:00Z',
      items: []
    };

    service.printInvoice(invoiceId).subscribe(invoice => {
      expect(invoice).toEqual(response);
    });

    const request = http.expectOne(`http://localhost:5002/api/invoices/${invoiceId}/print`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush(response);
  });
});
