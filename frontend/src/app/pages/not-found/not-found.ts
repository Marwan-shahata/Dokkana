import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/services/auth.service';

// Friendly catch-all for unknown routes. Sends the user back to a real place
// (their role home, or the public landing) instead of a blank screen.
@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.css',
})
export class NotFound {
  private auth = inject(AuthService);
  lang = inject(LanguageService);

  constructor() {
    inject(Title).setTitle(this.L('Dokkana | Page not found', 'دكانة | الصفحة غير موجودة'));
  }

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  homeRoute(): string {
    return this.auth.isLoggedIn() ? this.auth.homeRouteForRole(this.auth.role()) : '/';
  }
}
