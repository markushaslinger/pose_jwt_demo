import { ChangeDetectionStrategy, Component, inject, Inject } from "@angular/core";
import {
  MAT_DIALOG_DATA,
  MatDialogActions, MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from "@angular/material/dialog";
import { MatFormField, MatLabel } from "@angular/material/form-field";
import { MatButton } from "@angular/material/button";
import { MatInput } from "@angular/material/input";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { IProduct } from "../../../core/product/product.service";

@Component({
  selector: 'app-product-edit-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatFormField,
    MatDialogActions,
    MatButton,
    MatInput,
    MatLabel,
    ReactiveFormsModule,
    MatDialogClose,
    FormsModule
  ],
  templateUrl: './product-edit-dialog.component.html',
  styleUrl: './product-edit-dialog.component.scss'
})
export class ProductEditDialogComponent {
  public readonly dialogRef: MatDialogRef<ProductEditDialogComponent> = inject(MatDialogRef<ProductEditDialogComponent>);
  public readonly product: IProduct = inject(MAT_DIALOG_DATA);

  onNoClick(): void {
    this.dialogRef.close();
  }
}
