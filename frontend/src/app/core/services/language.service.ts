import { Injectable, signal, effect } from '@angular/core';

export type Lang = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  /** Reactive current language. */
  readonly lang = signal<Lang>(this.read());

  constructor() {
    // Keep <html dir/lang> and storage in sync whenever the language changes.
    effect(() => {
      this.apply(this.lang());
    });
  }

  toggle(): void {
    const next = this.lang() === 'en' ? 'ar' : 'en';
    this.lang.set(next);
    this.apply(next);
  }

  isAr(): boolean { return this.lang() === 'ar'; }

  /** Pick the right string for the current language. */
  t(en: string, ar: string): string {
    return this.lang() === 'ar' ? ar : en;
  }

  private read(): Lang {
    try {
      const stored = localStorage.getItem('dokkana-lang');
      return stored === 'ar' || stored === 'en' ? stored : 'en';
    } catch {
      return 'en';
    }
  }

  private apply(lang: Lang): void {
    const html = document.documentElement;
    html.setAttribute('lang', lang);
    html.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');
    try { localStorage.setItem('dokkana-lang', lang); } catch {}
  }
}
