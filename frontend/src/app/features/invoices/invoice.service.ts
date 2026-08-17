import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiEndpoints } from '../../core/api/api-endpoints';
import { CreateInvoiceRequest, Invoice, InvoiceListItem } from '../../core/models/invoice.model';

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${apiEndpoints.billing}/invoices`;

  getInvoices(): Observable<InvoiceListItem[]> {
    return this.http.get<InvoiceListItem[]>(this.baseUrl);
  }

  getInvoice(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.baseUrl}/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<Invoice> {
    return this.http.post<Invoice>(this.baseUrl, request);
  }

  printInvoice(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/print`, null);
  }
}
