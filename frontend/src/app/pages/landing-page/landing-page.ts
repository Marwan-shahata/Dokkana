import {Component,
  OnInit,
  HostListener,
  signal,
  computed, } from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Title } from '@angular/platform-browser';

export interface Product {
   id: number;
  name: string;
  category: string;
  store: string;
  price: number;
  oldPrice?: number;
  currency: string;
  rating: number;
  reviews: number;
  badge?: string;
  gradient: string;
  isFav: boolean;
}

export interface Category {
  id: string;
  name: string;
  count: number;
  gradient: string;
  icon: string;
}

export interface Feature {
  icon: string;
  title: string;
  desc: string;
  accent: string;
}

export interface Testimonial {
  name: string;
  role: string;
  avatar: string;
  rating: number;
  text: string;
  initials: string;
  color: string;
}



@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage implements OnInit {
  constructor(private sanitizer: DomSanitizer, private title: Title) {}

  safeSvg(svg: string): SafeHtml {
  return this.sanitizer.bypassSecurityTrustHtml(svg);
}
    isScrolled = false;
  isMobileOpen = false;
  cartCount = signal(0);
  activeProductTab = 'All';
  activeTestimonial = 0;
  newsletterEmail = '';
  newsletterState: 'idle' | 'success' | 'error' = 'idle';

  readonly navLinks = [
    { label: 'Home',      id: 'hero' },
    { label: 'Shop',      id: 'products' },
    { label: 'Categories',id: 'categories' },
    { label: 'About',     id: 'about' },
  ];

  readonly productTabs = ['All', 'New', 'Sale', 'Trending'];

  readonly features: Feature[] = [
    {
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12H3l9-9 9 9h-2M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7"/><path d="M9 21v-6h6v6"/></svg>`,
      title: 'Free Delivery',
      desc: 'Complimentary shipping on all orders above 500 EGP. Fast, tracked, and insured.',
      accent: 'var(--nile)',
    },
    {
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>`,
      title: 'Secure Payment',
      desc: 'Bank-grade encryption on every transaction. Multiple payment methods accepted.',
      accent: 'var(--clay)',
    },
    {
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>`,
      title: 'Curated Quality',
      desc: 'Every product hand-selected by our editorial team for craftsmanship and style.',
      accent: 'var(--ochre)',
    },
    {
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/></svg>`,
      title: '24/7 Support',
      desc: 'Our concierge team is always available to help via chat, call, or WhatsApp.',
      accent: 'var(--hibiscus)',
    },
  ];

  readonly categories: Category[] = [
    {
      id: 'dresses', name: 'Dresses', count: 218,
      gradient: 'linear-gradient(140deg, oklch(0.62 0.15 42/0.85), oklch(0.5 0.12 39/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><path d="M12 2L8 7l-4 1 2 12h12l2-12-4-1-4-5z"/><path d="M8 7c0 2.2 1.8 4 4 4s4-1.8 4-4"/></svg>`,
    },
    {
      id: 'shoes', name: 'Shoes', count: 156,
      gradient: 'linear-gradient(140deg, oklch(0.55 0.08 220/0.85), oklch(0.42 0.07 210/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><path d="M2 18h20v2H2zM3 18l2-8h4l2 4h6l2 4"/><path d="M9 10V6l3-2 3 2v4"/></svg>`,
    },
    {
      id: 'bags', name: 'Bags', count: 94,
      gradient: 'linear-gradient(140deg, oklch(0.65 0.1 58/0.85), oklch(0.5 0.12 48/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 01-8 0"/></svg>`,
    },
    {
      id: 'shirts', name: 'Shirts', count: 173,
      gradient: 'linear-gradient(140deg, oklch(0.72 0.06 195/0.85), oklch(0.58 0.08 200/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><path d="M20.38 3.46L16 2a4 4 0 01-8 0L3.62 3.46a2 2 0 00-1.34 2.23l.58 3.57a1 1 0 00.99.84H6v10c0 1.1.9 2 2 2h8a2 2 0 002-2V10h2.15a1 1 0 00.99-.84l.58-3.57a2 2 0 00-1.34-2.23z"/></svg>`,
    },
    {
      id: 'accessories', name: 'Accessories', count: 87,
      gradient: 'linear-gradient(140deg, oklch(0.54 0.17 22/0.85), oklch(0.44 0.14 20/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M12 1v4M12 19v4M4.22 4.22l2.83 2.83M16.95 16.95l2.83 2.83M1 12h4M19 12h4M4.22 19.78l2.83-2.83M16.95 7.05l2.83-2.83"/></svg>`,
    },
    {
      id: 'sportswear', name: 'Sportswear', count: 62,
      gradient: 'linear-gradient(140deg, oklch(0.42 0.08 148/0.85), oklch(0.34 0.07 152/0.95))',
      icon: `<svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.5" stroke-linecap="round"><circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/></svg>`,
    },
  ];

  readonly allProducts: Product[] = [
    { id:1, name:'Linen Summer Dress', category:'Dresses', store:'Boutique El Nour', price:480, currency:'EGP', rating:4.9, reviews:128, badge:'New', isFav:false, gradient:'linear-gradient(135deg, oklch(0.62 0.15 42/0.6), oklch(0.5 0.12 39/0.75))' },
    { id:2, name:'Canvas Sneakers', category:'Shoes', store:'Step & Style', price:650, oldPrice:820, currency:'EGP', rating:4.6, reviews:94, badge:'Sale', isFav:false, gradient:'linear-gradient(135deg, oklch(0.55 0.08 220/0.55), oklch(0.42 0.07 210/0.7))' },
    { id:3, name:'Tan Leather Tote', category:'Bags', store:'Cairo Craft', price:920, currency:'EGP', rating:5.0, reviews:57, badge:'Hot', isFav:true, gradient:'linear-gradient(135deg, oklch(0.68 0.1 55/0.6), oklch(0.55 0.12 45/0.75))' },
    { id:4, name:'Egyptian Cotton Shirt', category:'Shirts', store:'Delta Weave', price:340, currency:'EGP', rating:4.4, reviews:211, isFav:false, gradient:'linear-gradient(135deg, oklch(0.72 0.06 195/0.55), oklch(0.58 0.08 200/0.7))' },
    { id:5, name:'Floral Midi Dress', category:'Dresses', store:'Reem Fashion', price:560, oldPrice:720, currency:'EGP', rating:4.8, reviews:183, badge:'Sale', isFav:false, gradient:'linear-gradient(135deg, oklch(0.64 0.14 25/0.6), oklch(0.5 0.12 22/0.75))' },
    { id:6, name:'Suede Ankle Boots', category:'Shoes', store:'Step & Style', price:1100, currency:'EGP', rating:4.5, reviews:76, badge:'Trending', isFav:false, gradient:'linear-gradient(135deg, oklch(0.38 0.04 50/0.75), oklch(0.3 0.03 45/0.85))' },
    { id:7, name:'Woven Crossbody', category:'Bags', store:'Cairo Craft', price:430, currency:'EGP', rating:4.7, reviews:142, badge:'New', isFav:false, gradient:'linear-gradient(135deg, oklch(0.6 0.1 65/0.6), oklch(0.48 0.1 58/0.75))' },
    { id:8, name:'Linen Blend Shirt', category:'Shirts', store:'Delta Weave', price:295, currency:'EGP', rating:4.3, reviews:98, badge:'New', isFav:false, gradient:'linear-gradient(135deg, oklch(0.75 0.04 100/0.55), oklch(0.62 0.05 95/0.7))' },
  ];

  readonly testimonials: Testimonial[] = [
    { name:'Sara M.', role:'Fashion Blogger · Cairo', rating:5, initials:'SM', color:'var(--clay)', avatar:'', text:'Dokkana completely changed how I shop. I described exactly what I wanted and found it within minutes from a boutique literally around the corner. The quality was exceptional.' },
    { name:'Ahmed K.', role:'Architect · Alexandria', rating:5, initials:'AK', color:'var(--nile)', avatar:'', text:'I was skeptical at first, but the AI search is genuinely impressive. It understood "something minimal and structured for meetings" perfectly. My wardrobe has leveled up.' },
    { name:'Nadia R.', role:'Designer · Giza', rating:5, initials:'NR', color:'var(--ochre)', avatar:'', text:'The reserve-and-pickup feature is genius. No delivery anxiety, no waiting — I find it, reserve it, pick it up the same day. This is the future of local shopping.' },
    { name:'Omar F.', role:'Engineer · New Cairo', rating:4, initials:'OF', color:'var(--clay-deep)', avatar:'', text:'Excellent curation and the bilingual search works flawlessly. I searched in Egyptian Arabic and got perfectly matched results instantly. Very impressive platform.' },
  ];

  get filteredProducts(): Product[] {
    switch (this.activeProductTab) {
      case 'New':      return this.allProducts.filter(p => p.badge === 'New');
      case 'Sale':     return this.allProducts.filter(p => p.badge === 'Sale');
      case 'Trending': return this.allProducts.filter(p => p.badge === 'Trending' || p.badge === 'Hot');
      default:         return this.allProducts;
    }
  }

  starsArray(n: number): boolean[] {
    return Array(5).fill(false).map((_, i) => i < Math.round(n));
  }

  ngOnInit(): void {
    this.title.setTitle('Dokana | Home Page');
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled = window.scrollY > 16;
  }

  scrollTo(id: string): void {
    this.isMobileOpen = false;
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  addToCart(p: Product, e: Event): void {
    e.stopPropagation();
    this.cartCount.update(v => v + 1);
  }

  toggleFav(p: Product, e: Event): void {
    e.stopPropagation();
    p.isFav = !p.isFav;
  }

  prevTestimonial(): void {
    this.activeTestimonial = (this.activeTestimonial - 1 + this.testimonials.length) % this.testimonials.length;
  }

  nextTestimonial(): void {
    this.activeTestimonial = (this.activeTestimonial + 1) % this.testimonials.length;
  }

  setTestimonial(i: number): void {
    this.activeTestimonial = i;
  }

  onNewsletterSubmit(): void {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.newsletterEmail)) {
      this.newsletterState = 'error';
      setTimeout(() => this.newsletterState = 'idle', 3000);
      return;
    }
    this.newsletterState = 'success';
    this.newsletterEmail = '';
    setTimeout(() => this.newsletterState = 'idle', 4500);
  }

  readonly footerLinks = {
    shop:    ['Dresses', 'Shoes', 'Bags', 'Shirts', 'Accessories'],
    company: ['About Us', 'Careers', 'Press', 'Blog'],
    support: ['Help Center', 'Shipping Info', 'Returns', 'Privacy Policy', 'Terms'],
  };

  readonly socialLinks = [
    { name:'Instagram', href:'#', icon:`<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><rect x="2" y="2" width="20" height="20" rx="5"/><circle cx="12" cy="12" r="4"/><circle cx="17.5" cy="6.5" r="0.5" fill="currentColor"/></svg>` },
    { name:'Facebook',  href:'#', icon:`<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M18 2h-3a5 5 0 00-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 011-1h3z"/></svg>` },
    { name:'X',         href:'#', icon:`<svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>` },
    { name:'WhatsApp',  href:'#', icon:`<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M21 11.5a8.38 8.38 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.38 8.38 0 01-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 01-.9-3.8 8.5 8.5 0 014.7-7.6 8.38 8.38 0 013.8-.9h.5a8.48 8.48 0 018 8v.5z"/></svg>` },
  ];

  readonly currentYear = new Date().getFullYear();
}


