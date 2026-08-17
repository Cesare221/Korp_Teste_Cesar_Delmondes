import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { EMPTY, catchError, finalize, of } from 'rxjs';
import { Product } from '../../core/models/product.model';
import { ProductService } from '../products/product.service';
import { InvoiceService } from './invoice.service';

type InvoiceItemForm = FormGroup<{
  productId: FormControl<string>;
  quantity: FormControl<number>;
}>;

@Component({
  selector: 'app-invoice-new-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule
  ],
  template: `
    <section class="page-header">
      <div>
        <h1>Nova Nota Fiscal</h1>
      </div>
    </section>

    <mat-card appearance="outlined">
      <mat-card-content>
        @if (isLoadingProducts) {
          <div class="loading-state">
            <mat-spinner diameter="36" />
          </div>
        } @else if (loadError) {
          <div class="empty-state" role="alert">
            <mat-icon aria-hidden="true">error_outline</mat-icon>
            <p>{{ loadError }}</p>
            <button mat-button type="button" (click)="loadProducts()">Tentar novamente</button>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div formArrayName="items" class="items">
              @for (item of items.controls; track $index; let index = $index) {
                <div [formGroupName]="index" class="item-row">
                  <mat-form-field appearance="outline">
                    <mat-label>Produto</mat-label>
                    <mat-select formControlName="productId">
                      @for (product of products; track product.id) {
                        <mat-option
                          [value]="product.id"
                          [disabled]="isProductAlreadySelected(product.id, index)">
                          {{ product.code }} - {{ product.description }} - Saldo: {{ product.balance }}
                        </mat-option>
                      }
                    </mat-select>
                    @if (item.controls.productId.hasError('required')) {
                      <mat-error>Selecione um produto.</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Quantidade</mat-label>
                    <input matInput type="number" min="1" formControlName="quantity">
                    @if (item.controls.quantity.hasError('required')) {
                      <mat-error>Informe a quantidade.</mat-error>
                    }
                    @if (item.controls.quantity.hasError('min')) {
                      <mat-error>A quantidade deve ser maior que zero.</mat-error>
                    }
                  </mat-form-field>

                  <button mat-icon-button type="button" aria-label="Remover produto" (click)="removeItem(index)">
                    <mat-icon aria-hidden="true">delete</mat-icon>
                  </button>
                </div>
              }
            </div>

            @if (items.hasError('duplicateProduct')) {
              <p class="form-error" role="alert">O mesmo produto não pode aparecer duas vezes.</p>
            }
            @if (items.hasError('requiredItems')) {
              <p class="form-error" role="alert">Adicione pelo menos um produto.</p>
            }
            @if (errorMessage) {
              <p class="form-error" role="alert">{{ errorMessage }}</p>
            }

            <div class="actions">
              <button mat-button type="button" routerLink="/invoices" [disabled]="isSaving">Cancelar</button>
              <button mat-button type="button" (click)="addItem()" [disabled]="isSaving">
                <mat-icon aria-hidden="true">add</mat-icon>
                Adicionar produto
              </button>
              <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || isSaving">
                @if (isSaving) {
                  <mat-spinner diameter="18" />
                } @else {
                  Criar nota
                }
              </button>
            </div>
          </form>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .page-header {
      margin-bottom: var(--space-6);
    }

    h1 {
      margin: 0;
      color: var(--color-ink-black);
      font-family: var(--font-ui);
      font-size: clamp(1.8rem, 4vw, 2.75rem);
      font-weight: 600;
      line-height: 1.12;
    }

    .items {
      display: grid;
      gap: var(--space-4);
    }

    .item-row {
      display: grid;
      grid-template-columns: minmax(220px, 1fr) minmax(120px, 180px) 48px;
      gap: var(--space-3);
      align-items: start;
      border-radius: var(--radius-md);
      background: var(--color-ash-mist);
      box-shadow: 0 0 0 1px var(--color-border-soft);
      padding: var(--space-4);
    }

    .item-row button[mat-icon-button] {
      align-self: center;
      color: var(--color-smoke);
    }

    .item-row button[mat-icon-button]:hover {
      background: var(--color-danger-fill);
      color: var(--color-danger);
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: var(--space-3);
      margin-top: var(--space-5);
    }

    .actions button {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
    }

    .form-error {
      margin: var(--space-3) 0 0;
      border-radius: var(--radius-sm);
      background: var(--color-danger-fill);
      box-shadow: 0 0 0 1px rgba(180, 35, 24, 0.12);
      color: var(--color-danger);
      font-size: 0.875rem;
      padding: var(--space-3) var(--space-4);
    }

    .loading-state,
    .empty-state {
      display: grid;
      min-height: 180px;
      place-items: center;
      text-align: center;
    }

    .empty-state {
      gap: var(--space-3);
      border-radius: var(--radius-md);
      background: var(--color-ash-mist);
      color: var(--color-smoke);
      padding: var(--space-8);
    }

    .empty-state mat-icon {
      color: var(--color-signal-blue);
      font-size: 36px;
      height: 36px;
      width: 36px;
    }

    .empty-state p {
      margin: 0;
    }

    button[type='submit'] {
      min-width: 112px;
    }

    @media (max-width: 760px) {
      .item-row {
        grid-template-columns: 1fr;
      }

      .item-row button[mat-icon-button] {
        justify-self: end;
      }

      .actions {
        align-items: stretch;
        flex-direction: column;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InvoiceNewPageComponent implements OnInit {
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly invoiceService = inject(InvoiceService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly form = this.formBuilder.group({
    items: new FormArray<InvoiceItemForm>([], {
      validators: [InvoiceNewPageComponent.itemsRequiredValidator(), InvoiceNewPageComponent.uniqueProductsValidator()]
    })
  });

  products: Product[] = [];
  isLoadingProducts = false;
  isSaving = false;
  loadError = '';
  errorMessage = '';

  constructor() {
    this.addItem();
  }

  get items(): FormArray<InvoiceItemForm> {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoadingProducts = true;
    this.loadError = '';
    this.changeDetectorRef.markForCheck();

    this.productService.getProducts()
      .pipe(
        catchError(() => {
          this.loadError = 'Não foi possível carregar os produtos.';
          this.changeDetectorRef.markForCheck();
          return EMPTY;
        }),
        finalize(() => {
          this.isLoadingProducts = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(products => {
        this.products = products;
        this.changeDetectorRef.markForCheck();
      });
  }

  addItem(): void {
    this.items.push(this.createItemForm());
    this.items.updateValueAndValidity();
    this.changeDetectorRef.markForCheck();
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.items.updateValueAndValidity();
    this.changeDetectorRef.markForCheck();
  }

  isProductAlreadySelected(productId: string, currentIndex: number): boolean {
    return this.items.controls.some((control, index) =>
      index !== currentIndex && control.controls.productId.value === productId);
  }

  submit(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.changeDetectorRef.markForCheck();

    this.invoiceService.createInvoice({
      items: this.items.controls.map(item => ({
        productId: item.controls.productId.value,
        quantity: item.controls.quantity.value
      }))
    })
      .pipe(
        catchError((error: unknown) => {
          this.errorMessage = this.getErrorMessage(error);
          this.changeDetectorRef.markForCheck();
          return of(null);
        }),
        finalize(() => {
          this.isSaving = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(invoice => {
        if (!invoice) {
          return;
        }

        this.snackBar.open('Nota fiscal criada com sucesso.', 'Fechar', {
          duration: 3000
        });
        void this.router.navigate(['/invoices', invoice.id]);
      });
  }

  private createItemForm(): InvoiceItemForm {
    return this.formBuilder.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 422) {
        return 'Um ou mais produtos selecionados não existem mais.';
      }

      if (error.status === 503) {
        return 'Não foi possível validar os produtos no momento. Tente novamente.';
      }
    }

    return 'Não foi possível criar a nota fiscal.';
  }

  private static itemsRequiredValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const items = control as FormArray;
      return items.length > 0 ? null : { requiredItems: true };
    };
  }

  private static uniqueProductsValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const items = control as FormArray<InvoiceItemForm>;
      const selectedProductIds = items.controls
        .map(item => item.controls.productId.value)
        .filter(productId => productId.length > 0);
      const uniqueProductIds = new Set(selectedProductIds);

      return uniqueProductIds.size === selectedProductIds.length
        ? null
        : { duplicateProduct: true };
    };
  }
}
