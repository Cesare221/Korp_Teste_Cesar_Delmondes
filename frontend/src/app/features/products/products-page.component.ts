import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, HostListener, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { EMPTY, catchError, filter, finalize, switchMap } from 'rxjs';
import { Product } from '../../core/models/product.model';
import { ProductFormDialogComponent } from './product-form-dialog.component';
import { ProductService } from './product.service';

@Component({
  selector: 'app-products-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule
  ],
  template: `
    <section class="page-header">
      <div>
        <h1>Produtos</h1>
      </div>

      <div class="header-actions">
        <button mat-button type="button" [disabled]="isLoading" (click)="loadProducts()">
          <mat-icon aria-hidden="true">sync</mat-icon>
          Atualizar lista
        </button>

        <button mat-flat-button color="primary" type="button" (click)="openCreateDialog()">
          <mat-icon aria-hidden="true">add</mat-icon>
          Novo produto
        </button>
      </div>
    </section>

    <mat-card appearance="outlined">
      <mat-card-content>
        @if (isLoading) {
          <div class="loading-state">
            <mat-spinner diameter="36" />
          </div>
        } @else if (loadError) {
          <div class="empty-state" role="alert">
            <mat-icon aria-hidden="true">error_outline</mat-icon>
            <p>{{ loadError }}</p>
            <button mat-button type="button" (click)="loadProducts()">Tentar novamente</button>
          </div>
        } @else if (products.length === 0) {
          <div class="empty-state">
            <mat-icon aria-hidden="true">inventory_2</mat-icon>
            <p>Nenhum produto cadastrado.</p>
          </div>
        } @else {
          <div class="table-scroll">
            <table mat-table [dataSource]="products">
              <ng-container matColumnDef="code">
                <th mat-header-cell *matHeaderCellDef>Código</th>
                <td mat-cell *matCellDef="let product">{{ product.code }}</td>
              </ng-container>

              <ng-container matColumnDef="description">
                <th mat-header-cell *matHeaderCellDef>Descrição</th>
                <td mat-cell *matCellDef="let product">{{ product.description }}</td>
              </ng-container>

              <ng-container matColumnDef="balance">
                <th mat-header-cell *matHeaderCellDef>Saldo</th>
                <td mat-cell *matCellDef="let product">{{ product.balance }}</td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
            </table>
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-5);
      margin-bottom: var(--space-6);
    }

    .header-actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-3);
      justify-content: flex-end;
    }

    .header-actions button {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      justify-content: center;
    }

    h1 {
      margin: 0;
      color: var(--color-ink-black);
      font-family: var(--font-ui);
      font-size: clamp(1.8rem, 4vw, 2.75rem);
      font-weight: 600;
      line-height: 1.12;
    }

    table {
      min-width: 520px;
      width: 100%;
    }

    .table-scroll {
      border-radius: var(--radius-sm);
      overflow-x: auto;
      box-shadow: 0 0 0 1px var(--color-border-soft);
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

    @media (max-width: 640px) {
      .page-header {
        align-items: stretch;
        flex-direction: column;
      }

      .header-actions,
      .header-actions button {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly dialog = inject(MatDialog);
  private readonly productService = inject(ProductService);
  private readonly snackBar = inject(MatSnackBar);

  readonly displayedColumns = ['code', 'description', 'balance'];
  products: Product[] = [];
  isLoading = false;
  loadError = '';

  ngOnInit(): void {
    this.loadProducts();
  }

  @HostListener('window:focus')
  reloadProductsOnFocus(): void {
    this.reloadProductsIfIdle();
  }

  @HostListener('document:visibilitychange')
  reloadProductsOnVisibilityChange(): void {
    if (document.visibilityState === 'visible') {
      this.reloadProductsIfIdle();
    }
  }

  loadProducts(): void {
    this.isLoading = true;
    this.loadError = '';
    this.products = [];
    this.changeDetectorRef.markForCheck();

    this.productService.getProducts()
      .pipe(
        catchError(() => {
          this.loadError = 'Não foi possível carregar os produtos.';
          this.changeDetectorRef.markForCheck();
          return EMPTY;
        }),
        finalize(() => {
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(products => {
        this.products = products;
        this.changeDetectorRef.markForCheck();
      });
  }

  openCreateDialog(): void {
    this.dialog.open(ProductFormDialogComponent)
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.isLoading = true;
          this.loadError = '';
          this.products = [];
          this.changeDetectorRef.markForCheck();
          return this.productService.getProducts();
        }),
        catchError(() => {
          this.snackBar.open('Não foi possível atualizar a lista de produtos.', 'Fechar', {
            duration: 4000
          });
          this.changeDetectorRef.markForCheck();
          return EMPTY;
        }),
        finalize(() => {
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(products => {
        this.products = products;
        this.changeDetectorRef.markForCheck();
      });
  }

  private reloadProductsIfIdle(): void {
    if (!this.isLoading) {
      this.loadProducts();
    }
  }
}
