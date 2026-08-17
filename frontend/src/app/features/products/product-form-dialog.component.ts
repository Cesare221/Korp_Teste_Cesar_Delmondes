import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { catchError, finalize, of } from 'rxjs';
import { ProductService } from './product.service';

@Component({
  selector: 'app-product-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title>Novo produto</h2>

    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="form-content">
        <mat-form-field appearance="outline">
          <mat-label>Código</mat-label>
          <input matInput formControlName="code" autocomplete="off">
          @if (form.controls.code.hasError('required')) {
            <mat-error>Informe o código.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Descrição</mat-label>
          <input matInput formControlName="description" autocomplete="off">
          @if (form.controls.description.hasError('required')) {
            <mat-error>Informe a descrição.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Saldo</mat-label>
          <input matInput type="number" formControlName="balance" min="0">
          @if (form.controls.balance.hasError('required')) {
            <mat-error>Informe o saldo.</mat-error>
          }
          @if (form.controls.balance.hasError('min')) {
            <mat-error>O saldo não pode ser negativo.</mat-error>
          }
        </mat-form-field>

        @if (errorMessage) {
          <p class="form-error" role="alert">{{ errorMessage }}</p>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" [disabled]="isSaving" mat-dialog-close>Cancelar</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || isSaving">
          @if (isSaving) {
            <mat-spinner diameter="18" />
          } @else {
            Salvar
          }
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`
    h2 {
      color: var(--color-ink-black);
      font-family: var(--font-ui);
      font-weight: 600;
    }

    .form-content {
      display: grid;
      gap: var(--space-4);
      min-width: min(420px, 82vw);
      padding-top: var(--space-2);
    }

    .form-error {
      margin: 0;
      border-radius: var(--radius-sm);
      background: var(--color-danger-fill);
      box-shadow: 0 0 0 1px rgba(180, 35, 24, 0.12);
      color: var(--color-danger);
      font-size: 0.875rem;
      padding: var(--space-3) var(--space-4);
    }

    button[type='submit'] {
      min-width: 96px;
    }

    mat-dialog-actions {
      gap: var(--space-2);
      padding-inline: 24px;
      padding-bottom: 24px;
    }

    @media (max-width: 520px) {
      .form-content {
        min-width: 0;
        width: 100%;
      }

      mat-dialog-actions {
        align-items: stretch;
        flex-direction: column-reverse;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductFormDialogComponent {
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly productService = inject(ProductService);
  private readonly dialogRef = inject(MatDialogRef<ProductFormDialogComponent, boolean>);
  private readonly snackBar = inject(MatSnackBar);

  readonly form = this.formBuilder.group({
    code: ['', Validators.required],
    description: ['', Validators.required],
    balance: [0, [Validators.required, Validators.min(0)]]
  });

  isSaving = false;
  errorMessage = '';

  save(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.changeDetectorRef.markForCheck();

    this.productService.createProduct(this.form.getRawValue())
      .pipe(
        catchError((error: unknown) => {
          this.errorMessage = this.getErrorMessage(error);
          this.changeDetectorRef.markForCheck();
          return of(null);
        }),
        finalize(() => {
          this.isSaving = false;
          this.changeDetectorRef.markForCheck();
        })
      )
      .subscribe(product => {
        if (!product) {
          return;
        }

        this.snackBar.open('Produto cadastrado com sucesso.', 'Fechar', {
          duration: 3000
        });
        this.dialogRef.close(true);
      });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 409) {
        return 'Já existe um produto com este código.';
      }

      if (error.status === 400) {
        return 'Verifique os campos informados.';
      }
    }

    return 'Não foi possível cadastrar o produto. Tente novamente.';
  }
}
