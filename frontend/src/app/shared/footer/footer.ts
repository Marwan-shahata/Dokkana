import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  email = '';
  subscribeState: 'idle' | 'success' | 'error' = 'idle';

  readonly currentYear = new Date().getFullYear();

  quickLinks = [
    { label: 'Home', href: '#home' },
    { label: 'Shop', href: '#shop' },
    { label: 'Stores', href: '#stores' },
    { label: 'How It Works', href: '#how-it-works' },
  ];

  serviceLinks = [
    { label: 'Contact Us', href: '#contact' },
    { label: 'FAQs', href: '#faqs' },
    { label: 'Shipping & Returns', href: '#shipping' },
    { label: 'Privacy Policy', href: '#privacy' },
  ];

  socials: { name: string; href: string; icon: SafeHtml }[] = [];

  constructor(private sanitizer: DomSanitizer) {
    this.socials = [
      {
        name: 'Instagram',
        href: 'https://instagram.com',
        icon: this.sanitizer.bypassSecurityTrustHtml(`
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
          <rect width="20" height="20" x="2" y="2" rx="5" ry="5"/>
          <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/>
          <line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/>
          </svg>
        `)
      },
      {
        name: 'Facebook',
        href: 'https://facebook.com',
        icon: this.sanitizer.bypassSecurityTrustHtml(`
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
          <path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"/>
          </svg>
        `)
      },
      {
        name: 'X (Twitter)',
        href: 'https://x.com',
        icon: this.sanitizer.bypassSecurityTrustHtml(`
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
          <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
          </svg>
        `)
      },
    ];
  }

  onSubscribe(): void {
    if (!this.email || !this.isValidEmail(this.email)) {
      this.subscribeState = 'error';
      setTimeout(() => (this.subscribeState = 'idle'), 2800);
      return;
    }

    this.subscribeState = 'success';
    this.email = '';
    setTimeout(() => (this.subscribeState = 'idle'), 4000);
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}