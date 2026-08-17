import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { EMPTY, catchError, finalize } from 'rxjs';
import { InvoiceListItem, InvoiceStatus } from '../../core/models/invoice.model';
import { InvoiceService } from './invoice.service';

@Component({
  selector: 'app-invoices-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  template: `
    <section class="page-header">
      <div>
        <h1>Notas Fiscais</h1>
      </div>

      <a mat-flat-button color="primary" routerLink="/invoices/new">
        <mat-icon aria-hidden="true">add</mat-icon>
        Nova nota
      </a>
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
            <button mat-button type="button" (click)="loadInvoices()">Tentar novamente</button>
          </div>
        } @else if (invoices.length === 0) {
          <div class="empty-state">
            <mat-icon aria-hidden="true">description</mat-icon>
            <p>Nenhuma nota fiscal cadastrada.</p>
          </div>
        } @else {
          <div class="table-scroll">
            <table mat-table [dataSource]="invoices">
              <ng-container matColumnDef="number">
                <th mat-header-cell *matHeaderCellDef>Número</th>
                <td mat-cell *matCellDef="let invoice">Nota {{ invoice.number }}</td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let invoice">
                  <mat-chip>{{ statusLabel(invoice.status) }}</mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="createdAt">
                <th mat-header-cell *matHeaderCellDef>Data de criação</th>
                <td mat-cell *matCellDef="let invoice">{{ formatDate(invoice.createdAt) }}</td>
              </ng-container>

              <ng-container matColumnDef="itemCount">
                <th mat-header-cell *matHeaderCellDef>Itens</th>
                <td mat-cell *matCellDef="let invoice">{{ invoice.itemCount }}</td>
              </ng-container>

              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Ações</th>
                <td mat-cell *matCellDef="let invoice">
                  <a mat-button [routerLink]="['/invoices', invoice.id]">Visualizar</a>
                </td>
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

    a[mat-flat-button] {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      justify-content: center;
    }

    table {
      min-width: 760px;
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

    @media (max-width: 760px) {
      .page-header {
        align-items: stretch;
        flex-direction: column;
      }

      a[mat-flat-button] {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InvoicesPageComponent implements OnInit {
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly invoiceService = inject(InvoiceService);

  readonly displayedColumns = ['number', 'status', 'createdAt', 'itemCount', 'actions'];
  invoices: InvoiceListItem[] = [];
  isLoading = false;
  loadError = '';

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.isLoading = true;
    this.loadError = '';
    this.invoices = [];
    this.changeDetectorRef.markForCheck();

    this.invoiceService.getInvoices()
      .pipe(
        catchError(() => {
          this.loadError = 'Não foi possível carregar as notas fiscais.';
          this.changeDetectorRef.markForCheck();
          return EMPTY;
        }),
        finalize(() => {
          this.isLoading = false;
          this.changeDetectorRef.markForCheck();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(invoices => {
        this.invoices = invoices;
        this.changeDetectorRef.markForCheck();
      });
  }

  statusLabel(status: InvoiceStatus): string {
    return status === 'Open' ? 'Aberta' : 'Fechada';
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleString('pt-BR');
  }
}
