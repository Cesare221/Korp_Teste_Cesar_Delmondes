import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'products'
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/products-page.component')
      .then(component => component.ProductsPageComponent),
    title: 'Produtos'
  },
  {
    path: 'invoices',
    loadComponent: () => import('./features/invoices/invoices-page.component')
      .then(component => component.InvoicesPageComponent),
    title: 'Notas Fiscais'
  },
  {
    path: 'invoices/new',
    loadComponent: () => import('./features/invoices/invoice-new-page.component')
      .then(component => component.InvoiceNewPageComponent),
    title: 'Nova Nota Fiscal'
  },
  {
    path: 'invoices/:id',
    loadComponent: () => import('./features/invoices/invoice-detail-page.component')
      .then(component => component.InvoiceDetailPageComponent),
    title: 'Detalhe da Nota Fiscal'
  }
];
