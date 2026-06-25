import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Attaches the JWT bearer token to outgoing API calls and reacts to 401s by
// clearing the session and bouncing the user to the login page.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.getToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && auth.isLoggedIn() && !isAuthEndpoint(req.url)) {
        return auth.refreshSession().pipe(
          switchMap(() => {
            const refreshedToken = auth.getToken();
            const retryReq = refreshedToken
              ? req.clone({ setHeaders: { Authorization: `Bearer ${refreshedToken}` } })
              : req;
            return next(retryReq);
          }),
          catchError(() => {
            auth.clearSession();
            router.navigate(['/login']);
            return throwError(() => err);
          }),
        );
      }

      if (err.status === 401 && auth.isLoggedIn()) {
        auth.clearSession();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    }),
  );
};

function isAuthEndpoint(url: string): boolean {
  return ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout']
    .some(path => url.includes(path));
}
