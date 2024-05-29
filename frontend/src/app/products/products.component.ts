import { ChangeDetectionStrategy, Component, inject, signal, Signal } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { IProduct, ProductService } from "../../core/product/product.service";
import { toSignal } from "@angular/core/rxjs-interop";
import { from } from "rxjs";
import {
  MatCell,
  MatCellDef,
  MatColumnDef,
  MatHeaderCell,
  MatHeaderCellDef,
  MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef,
  MatTable
} from "@angular/material/table";
import { MatIconButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";

@Component({
  selector: "app-products",
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatCard,
    MatTable,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatCell,
    MatCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatIconButton,
    MatIcon
  ],
  templateUrl: "./products.component.html",
  styleUrl: "./products.component.scss"
})
export class ProductsComponent {
  private readonly productService: ProductService = inject(ProductService);
  public readonly products: Signal<IProduct[]> = toSignal(from(this.productService.getAllProducts()),
    {initialValue: []});
  public readonly displayedColumns: Signal<string[]> = signal(["id", "name", "price"]);

  public async editProduct(productId: number): Promise<void> {

  }
}


