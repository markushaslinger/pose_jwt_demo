import { ChangeDetectionStrategy, Component, inject, OnInit, signal, Signal, WritableSignal } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { IProduct, ProductService } from "../../core/product/product.service";
import { toSignal } from "@angular/core/rxjs-interop";
import { firstValueFrom, from } from "rxjs";
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
import { MatDialog } from "@angular/material/dialog";
import { ProductEditDialogComponent } from "./product-edit-dialog/product-edit-dialog.component";
import { UserRole, UserService } from "../../core/user/user.service";
import { MatProgressBar } from "@angular/material/progress-bar";

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
    MatIcon,
    MatProgressBar
  ],
  templateUrl: "./products.component.html",
  styleUrl: "./products.component.scss"
})
export class ProductsComponent implements OnInit {
  private readonly productService: ProductService = inject(ProductService);
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);
  public readonly loading = signal(false);
  public readonly products: WritableSignal<IProduct[]> = signal([]);
  public readonly displayedColumns: WritableSignal<string[]> = signal(["id", "name", "price"]);

  public async editProduct(productId: number): Promise<void> {
    // we could (and should) get this from the local array, just wanted to use all API endpoints
    const product = await this.productService.getProductById(productId);
    if (!product){
      return;
    }

    const origPrice = product.price;
    const dialogRef = this.dialog.open(ProductEditDialogComponent, {
      data: product
    });

    const newPrice = await firstValueFrom(dialogRef.afterClosed());

    this.loading.set(true);
    if (newPrice !== origPrice) {
      await this.productService.updatePrice(productId, newPrice);

      // local update instead of full reload would be more efficient
      await this.loadData();
    }

    this.loading.set(false);
  }

  public async ngOnInit(): Promise<void> {
    this.loading.set(true);

    await this.loadData();

    const isAdmin = await this.userService.hasRole(UserRole.Admin);
    if (isAdmin){
      this.displayedColumns.update(origCols => [...origCols, "edit"]);
    }

    this.loading.set(false);
  }

  private async loadData(): Promise<void> {
    const products = await this.productService.getAllProducts();
    this.products.set(products);
  }
}


