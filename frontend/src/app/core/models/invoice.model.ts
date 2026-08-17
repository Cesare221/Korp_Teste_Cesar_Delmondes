export type InvoiceStatus = 'Open' | 'Closed';

export interface Invoice {
  id: string;
  number: number;
  status: InvoiceStatus;
  createdAt: string;
  closedAt: string | null;
  items: InvoiceItem[];
}

export interface InvoiceItem {
  id: string;
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface InvoiceListItem {
  id: string;
  number: number;
  status: InvoiceStatus;
  createdAt: string;
  itemCount: number;
}

export interface CreateInvoiceRequest {
  items: CreateInvoiceItemRequest[];
}

export interface CreateInvoiceItemRequest {
  productId: string;
  quantity: number;
}
