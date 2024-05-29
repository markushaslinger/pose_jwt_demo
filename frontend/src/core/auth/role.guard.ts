import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot } from "@angular/router";
import { UserRole, UserService } from "../user/user.service";
import { inject } from "@angular/core";

export function roleGuard(requiredRole: UserRole): CanActivateFn {
  return async (route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> => {
    const userService = inject(UserService);
    return await userService.hasRole(requiredRole);
  }
}
