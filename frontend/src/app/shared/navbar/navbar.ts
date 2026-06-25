import { CommonModule } from '@angular/common';
import { Component, DestroyRef, HostListener, effect, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LanguageService } from '../../core/services/language.service';
import { ThemeService } from '../../core/services/theme.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { CartDrawerService } from '../../core/services/cart-drawer.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationBell } from '../components/notification-bell/notification-bell';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, NotificationBell],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  lang = inject(LanguageService);
  theme = inject(ThemeService);
  cart = inject(CartService);
  auth = inject(AuthService);
  private cartApi = inject(CartApiService);
  private cartDrawer = inject(CartDrawerService);
  private notifications = inject(NotificationService);

  // Open the realtime notifications feed while signed in; tear it down on logout.
  private readonly notificationsEffect = effect(() => {
    if (this.auth.isLoggedIn()) {
      this.notifications.start();
    } else {
      this.notifications.stop();
    }
  });

  private readonly cartRefreshEffect = effect(() => {
    if (!this.auth.isLoggedIn() || !this.auth.isShopper()) {
      this.cart.clear();
      return;
    }

    this.cartApi.get().subscribe({
      next: cart => this.cart.setFromCart(cart),
      error: () => this.cart.clear(),
    });
  });

  isScrolled = false;
  isHomeRoute = false;
  mobileOpen = false;
  activeId = 'hero';

  readonly shopperLinks: { id: string; en: string; ar: string; route?: string }[] = [
    { id: 'hero', en: 'Home', ar: 'الرئيسية' },
    { id: 'ai', en: 'AI Search', ar: 'بحث ذكي', route: '/ai-search' },
    { id: 'trending', en: 'Shop', ar: 'تسوّق' },
    { id: 'why', en: 'Stores', ar: 'المحلات', route: '/stores' },
    { id: 'experience', en: 'How it works', ar: 'إزاي بتشتغل' },
  ];
  // Signed-in shoppers get their dedicated home surfaced first.
  private readonly discoverLink = { id: 'discover', en: 'Discover', ar: 'اكتشف', route: '/discover' };
  readonly merchantLinks: { id: string; en: string; ar: string; route?: string }[] = [
    { id: 'merchant', en: 'Merchant portal', ar: 'بوابة التاجر', route: '/merchant' },
  ];
  readonly adminLinks: { id: string; en: string; ar: string; route?: string }[] = [
    { id: 'admin', en: 'Admin dashboard', ar: 'لوحة الإدارة', route: '/admin' },
  ];

  constructor() {
    this.syncRouteState();
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.syncRouteState());
  }

  get useHeroNav(): boolean {
    return this.isHomeRoute && !this.isScrolled;
  }

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  visibleLinks(): { id: string; en: string; ar: string; route?: string }[] {
    if (this.auth.isAdmin()) return this.adminLinks;
    if (this.auth.isStoreOwner()) return this.merchantLinks;
    // Show Discover only to signed-in shoppers (guests have no discover home).
    return this.auth.isShopper() ? [this.discoverLink, ...this.shopperLinks] : this.shopperLinks;
  }

  themeLabel(): string {
    return this.theme.isDark()
      ? this.L('Switch to light theme', 'غير للوضع الفاتح')
      : this.L('Switch to dark theme', 'غير للوضع الداكن');
  }

  languageLabel(): string {
    return this.lang.isAr()
      ? this.L('Switch to English', 'غير للإنجليزي')
      : this.L('Switch to Arabic', 'غير للعربي');
  }

  @HostListener('window:scroll')
  onScroll(): void { this.isScrolled = window.scrollY > 14; }

  go(id: string, route?: string): void {
    this.activeId = id;
    this.mobileOpen = false;

    // Links with a route navigate to a real page; the rest scroll within home.
    if (route) {
      this.router.navigateByUrl(route);
      return;
    }

    const scroll = () => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    if (this.router.url.split('?')[0] !== '/') {
      this.router.navigate(['/']).then(() => setTimeout(scroll, 120));
    } else {
      scroll();
    }
  }

  goBrand(): void {
    this.mobileOpen = false;
    // Signed-in users go to their role home (shopper -> discover); guests get
    // the public landing hero.
    if (this.auth.isLoggedIn()) {
      this.router.navigateByUrl(this.auth.homeRouteForRole(this.auth.role()));
      return;
    }
    this.go('hero');
  }

  goCart(): void {
    this.mobileOpen = false;
    this.cartDrawer.open();
  }
  goLogin(): void { this.mobileOpen = false; this.router.navigate(['/login']); }

  toggleTheme(): void {
    this.mobileOpen = false;
    this.theme.toggle();
  }

  toggleLanguage(): void {
    this.mobileOpen = false;
    this.lang.toggle();
  }

  // Account button: shoppers go to their profile, business roles to their
  // dashboard, and guests to login.
  goAccount(): void {
    this.mobileOpen = false;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    if (this.auth.isShopper()) {
      this.router.navigateByUrl('/profile');
      return;
    }
    this.router.navigateByUrl(this.auth.homeRouteForRole(this.auth.role()));
  }

  logout(): void {
    this.mobileOpen = false;
    this.auth.logout();
    // replaceUrl so pressing back can't return to the protected page we left.
    this.router.navigate(['/login'], { replaceUrl: true });
  }

  toggleMenu(): void { this.mobileOpen = !this.mobileOpen; }

  private syncRouteState(): void {
    this.isScrolled = window.scrollY > 14;
    const path = this.router.url.split('?')[0].split('#')[0] || '/';
    this.isHomeRoute = path === '/';

    if (this.isHomeRoute) {
      const active = this.visibleLinks().find(link => link.id === this.activeId);
      if (!active || active.route) this.activeId = 'hero';
      return;
    }

    const routeLink = this.visibleLinks().find(link =>
      link.route && (path === link.route || path.startsWith(`${link.route}/`)),
    );
    this.activeId = routeLink?.id ?? '';
  }
}
