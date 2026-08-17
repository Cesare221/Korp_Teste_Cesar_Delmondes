import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ProductFormDialogComponent } from './product-form-dialog.component';

describe('ProductFormDialogComponent', () => {
  let fixture: ComponentFixture<ProductFormDialogComponent>;
  let component: ProductFormDialogComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductFormDialogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
        { provide: MAT_DIALOG_DATA, useValue: null }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('requires product code', () => {
    component.form.patchValue({
      code: '',
      description: 'Produto',
      balance: 0
    });

    expect(component.form.controls.code.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('requires product description', () => {
    component.form.patchValue({
      code: 'PROD-001',
      description: '',
      balance: 0
    });

    expect(component.form.controls.description.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('rejects negative balance', () => {
    component.form.patchValue({
      code: 'PROD-001',
      description: 'Produto',
      balance: -1
    });

    expect(component.form.controls.balance.invalid).toBe(true);
    expect(component.form.valid).toBe(false);
  });

  it('is valid when all fields are valid', () => {
    component.form.patchValue({
      code: 'PROD-001',
      description: 'Produto',
      balance: 10
    });

    expect(component.form.valid).toBe(true);
  });
});
