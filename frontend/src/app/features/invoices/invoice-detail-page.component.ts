import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { EMPTY, catchError, finalize, of, switchMap } from 'rxjs';
import { Invoice, InvoiceStatus } from '../../core/models/invoice.model';
import { InvoiceService } from './invoice.service';

@Component({
  selector: 'app-invoice-detail-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule
  ],
  template: `
    <section class="page-header">
      <div>
        <h1>{{ invoice ? 'Nota Fiscal Nº ' + invoice.number : 'Nota Fiscal' }}</h1>
      </div>

      <div class="screen-actions">
        @if (invoice?.status === 'Open') {
          <button mat-flat-button color="primary" type="button" [disabled]="isPrinting" (click)="printInvoice()">
            @if (isPrinting) {
              <mat-spinner diameter="18" />
            } @else {
              <ng-container>
                <mat-icon aria-hidden="true">print</mat-icon>
                Finalizar e imprimir
              </ng-container>
            }
          </button>
        }

        <a mat-button routerLink="/invoices">
          <mat-icon aria-hidden="true">arrow_back</mat-icon>
          Voltar
        </a>
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
          </div>
        } @else if (invoice) {
          @if (isPrinting) {
            <p class="processing-state">Processando nota fiscal...</p>
          }
          @if (printError) {
            <p class="print-error" role="alert">{{ printError }}</p>
          }

          <section class="summary">
            <div>
              <span>Status</span>
              <mat-chip class="status-chip" [class.status-open]="invoice.status === 'Open'" [class.status-closed]="invoice.status === 'Closed'">
                {{ statusLabel(invoice.status) }}
              </mat-chip>
            </div>
            <div>
              <span>Criada em</span>
              <strong>{{ formatDate(invoice.createdAt) }}</strong>
            </div>
          </section>

          <div class="table-scroll">
            <table mat-table [dataSource]="invoice.items">
              <ng-container matColumnDef="productCode">
                <th mat-header-cell *matHeaderCellDef>Código</th>
                <td mat-cell *matCellDef="let item">{{ item.productCode }}</td>
              </ng-container>

              <ng-container matColumnDef="productDescription">
                <th mat-header-cell *matHeaderCellDef>Produto</th>
                <td mat-cell *matCellDef="let item">{{ item.productDescription }}</td>
              </ng-container>

              <ng-container matColumnDef="quantity">
                <th mat-header-cell *matHeaderCellDef>Quantidade</th>
                <td mat-cell *matCellDef="let item">{{ item.quantity }}</td>
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

    h1 {
      margin: 0;
      color: var(--color-ink-black);
      font-family: var(--font-ui);
      font-size: clamp(1.8rem, 4vw, 2.75rem);
      font-weight: 600;
      line-height: 1.12;
    }

    .screen-actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-3);
      justify-content: flex-end;
    }

    .screen-actions button,
    .screen-actions a {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      justify-content: center;
    }

    .processing-state {
      margin: 0 0 var(--space-4);
      border-radius: var(--radius-sm);
      background: #eef6ff;
      box-shadow: 0 0 0 1px rgba(0, 122, 255, 0.14);
      color: #005bbf;
      font-weight: 500;
      padding: var(--space-3) var(--space-4);
    }

    .print-error {
      margin: 0 0 var(--space-4);
      border-radius: var(--radius-sm);
      background: var(--color-danger-fill);
      box-shadow: 0 0 0 1px rgba(180, 35, 24, 0.12);
      color: var(--color-danger);
      font-size: 0.875rem;
      padding: var(--space-3) var(--space-4);
    }

    .summary {
      display: grid;
      grid-template-columns: repeat(2, minmax(180px, 1fr));
      gap: var(--space-4);
      margin-bottom: var(--space-5);
    }

    .summary div {
      display: grid;
      gap: var(--space-2);
      border-radius: var(--radius-sm);
      background: var(--color-ash-mist);
      box-shadow: 0 0 0 1px var(--color-border-soft);
      padding: var(--space-4);
    }

    .summary span {
      color: var(--color-smoke);
      font-size: 0.8125rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .summary strong {
      color: var(--color-ink-black);
      font-weight: 500;
    }

    .status-chip {
      width: max-content;
      font-weight: 600;
    }

    .status-open {
      --mdc-chip-elevated-container-color: #eef6ff;
      --mdc-chip-label-text-color: #005bbf;
    }

    .status-closed {
      --mdc-chip-elevated-container-color: #edf7ed;
      --mdc-chip-label-text-color: #1f6b3a;
    }

    table {
      min-width: 620px;
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

    @media (max-width: 720px) {
      .page-header,
      .screen-actions {
        align-items: stretch;
        flex-direction: column;
      }

      .screen-actions button,
      .screen-actions a {
        width: 100%;
      }

      .summary {
        grid-template-columns: 1fr;
      }
    }

    @media print {
      .page-header {
        margin-bottom: 18px;
        border-bottom: 1px solid #d1d5db;
        padding-bottom: 10px;
      }

      h1 {
        color: #111827;
        font-family: Arial, sans-serif;
        font-size: 24px;
        font-weight: 700;
        line-height: 1.2;
      }

      .screen-actions,
      .processing-state,
      .print-error {
        display: none !important;
      }

      .summary {
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 12px;
        margin-bottom: 18px;
      }

      .summary div {
        border: 1px solid #d1d5db;
        background: #fff;
        box-shadow: none;
        padding: 10px 12px;
      }

      mat-card {
        border: 0;
        box-shadow: none;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InvoiceDetailPageComponent implements OnInit {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly invoiceService = inject(InvoiceService);
  private readonly snackBar = inject(MatSnackBar);

  readonly displayedColumns = ['productCode', 'productDescription', 'quantity'];
  invoice: Invoice | null = null;
  isLoading = false;
  isPrinting = false;
  loadError = '';
  printError = '';

  ngOnInit(): void {
    this.isLoading = true;
    this.changeDetectorRef.markForCheck();

    this.activatedRoute.paramMap
      .pipe(
        switchMap(params => this.invoiceService.getInvoice(params.get('id') ?? '')
          .pipe(
            catchError(() => {
              this.loadError = 'Não foi possível carregar a nota fiscal.';
              this.changeDetectorRef.markForCheck();
              return EMPTY;
            }),
            finalize(() => {
              this.isLoading = false;
              this.changeDetectorRef.markForCheck();
            })
          )),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(invoice => {
        this.invoice = invoice;
        this.changeDetectorRef.markForCheck();
      });
  }

  printInvoice(): void {
    if (!this.invoice || this.invoice.status !== 'Open' || this.isPrinting) {
      return;
    }

    this.isPrinting = true;
    this.printError = '';
    this.changeDetectorRef.markForCheck();

    this.invoiceService.printInvoice(this.invoice.id)
      .pipe(
        catchError((error: unknown) => {
          this.printError = this.getPrintErrorMessage(error);
          this.changeDetectorRef.markForCheck();
          return of(null);
        }),
        finalize(() => {
          this.isPrinting = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(invoice => {
        if (!invoice) {
          return;
        }

        this.invoice = invoice;
        this.snackBar.open('Nota fiscal fechada e estoque atualizado.', 'Fechar', {
          duration: 3000
        });
        this.changeDetectorRef.markForCheck();
        window.print();
      });
  }

  statusLabel(status: InvoiceStatus): string {
    return status === 'Open' ? 'Aberta' : 'Fechada';
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleString('pt-BR');
  }

  private getPrintErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 409) {
        return 'Não há saldo suficiente para um ou mais produtos desta nota.';
      }

      if (error.status === 503) {
        return 'Serviço de estoque temporariamente indisponível. Tente novamente.';
      }
    }

    return 'Não foi possível processar a nota fiscal.';
  }
}
