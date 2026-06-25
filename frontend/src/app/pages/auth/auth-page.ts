import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { trigger, transition, style, animate } from '@angular/animations';
import { LoginComponent } from '../../shared/components/login/login';
import { SignUpComponent } from '../../shared/components/sign-up/sign-up';
import { LanguageService } from '../../core/services/language.service';
import { ThemeService } from '../../core/services/theme.service';

type AuthMode = 'login' | 'signup';

// Single full-page auth surface. Owns the branded background once and morphs
// between the Sign In and Sign Up cards in place - so toggling never swaps a
// route (no flash, no jump) and only the card cross-fades.
@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [CommonModule, LoginComponent, SignUpComponent],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.css',
  animations: [
    trigger('cardSwap', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(16px) scale(0.985)' }),
        animate('0.42s cubic-bezier(.16,1,.3,1)', style({ opacity: 1, transform: 'none' })),
      ]),
      transition(':leave', [
        animate('0.3s cubic-bezier(.16,1,.3,1)', style({ opacity: 0, transform: 'translateY(-12px) scale(0.985)' })),
      ]),
    ]),
  ],
})
export class AuthPage {
  private route = inject(ActivatedRoute);
  private location = inject(Location);
  private router = inject(Router);
  private title = inject(Title);
  readonly lang = inject(LanguageService);
  readonly theme = inject(ThemeService);

  readonly mode = signal<AuthMode>(
    this.route.snapshot.data['mode'] === 'signup' ? 'signup' : 'login',
  );

  constructor() {
    effect(() => {
      const isSignup = this.mode() === 'signup';
      this.title.setTitle(
        this.lang.t(
          isSignup ? 'Dokkana | Create account' : 'Dokkana | Sign in',
          isSignup ? 'دكانة | إنشاء حساب' : 'دكانة | تسجيل الدخول',
        ),
      );
    });
  }

  // Flip the card and keep the address bar in sync without re-running the
  // router (which would reload the component and kill the animation).
  switch(to: AuthMode): void {
    if (this.mode() === to) return;
    this.mode.set(to);
    const returnUrl = this.safeReturnUrl();
    const url = `/${to}` + (returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '');
    this.location.replaceState(url);
  }

  goHome(): void {
    this.router.navigateByUrl('/');
  }

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  themeLabel(): string {
    return this.theme.isDark()
      ? this.L('Switch to light theme', 'التبديل إلى السمة الفاتحة')
      : this.L('Switch to dark theme', 'التبديل إلى السمة الداكنة');
  }

  languageLabel(): string {
    return this.lang.isAr()
      ? this.L('Switch to English', 'التبديل إلى الإنجليزية')
      : this.L('Switch to Arabic', 'التبديل إلى العربية');
  }

  private safeReturnUrl(): string | null {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (!returnUrl) return null;
    if (!returnUrl.startsWith('/') || returnUrl.startsWith('//')) return null;
    if (/^\/(?:login|signup)(?:[/?#]|$)/.test(returnUrl)) return null;
    return returnUrl;
  }
}
