import { Routes } from '@angular/router';
import { LoginComponent } from "./login/login.component";
import { HomeComponent } from "./home/home.component";
import { ProductsComponent } from "./products/products.component";
import { roleGuard } from "../core/auth/role.guard";
import { UserRole } from "../core/user/user.service";

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent },
  { path: 'products', component: ProductsComponent, canActivate: [roleGuard(UserRole.User)] },
  { path: '', redirectTo: '/home', pathMatch: 'full' },
];
