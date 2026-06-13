import { CommonModule } from '@angular/common';
import { Component,
  OnInit,
  OnDestroy,
  HostListener,
  Renderer2,
  inject, } from '@angular/core';
import { Router, RouterLink } from '@angular/router';


export type ViewMode = 'shopper' | 'store' | 'admin';
export type NavLink = 'home' | 'shop' | 'stores' | 'how-it-works';


@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements OnInit, OnDestroy {
  constructor(private router: Router) {}

  private renderer = inject(Renderer2);

  activeView: ViewMode = 'shopper';
  activeLink: NavLink = 'home';
  cartCount = 3;
  isDark = false;
  isRTL = false;
  isScrolled = false;
  isMobileMenuOpen = false;

  readonly views: { id: ViewMode; label: string; labelAr: string }[] = [
    { id: 'shopper', label: 'Shopper', labelAr: 'متسوق' },
    { id: 'store', label: 'Store', labelAr: 'متجر' },
  ];

  readonly navLinks: { id: NavLink; label: string; labelAr: string; sectionId: string }[] = [
    { id: 'home', label: 'Home', labelAr: 'الرئيسية', sectionId: 'section-home' },
    { id: 'shop', label: 'Shop', labelAr: 'تسوق', sectionId: 'section-shop' },
    { id: 'stores', label: 'Stores', labelAr: 'المتاجر', sectionId: 'section-stores' },
    { id: 'how-it-works', label: 'How it works', labelAr: 'كيف يعمل', sectionId: 'section-how' },
  ];

  ngOnInit(): void {
    const savedTheme = localStorage.getItem('dokkana-theme');
    const savedLang = localStorage.getItem('dokkana-lang');

    if (savedTheme === 'dark') {
      this.isDark = true;
      this.applyTheme();
    }
    if (savedLang === 'ar') {
      this.isRTL = true;
      this.applyLang();
    }
  }

  ngOnDestroy(): void {}

  @HostListener('window:scroll', [])
  onWindowScroll(): void {
    this.isScrolled = window.scrollY > 12;
  }

  setView(view: ViewMode): void {
    this.activeView = view;
    this.isMobileMenuOpen = false;
  }

  setActiveLink(link: NavLink): void {
    this.activeLink = link;
  }

  scrollTo(sectionId: string, linkId: NavLink): void {
    this.setActiveLink(linkId);
    this.isMobileMenuOpen = false;

      if (linkId === 'home') {
    this.router.navigate(['/']);
    return;
  }

  const el = document.getElementById(sectionId);

  if (el) {
    el.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }
  }

  toggleTheme(): void {
    this.isDark = !this.isDark;
    this.applyTheme();
    localStorage.setItem('dokkana-theme', this.isDark ? 'dark' : 'light');
  }

  toggleLanguage(): void {
    this.isRTL = !this.isRTL;
    this.applyLang();
    localStorage.setItem('dokkana-lang', this.isRTL ? 'ar' : 'en');
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  openCart(): void {
    console.log('Cart opened — cartCount:', this.cartCount);
  }

  openProfile(): void {
    console.log('Profile opened');
  }

  private applyTheme(): void {
    const html = document.documentElement;
    if (this.isDark) {
      this.renderer.setAttribute(html, 'data-theme', 'dark');
    } else {
      this.renderer.removeAttribute(html, 'data-theme');
    }
  }

  private applyLang(): void {
    const html = document.documentElement;
    if (this.isRTL) {
      this.renderer.setAttribute(html, 'dir', 'rtl');
      this.renderer.setAttribute(html, 'lang', 'ar');
    } else {
      this.renderer.setAttribute(html, 'dir', 'ltr');
      this.renderer.setAttribute(html, 'lang', 'en');
    }
  }
}
