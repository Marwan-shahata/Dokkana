import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { trigger, transition, style, animate } from '@angular/animations';
import { Navbar } from './shared/navbar/navbar';
import { Footer } from './shared/footer/footer';
import { Cart } from './pages/cart/cart';
import { NoticeService } from './core/services/notice.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, Navbar, Footer, Cart],
  templateUrl: './app.html',
  styleUrl: './app.css',
  animations: [
    trigger('routeAnim', [
      // Auth pages use a fixed full-screen stage; a transform on this host would
      // become its containing block and trap it. So skip the transform for auth
      // (both entering it and toggling login<->signup share the 'auth' key).
      transition('* => auth', []),
      transition('* => *', [
        style({ opacity: 0, transform: 'translateY(14px)' }),
        animate('0.45s cubic-bezier(.16,1,.3,1)', style({ opacity: 1, transform: 'none' })),
      ]),
    ]),
  ],
})
export class App {
  readonly notice = inject(NoticeService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  // The admin dashboard owns its full chrome (sidebar + topbar + logout) and
  // the auth pages are a full-screen takeover, so the global navbar/footer
  // should not render over them. (The merchant portal has no chrome of its own
  // and relies on the global navbar, so it is intentionally excluded.)
  readonly chromeless = signal(this.isChromeless(this.router.url));

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(e => this.chromeless.set(this.isChromeless(e.urlAfterRedirects)));
  }

  private isChromeless(url: string): boolean {
    const path = url.split('?')[0].split('#')[0];
    return path.startsWith('/admin') || path === '/login' || path === '/signup';
  }

  // Returns a key that changes per navigation so the enter animation re-runs.
  // Login & signup share the 'auth' key so neither transition animates the host.
  routeState(outlet: RouterOutlet): unknown {
    if (!outlet?.isActivated) return 'void';
    const path = outlet.activatedRoute.snapshot.routeConfig?.path ?? '';
    return path === 'login' || path === 'signup' ? 'auth' : path || 'root';
  }
}
