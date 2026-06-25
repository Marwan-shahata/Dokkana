import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';

// A footer link is either an internal route (rendered with routerLink) or an
// external URL (rendered as an anchor that opens in a new tab). We deliberately
// avoid dead buttons that look clickable but do nothing.
interface FooterLink {
  en: string;
  ar: string;
  route?: string;
  href?: string;
}

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  lang = inject(LanguageService);
  readonly year = new Date().getFullYear();

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  readonly cols: { h: [string, string]; links: FooterLink[] }[] = [
    {
      h: ['Explore', 'استكشف'],
      links: [
        { en: 'Local stores', ar: 'محلات قريبة منك', route: '/stores' },
        { en: 'Sign in', ar: 'تسجيل الدخول', route: '/login' },
        { en: 'Create account', ar: 'إنشاء حساب', route: '/signup' },
      ],
    },
    {
      h: ['For stores', 'لأصحاب المحلات'],
      links: [
        { en: 'Open your store', ar: 'افتح محلك', route: '/signup' },
        { en: 'Merchant portal', ar: 'بوابة صاحب المحل', route: '/merchant' },
      ],
    },
    {
      h: ['Contact', 'تواصل'],
      links: [
        { en: 'support@dokkana.app', ar: 'support@dokkana.app', href: 'mailto:support@dokkana.app' },
      ],
    },
  ];
}
