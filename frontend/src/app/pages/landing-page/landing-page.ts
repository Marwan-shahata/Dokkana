import { Component, ElementRef, AfterViewInit, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DomSanitizer, SafeHtml, Title } from '@angular/platform-browser';
import { LanguageService } from '../../core/services/language.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { AuthService } from '../../core/services/auth.service';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { AiSearchService } from '../../core/services/ai-search.service';
import { ProductListItem } from '../../core/models/catalog.models';

interface Product {
  id: number; name: string; nameAr: string; cat: string;
  store: string; price: number;
  rating: number;
  icon: 'dress' | 'shirt' | 'sneaker' | 'bag';
  gradient: string; image: string; fav: boolean;
  fromApi?: boolean;
}

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage implements OnInit, AfterViewInit {
  private sanitizer = inject(DomSanitizer);
  private title = inject(Title);
  private host = inject(ElementRef<HTMLElement>);
  private router = inject(Router);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private aiSearch = inject(AiSearchService);
  private cartApi = inject(CartApiService);
  private auth = inject(AuthService);
  lang = inject(LanguageService);
  cart = inject(CartService);

  query = '';
  activeTab = 'All';
  aiTags = signal<string[]>([]);
  aiAnswer = signal('');     // natural-language answer from the AI endpoint
  searching = signal(false); // AI search in-flight
  toastMsg = signal('');

  // Live data from the backend. When empty (API down / no data) we fall back to
  // the demo content below so the landing page always looks complete.
  readonly apiProducts = signal<Product[]>([]);
  readonly apiTabs = signal<{ id: string; en: string; ar: string }[]>([]);
  readonly loading = signal(true);
  get usingApi(): boolean { return this.apiProducts().length > 0; }

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  canUseShopperActions(): boolean {
    return !this.auth.isLoggedIn() || this.auth.isShopper();
  }

  // Static demo tabs - used as a fallback until categories load from the API.
  readonly demoTabs = [
    { id: 'All', en: 'All', ar: 'الكل' },
    { id: 'Dresses', en: 'Dresses', ar: 'فساتين' },
    { id: 'Shoes', en: 'Shoes', ar: 'أحذية' },
    { id: 'Bags', en: 'Bags', ar: 'شنط' },
    { id: 'Shirts', en: 'Shirts', ar: 'قمصان' },
  ];

  get tabs(): { id: string; en: string; ar: string }[] {
    return this.apiTabs().length > 0 ? this.apiTabs() : this.demoTabs;
  }

  readonly chips = [
    { q: 'dress', label: 'summer dress', ar: false },
    { q: 'sneaker', label: 'white sneakers', ar: false },
    { q: 'bag', label: 'شنطة جلد', ar: true },
    { q: 'shirt', label: 'قميص كتان', ar: true },
  ];

  readonly features = [
    {
      en: 'AI natural search', ar: 'بحث ذكي طبيعي',
      enD: 'Type or speak in Egyptian Arabic or English. The AI reads style, color, and budget.',
      arD: 'اكتب أو قول بالعامية المصرية أو الإنجليزي. الذكاء الاصطناعي بيفهم الستايل واللون والميزانية.',
      icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4"/></svg>`,
    },
    {
      en: 'Real local stock', ar: 'مخزون محلي حقيقي',
      enD: 'Results come from real catalogs of nearby shops, with live prices and availability.',
      arD: 'النتائج من كتالوجات حقيقية لمحلات قريبة، بأسعار وتوافر مباشر.',
      icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/></svg>`,
    },
    {
      en: 'Reserve & pick up', ar: 'احجز واستلم',
      enD: 'Reserve an item, get a code, and collect it at the shop. Or chat the store on WhatsApp.',
      arD: 'احجز منتج، خد كود، واستلمه من المحل. أو كلّم المحل على واتساب.',
      icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 7l-8-4-8 4 8 4 8-4z"/><path d="M4 7v10l8 4 8-4V7"/></svg>`,
    },
  ];

  readonly steps = [
    { n: '1', en: 'Describe it your way', ar: 'اوصفه بطريقتك', enD: 'Natural language, photo, or voice in Arabic or English.', arD: 'لغة طبيعية، صورة، أو صوت بالعربي أو الإنجليزي.' },
    { n: '2', en: "See what's in stock nearby", ar: 'شوف المتوفر قريب منك', enD: 'Live results from real local catalogs, ask the catalog questions.', arD: 'نتائج مباشرة من كتالوجات محلية حقيقية، واسأل الكتالوج.' },
    { n: '3', en: 'Reserve, chat, or visit', ar: 'احجز، كلّم، أو زور', enD: 'Reserve for pickup, message on WhatsApp, or get directions.', arD: 'احجز للاستلام، كلّم على واتساب، أو خد الاتجاهات.' },
  ];

  readonly demoProducts: Product[] = [
    { id: 1, name: 'Summer Wrap Dress', nameAr: 'فستان صيفي ملفوف', cat: 'Dresses', store: 'Layla Atelier', price: 480, rating: 5, icon: 'dress', gradient: 'linear-gradient(150deg, oklch(0.56 0.17 22), oklch(0.36 0.11 22))', image: '/hero.jpg', fav: false },
    { id: 2, name: 'Linen Button Shirt', nameAr: 'قميص كتان', cat: 'Shirts', store: 'Boutique El Nour', price: 320, rating: 5, icon: 'shirt', gradient: 'linear-gradient(150deg, oklch(0.64 0.155 45), oklch(0.42 0.1 45))', image: '/about.png', fav: false },
    { id: 3, name: 'White Court Sneakers', nameAr: 'كوتشي أبيض', cat: 'Shoes', store: 'Cairo Kicks', price: 650, rating: 4, icon: 'sneaker', gradient: 'linear-gradient(150deg, oklch(0.61 0.085 195), oklch(0.4 0.06 195))', image: '/ecommerce-store.png', fav: false },
    { id: 4, name: 'Brown Leather Tote', nameAr: 'شنطة جلد بني', cat: 'Bags', store: 'Zahra Goods', price: 540, rating: 5, icon: 'bag', gradient: 'linear-gradient(150deg, oklch(0.52 0.14 42), oklch(0.34 0.09 42))', image: '/shop.png', fav: false },
    { id: 5, name: 'Floral Day Dress', nameAr: 'فستان مزهّر', cat: 'Dresses', store: 'Layla Atelier', price: 390, rating: 4, icon: 'dress', gradient: 'linear-gradient(150deg, oklch(0.64 0.14 25), oklch(0.42 0.1 25))', image: '/hero.jpg', fav: false },
    { id: 6, name: 'Oxford Cotton Shirt', nameAr: 'قميص قطن', cat: 'Shirts', store: 'Boutique El Nour', price: 410, rating: 4, icon: 'shirt', gradient: 'linear-gradient(150deg, oklch(0.72 0.06 195), oklch(0.5 0.06 200))', image: '/about.png', fav: false },
    { id: 7, name: 'Suede Crossbody', nameAr: 'شنطة شامواه', cat: 'Bags', store: 'Zahra Goods', price: 470, rating: 5, icon: 'bag', gradient: 'linear-gradient(150deg, oklch(0.6 0.1 65), oklch(0.42 0.09 58))', image: '/shop.png', fav: false },
    { id: 8, name: 'Runner Sneakers', nameAr: 'كوتشي رياضي', cat: 'Shoes', store: 'Cairo Kicks', price: 720, rating: 4, icon: 'sneaker', gradient: 'linear-gradient(150deg, oklch(0.55 0.06 250), oklch(0.38 0.05 250))', image: '/ecommerce-store.png', fav: false },
  ];

  private readonly icons: Record<string, string> = {
    dress: '<path d="M9 2h6l-1 4 3 4-2 2 2 9H7l2-9-2-2 3-4-1-4z"/>',
    shirt: '<path d="M6 3l3-1 3 2 3-2 3 1 2 5-3 1v9H7v-9L4 8z"/>',
    sneaker: '<path d="M2 16l2-7 4 2 3-3 2 1v3l7 2c1 .3 2 1 2 2v2H2z"/>',
    bag: '<path d="M5 8h14l-1 13H6L5 8z"/><path d="M9 8V6a3 3 0 016 0v2"/>',
  };

  garment(type: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(
      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round">${this.icons[type]}</svg>`
    );
  }
  svg(html: string): SafeHtml { return this.sanitizer.bypassSecurityTrustHtml(html); }

  // Source = live API products when available, otherwise the demo set.
  get source(): Product[] {
    return this.usingApi ? this.apiProducts() : this.demoProducts;
  }

  get filtered(): Product[] {
    return this.activeTab === 'All' ? this.source : this.source.filter(p => p.cat === this.activeTab);
  }

  // Switching tabs renders a different set of cards; reveal the new ones.
  selectTab(id: string): void {
    this.activeTab = id;
    this.revealAfterRender();
  }

  catLabel(cat: string): string {
    const tab = this.tabs.find(t => t.id === cat);
    return tab ? this.L(tab.en, tab.ar) : cat;
  }

  stars(n: number): boolean[] { return Array(5).fill(0).map((_, i) => i < n); }

  ngOnInit(): void {
    this.title.setTitle(this.L('Dokkana · Local fashion, searchable', 'دكانة · أزياء محلية قابلة للبحث'));
    this.loadCategories();
    this.loadProducts();
  }

  private loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: cats => {
        if (cats.length) {
          this.apiTabs.set([
            { id: 'All', en: 'All', ar: 'الكل' },
            ...cats.map(c => ({ id: c.name, en: c.name, ar: c.name })),
          ]);
        }
      },
      error: () => {}, // keep demo tabs on failure
    });
  }

  private loadProducts(): void {
    this.loading.set(true);
    this.productService.getAll({ page: 1, pageSize: 48 }).subscribe({
      next: page => {
        this.apiProducts.set(page.items.map((p, i) => this.toCard(p, i)));
        this.loading.set(false);
        this.revealAfterRender();
      },
      error: () => this.loading.set(false), // demo fallback stays
    });
  }

  // Maps a backend ProductListItem into the rich card shape the template uses,
  // synthesizing the demo-only visuals (gradient/icon) the API doesn't provide.
  private readonly gradients = [
    'linear-gradient(150deg, oklch(0.56 0.17 22), oklch(0.36 0.11 22))',
    'linear-gradient(150deg, oklch(0.64 0.155 45), oklch(0.42 0.1 45))',
    'linear-gradient(150deg, oklch(0.61 0.085 195), oklch(0.4 0.06 195))',
    'linear-gradient(150deg, oklch(0.52 0.14 42), oklch(0.34 0.09 42))',
  ];

  private toCard(p: ProductListItem, index: number): Product {
    return {
      id: p.id,
      name: p.title,
      nameAr: p.title,
      cat: p.categoryName,
      store: p.storeName,
      price: p.price,
      rating: 5,
      icon: this.iconFor(p.categoryName),
      gradient: this.gradients[index % this.gradients.length],
      image: p.imageUrl ?? '',
      fav: false,
      fromApi: true,
    };
  }

  private iconFor(category: string): Product['icon'] {
    const c = category.toLowerCase();
    if (c.includes('dress')) return 'dress';
    if (c.includes('shoe') || c.includes('sneaker') || c.includes('activ')) return 'sneaker';
    if (c.includes('bag')) return 'bag';
    return 'shirt';
  }

  openProduct(p: Product, e?: Event): void {
    e?.stopPropagation();
    // Only navigate for real backend products; demo cards have no detail page.
    if (p.fromApi) this.router.navigate(['/products', p.id]);
  }

  // Scroll-reveal observer for static content blocks.
  private io?: IntersectionObserver;

  ngAfterViewInit(): void {
    this.io = new IntersectionObserver((entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) { e.target.classList.add('in'); this.io!.unobserve(e.target); }
      });
    }, { threshold: 0.12 });
    this.observeReveals();
  }

  // Observe any `.reveal` element not yet revealed. Safe to call repeatedly.
  private observeReveals(): void {
    if (!this.io) return;
    this.host.nativeElement
      .querySelectorAll('.reveal:not(.in)')
      .forEach((el: Element) => this.io!.observe(el));
  }

  // Observe reveal blocks after async data renders.
  private revealAfterRender(): void {
    setTimeout(() => this.observeReveals());
  }

  // AI search
  private readonly rec: Record<string, { cat: string; en: string[]; ar: string[] }> = {
    dress: { cat: 'Dresses', en: ['Dress', 'Summer', '< 500'], ar: ['فستان', 'صيفي', '< ٥٠٠'] },
    sneaker: { cat: 'Shoes', en: ['Sneakers', 'White', 'Open now'], ar: ['كوتشي', 'أبيض', 'مفتوح'] },
    bag: { cat: 'Bags', en: ['Bag', 'Leather', 'Brown'], ar: ['شنطة', 'جلد', 'بني'] },
    shirt: { cat: 'Shirts', en: ['Shirt', 'Cotton'], ar: ['قميص', 'قطن'] },
  };

  searchFromChip(q: string, label: string): void { this.query = label; this.search(); }

  search(): void {
    const query = this.query.trim();
    this.setVisualTags();

    if (!query) {
      this.aiAnswer.set('');
      this.loadProducts();
      this.scrollToTrending();
      return;
    }

    // Real AI-powered search: the backend returns a natural-language answer plus
    // ranked product matches. On failure we fall back to plain product search.
    this.searching.set(true);
    this.aiSearch.search({ query }).subscribe({
      next: res => {
        this.searching.set(false);
        this.aiAnswer.set(res.answer ?? '');
        if (res.products.length) {
          this.apiProducts.set(res.products.map((p, i) => this.toCard(p, i)));
          this.activeTab = 'All';
        }
        this.revealAfterRender();
        this.scrollToTrending();
      },
      error: () => {
        this.searching.set(false);
        // Fallback to direct product search if the AI endpoint is unavailable.
        this.productService.getAll({ search: query, page: 1, pageSize: 48 }).subscribe({
          next: page => {
            if (page.items.length) {
              this.apiProducts.set(page.items.map((p, i) => this.toCard(p, i)));
              this.activeTab = 'All';
            }
            this.revealAfterRender();
            this.scrollToTrending();
          },
          error: () => this.scrollToTrending(),
        });
      },
    });
  }

  // Sets the small "AI understood" tag banner based on the query keywords.
  private setVisualTags(): void {
    const v = this.query.toLowerCase();
    let k = 'dress';
    if (v.match(/sneaker|shoe|كوتش|حذاء/)) k = 'sneaker';
    else if (v.match(/bag|شنط/)) k = 'bag';
    else if (v.match(/shirt|قميص/)) k = 'shirt';
    const r = this.rec[k] || this.rec['dress'];
    this.aiTags.set(this.lang.isAr() ? r.ar : r.en);
  }

  private scrollToTrending(): void {
    setTimeout(() => document.getElementById('trending')?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 80);
  }

  // interactions
  addToCart(p: Product, e: Event): void {
    e.stopPropagation();
    if (!p.fromApi) {
      this.showToast(this.L('Start the API to reserve real products', 'شغّل الـ API لحجز المنتجات الحقيقية'));
      return;
    }

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (!this.auth.isShopper()) {
      this.showToast(this.L('Shopper accounts only can reserve items', 'حسابات المتسوقين فقط يمكنها الحجز'));
      return;
    }

    this.cartApi.addItem(p.id).subscribe({
      next: res => {
        if (res.success && res.data) {
          this.cart.setFromCart(res.data);
          this.showToast(this.L('Added to reservation', 'تمت الإضافة للحجز'));
        } else {
          this.showToast(res.message || this.L('Could not add item', 'تعذّرت إضافة المنتج'));
        }
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not add item', 'تعذّرت إضافة المنتج')),
    });
  }

  showCatalogToast(): void {
    this.showToast(this.L('Demo: full catalog page', 'ديمو: صفحة الكتالوج الكاملة'));
  }

  toggleFav(p: Product, e: Event): void { e.stopPropagation(); p.fav = !p.fav; }

  onImgError(e: Event): void { (e.target as HTMLElement).style.display = 'none'; }

  private toastTimer: any;
  private showToast(msg: string): void {
    this.toastMsg.set(msg);
    clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toastMsg.set(''), 2200);
  }
}
