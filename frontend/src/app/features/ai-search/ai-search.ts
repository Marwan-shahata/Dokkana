import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeHtml, Title } from '@angular/platform-browser';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { ProductService } from '../../core/services/product.service';
import { AiSearchService } from '../../core/services/ai-search.service';
import { NoticeService } from '../../core/services/notice.service';
import { MonetizationService } from '../../core/services/monetization.service';
import {
  AiProductResult,
  AiRankedResult,
  AiSearchResponse,
} from '../../core/models/catalog.models';

// One result card on the AI page. Mirrors the shape the landing page used, but
// this page is the dedicated "assistant + result explorer" the plan called for.
interface AiCard {
  id: number;
  storeId?: number;
  title: string;
  store: string;
  price: number;
  originalPrice?: number | null;
  salePrice?: number | null;
  isOnSale?: boolean;
  isSponsored?: boolean;
  promotionId?: number | null;
  image: string;
  rating: number;
  reviewCount: number;
  location: string;
  reason: string;
  icon: 'dress' | 'shirt' | 'sneaker' | 'bag';
  gradient: string;
}

@Component({
  selector: 'app-ai-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-search.html',
  styleUrl: './ai-search.css',
})
export class AiSearchPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private title = inject(Title);
  private sanitizer = inject(DomSanitizer);
  private aiSearch = inject(AiSearchService);
  private productService = inject(ProductService);
  private cartApi = inject(CartApiService);
  private notice = inject(NoticeService);
  private monetization = inject(MonetizationService);
  private auth = inject(AuthService);
  lang = inject(LanguageService);
  cart = inject(CartService);

  query = '';
  // The query the current results belong to (so follow-ups refine the right one).
  private baseQuery = '';

  readonly searching = signal(false);
  readonly hasSearched = signal(false);
  readonly answer = signal('');
  readonly intentTags = signal<string[]>([]);
  readonly products = signal<AiCard[]>([]);
  readonly webResults = signal<AiRankedResult[]>([]);
  readonly error = signal('');
  readonly savingSearchAlert = signal(false);
  // True when the AI returned no local matches we can show (low confidence).
  readonly lowConfidence = signal(false);

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  canUseShopperActions(): boolean {
    return !this.auth.isLoggedIn() || this.auth.isShopper();
  }

  // Localized follow-up refinements appended to the active query.
  readonly followUps: { en: string; ar: string }[] = [
    { en: 'cheaper options', ar: 'أرخص' },
    { en: 'highest rated', ar: 'الأعلى تقييماً' },
    { en: 'nearest stores', ar: 'أقرب المحلات' },
    { en: 'in stock now', ar: 'متوفر الآن' },
  ];

  readonly exampleChips: { q: string; en: string; ar: string }[] = [
    { q: 'red linen summer dress under 500 EGP', en: 'red linen dress < 500', ar: 'فستان كتان أحمر < ٥٠٠' },
    { q: 'white leather sneakers', en: 'white sneakers', ar: 'كوتشي أبيض' },
    { q: 'brown leather crossbody bag', en: 'leather bag', ar: 'شنطة جلد' },
    { q: 'cotton oxford shirt', en: 'cotton shirt', ar: 'قميص قطن' },
  ];

  ngOnInit(): void {
    this.title.setTitle(this.L('Dokkana | AI search', 'دكانة | البحث الذكي'));
    const q = this.route.snapshot.queryParamMap.get('q')?.trim();
    if (q) {
      this.query = q;
      this.runSearch();
    }
  }

  runSearch(): void {
    const query = this.query.trim();
    if (!query) return;

    this.baseQuery = query;
    this.searching.set(true);
    this.hasSearched.set(true);
    this.error.set('');
    this.lowConfidence.set(false);
    // Keep the address bar shareable/refreshable.
    this.router.navigate([], { queryParams: { q: query }, replaceUrl: true });

    this.aiSearch.search({ query, includeWeb: true }).subscribe({
      next: res => this.applyResponse(res),
      error: () => {
        // The AI endpoint failed entirely: fall back to plain product search so
        // the user still gets results instead of a dead end.
        this.searching.set(false);
        this.error.set(this.L(
          'The AI assistant is unavailable right now. Showing a normal search instead.',
          'المساعد الذكي غير متاح حالياً. يتم عرض بحث عادي بدلاً منه.',
        ));
        this.normalSearchFallback();
      },
    });
  }

  // Visual search: upload a photo, find visually similar local products.
  searchByImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.searching.set(true);
    this.hasSearched.set(true);
    this.error.set('');
    this.lowConfidence.set(false);
    this.answer.set(this.L('Searching by image…', 'جاري البحث بالصورة…'));

    this.aiSearch.searchByImage(file).subscribe({
      next: res => this.applyResponse(res),
      error: () => {
        this.searching.set(false);
        this.error.set(this.L('Image search is unavailable right now.', 'البحث بالصورة غير متاح حالياً.'));
      },
    });
  }

  private applyResponse(res: AiSearchResponse): void {
    this.searching.set(false);
    this.answer.set(res.answer ?? '');
    this.intentTags.set((res.intent?.searchTerms ?? []).slice(0, 6));

    const local = res.localResults ?? [];
    this.products.set(local.map((p, i) => this.toCard(p, i)));
    this.webResults.set(this.extractWebResults(res));
    // No local matches = low confidence; offer the normal-search escape hatch.
    this.lowConfidence.set(local.length === 0);
  }

  followUp(chip: { en: string; ar: string }): void {
    const modifier = this.lang.isAr() ? chip.ar : chip.en;
    this.query = `${this.baseQuery} ${modifier}`.trim();
    this.runSearch();
  }

  useExample(chip: { q: string }): void {
    this.query = chip.q;
    this.runSearch();
  }

  // Plain catalog search, shown when the AI fails or has low confidence.
  normalSearchFallback(): void {
    const query = (this.baseQuery || this.query).trim();
    if (!query) return;
    this.searching.set(true);
    this.productService.getAll({ search: query, page: 1, pageSize: 48 }).subscribe({
      next: page => {
        this.searching.set(false);
        this.answer.set('');
        this.webResults.set([]);
        this.lowConfidence.set(false);
        this.products.set(page.items.map((p, i) => ({
          id: p.id,
          storeId: p.storeId,
          title: p.title,
          store: p.storeName,
          price: p.price,
          originalPrice: p.originalPrice,
          salePrice: p.salePrice,
          isOnSale: p.isOnSale,
          image: p.imageUrl ?? '',
          rating: 5,
          reviewCount: 0,
          location: '',
          reason: '',
          icon: this.iconFor(p.categoryName),
          gradient: this.gradients[i % this.gradients.length],
        })));
      },
      error: () => this.searching.set(false),
    });
  }

  openProduct(card: AiCard): void {
    if (card.promotionId) {
      this.monetization.trackPromotionClick(card.promotionId).subscribe({ error: () => {} });
    }
    this.router.navigate(['/products', card.id]);
  }

  createSearchAlert(): void {
    const query = (this.baseQuery || this.query).trim();
    if (!query || this.savingSearchAlert()) return;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.notice.show(this.L('Shopper accounts only can create alerts', 'حسابات المتسوقين فقط يمكنها إنشاء التنبيهات'), 'error');
      return;
    }

    this.savingSearchAlert.set(true);
    this.monetization.createAlert({ type: 'NewMatch', query }).subscribe({
      next: () => {
        this.savingSearchAlert.set(false);
        this.notice.show(this.L('Search alert saved.', 'تم حفظ تنبيه البحث.'));
      },
      error: err => {
        this.savingSearchAlert.set(false);
        this.notice.show(err?.error?.message || this.L('Could not save alert.', 'تعذّر حفظ التنبيه.'), 'error');
      },
    });
  }

  addToCart(card: AiCard, event: Event): void {
    event.stopPropagation();
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.notice.show(this.L('Shopper accounts only can reserve items', 'حسابات المتسوقين فقط يمكنها الحجز'), 'error');
      return;
    }
    this.cartApi.addItem(card.id).subscribe({
      next: res => {
        if (res.success && res.data) {
          this.cart.setFromCart(res.data);
          if (card.storeId) {
            this.monetization.createLead({ storeId: card.storeId, productId: card.id, type: 'Reservation' }).subscribe({ error: () => {} });
          }
          this.notice.show(this.L('Added to reservation', 'تمت الإضافة للحجز'));
        } else {
          this.notice.show(res.message || this.L('Could not add item', 'تعذّرت إضافة المنتج'), 'error');
        }
      },
      error: err => this.notice.show(err?.error?.message || this.L('Could not add item', 'تعذّرت إضافة المنتج'), 'error'),
    });
  }

  stars(n: number): boolean[] {
    return Array(5).fill(0).map((_, i) => i < Math.round(n));
  }

  onImgError(e: Event): void { (e.target as HTMLElement).style.display = 'none'; }

  // ---- mapping + visuals -------------------------------------------------

  private readonly gradients = [
    'linear-gradient(150deg, oklch(0.56 0.17 22), oklch(0.36 0.11 22))',
    'linear-gradient(150deg, oklch(0.64 0.155 45), oklch(0.42 0.1 45))',
    'linear-gradient(150deg, oklch(0.61 0.085 195), oklch(0.4 0.06 195))',
    'linear-gradient(150deg, oklch(0.52 0.14 42), oklch(0.34 0.09 42))',
  ];

  private readonly icons: Record<string, string> = {
    dress: '<path d="M9 2h6l-1 4 3 4-2 2 2 9H7l2-9-2-2 3-4-1-4z"/>',
    shirt: '<path d="M6 3l3-1 3 2 3-2 3 1 2 5-3 1v9H7v-9L4 8z"/>',
    sneaker: '<path d="M2 16l2-7 4 2 3-3 2 1v3l7 2c1 .3 2 1 2 2v2H2z"/>',
    bag: '<path d="M5 8h14l-1 13H6L5 8z"/><path d="M9 8V6a3 3 0 016 0v2"/>',
  };

  garment(type: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(
      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round">${this.icons[type]}</svg>`,
    );
  }

  private toCard(p: AiProductResult, index: number): AiCard {
    const loc: string[] = [];
    if (p.storeAddress) loc.push(p.storeAddress);
    if (p.distanceKm != null) loc.push(`${p.distanceKm} ${this.L('km away', 'كم')}`);
    return {
      id: p.id,
      storeId: p.storeId,
      title: p.title,
      store: p.storeName ?? '',
      price: p.price,
      originalPrice: p.originalPrice,
      salePrice: p.salePrice,
      isOnSale: p.isOnSale,
      isSponsored: p.isSponsored,
      promotionId: p.promotionId,
      image: p.imageUrl ?? '',
      rating: p.rating > 0 ? p.rating : 0,
      reviewCount: p.reviewCount,
      location: loc.join(' · '),
      reason: p.reason ?? '',
      icon: this.iconFor(p.title),
      gradient: this.gradients[index % this.gradients.length],
    };
  }

  displayPrice(card: AiCard): number {
    return card.isOnSale && card.salePrice != null ? card.salePrice : card.price;
  }

  private iconFor(text: string): AiCard['icon'] {
    const c = (text || '').toLowerCase();
    if (c.includes('dress') || c.includes('فستان')) return 'dress';
    if (c.includes('shoe') || c.includes('sneaker') || c.includes('كوتش') || c.includes('حذاء')) return 'sneaker';
    if (c.includes('bag') || c.includes('شنط')) return 'bag';
    return 'shirt';
  }

  private extractWebResults(res: AiSearchResponse): AiRankedResult[] {
    const ranked = (res.rankedResults ?? []).filter(r => r.source?.toLowerCase() === 'web' && r.url);
    if (ranked.length) return ranked;
    return (res.webResults ?? []).map(web => ({
      source: 'web',
      title: web.title,
      description: web.snippet,
      url: web.url,
      score: web.score,
      reason: web.reason,
      webResult: web,
    }));
  }
}
