import { inject, Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { API_BASE_URL } from "../const";
import { firstValueFrom } from "rxjs";
import { z } from "zod";

@Injectable({
  providedIn: "root"
})
export class ProductService {
  private readonly baseUrl = `${API_BASE_URL}/products`;
  private readonly client: HttpClient = inject(HttpClient);

  public async getAllProducts(): Promise<IProduct[]> {
    // HttpClient will have the bearer token set automatically by the interceptor for every request!
    const response = await firstValueFrom(this.client.get(this.baseUrl, { observe: "response" }));
    if (response.ok) {
      return ProductsWire.parse(response.body) as IProduct[];
    }

    // throughout this service the error handling and logging is very basic again - just for demonstration purposes
    if (response.status === 401) {
      console.log("Unauthenticated user cannot get products");
    }

    return [];
  }

  public async getProductById(id: number): Promise<IProduct | null> {
    const url = `${this.baseUrl}/${id}`;
    const response = await firstValueFrom(this.client.get(url, { observe: "response" }));
    if (response.ok) {
      return ProductWire.parse(response.body) as IProduct;
    }

    if (response.status === 401) {
      console.log("Unauthenticated user cannot get product by id");
    }

    return null;
  }

  public async updatePrice(id: number, price: number): Promise<boolean> {
    const url = `${this.baseUrl}/${id}/price`;
    const response = await firstValueFrom(this.client.patch(url, { price }, { observe: "response" }));
    if (response.ok) {
      return true;
    }

    switch (response.status) {
      case 401:
        console.log("Unauthenticated user cannot update price");
        break;
      case 403:
        console.log("User does not have permission to update price");
        break;
      case 404:
        console.log("Product not found");
        break;
      default:
        // being lazy here
        break;
    }

    return false;
  }
}

export interface IProduct {
  id: number;
  name: string;
  price: number;
}

const ProductWire = z.object({
  id: z.number(),
  name: z.string().min(1),
  price: z.number()
});
const ProductsWire = z.array(ProductWire);
