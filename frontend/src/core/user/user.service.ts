import { inject, Injectable } from "@angular/core";
import { AuthService } from "../auth/auth.service";
import { jwtDecode } from "jwt-decode";

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly authService = inject(AuthService);

  public async getUser(): Promise<IUser | null> {
    const token = await this.authService.getAccessToken();
    if (token) {
      return UserService.decodeToken(token.token);
    }
    return null;
  }

  public async isLoggedIn(): Promise<boolean> {
    const user = await this.getUser();
    return user !== null;
  }

  public async hasRole(role: UserRole): Promise<boolean> {
    const user = await this.getUser();

    // this works because we assume that higher roles contain all permissions of lower roles
    // and the enum values are ordered accordingly
    return user !== null && user.role >= role;
  }

  public static decodeToken(accessToken: string): IUser | null {
    const decoded = jwtDecode(accessToken) as IAccessToken;

    if (!decoded) {
      return null;
    }

    const roles = decoded.role as string[];
    let role: UserRole;
    if (roles.includes("Admin")) {
      role = UserRole.Admin;
    } else if (roles.includes("User")) {
      role = UserRole.User;
    } else if (roles.includes("Guest")) {
      role = UserRole.Guest;
    } else {
      role = UserRole.Unknown;
    }

    return {
      username: decoded.unique_name,
      role: role
    };
  }
}

interface IAccessToken {
  unique_name: string;
  role: string[];
}

export interface IUser {
  username: string;
  role: UserRole;
}

export enum UserRole {
  Guest = 0,
  User = 5,
  Admin = 20,
  Unknown = 100
}
