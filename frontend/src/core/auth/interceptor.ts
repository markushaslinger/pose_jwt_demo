import { HttpContextToken, HttpEvent, HttpHandlerFn, HttpRequest } from "@angular/common/http";
import { from, lastValueFrom, Observable } from "rxjs";
import { inject } from "@angular/core";
import { AuthService } from "./auth.service";

export const TOKEN_INTERCEPTION_DISABLED = new HttpContextToken<boolean>(() =>  true);
export function tokenInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
  if (req.context.get(TOKEN_INTERCEPTION_DISABLED)) {
    return next(req);
  }

  return from(handle(req, next));
}

async function handle(req: HttpRequest<any>, next: HttpHandlerFn) {
  const token = await getToken();
  let newReq;
  if (!token) {
    newReq = req;
  } else {
    newReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return lastValueFrom(next(newReq));
}

async function getToken(): Promise<string | null> {
  const authService = inject(AuthService);
  const accessToken = await authService.getAccessToken();
  if (accessToken) {
    return accessToken.token;
  }
  return null;
}
