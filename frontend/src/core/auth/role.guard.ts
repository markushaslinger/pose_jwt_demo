import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot } from "@angular/router";
import { UserRole, UserService } from "../user/user.service";
import { inject } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";

export function roleGuard(requiredRole: UserRole): CanActivateFn {
  return async (route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> => {
    // we have to inject all dependencies before awaiting - this service is only need for demo purposes, see below
    const snackbar = inject(MatSnackBar);

    const userService = inject(UserService);
    const isAllowed = await userService.hasRole(requiredRole);

    // this is code for demo purpose, you wouldn't do that in a real app
    if (!isAllowed) {
      snackbar.open("Only logged-in users can access this page", "Close", {
        duration: 4000,
        verticalPosition: "top",
      });
    }

    return isAllowed;
  }
}
