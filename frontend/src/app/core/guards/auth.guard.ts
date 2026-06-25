import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LanguageService } from '../services/language.service';
import { NoticeService } from '../services/notice.service';
import { UserRole } from '../models/auth.models';

// Blocks a route unless the user is logged in. Sends guests to /login and
// remembers where they were headed so we can return them after login.
// Re-runs on browser back/forward too, so logging out and pressing "back"
// into a protected page bounces the user to /login instead of showing it.
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  const notice = inject(NoticeService);
  const lang = inject(LanguageService);
  notice.show(lang.t('Please sign in to access that page.', 'يرجى تسجيل الدخول للوصول إلى تلك الصفحة.'), 'error');

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

// Keeps already-authenticated users out of /login and /signup. Handles both
// typing the URL directly and pressing "back" onto the auth pages after login.
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return true;

  const notice = inject(NoticeService);
  const lang = inject(LanguageService);
  notice.show(lang.t("You're already signed in.", 'أنت مسجّل الدخول بالفعل.'));

  return router.createUrlTree([auth.homeRouteForRole(auth.role())]);
};

// Restricts a route to specific roles. Attach allowed roles via route data:
//   { path: 'admin', canActivate: [roleGuard], data: { roles: ['Admin'] } }
export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notice = inject(NoticeService);
  const lang = inject(LanguageService);

  if (!auth.isLoggedIn()) {
    notice.show(lang.t('Please sign in to access that page.', 'يرجى تسجيل الدخول للوصول إلى تلك الصفحة.'), 'error');
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  const allowed = (route.data?.['roles'] as UserRole[] | undefined) ?? [];
  const role = auth.role();
  if (allowed.length === 0 || (role && allowed.includes(role))) return true;

  // Logged in but wrong role: send them to their own home instead of the page.
  notice.show(lang.t("You don't have access to that page.", 'ليس لديك صلاحية الوصول إلى تلك الصفحة.'), 'error');
  return router.createUrlTree([auth.homeRouteForRole(role)]);
};
