export interface Product {
  id: string;
  code: string;
  description: string;
  balance: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  code: string;
  description: string;
  balance: number;
}
