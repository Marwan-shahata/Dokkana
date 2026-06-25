import { AfterViewInit, Component, ElementRef, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { firstValueFrom } from 'rxjs';
import { ProductService } from '../../core/services/product.service';
import { StoreService } from '../../core/services/store.service';
import { MonetizationService } from '../../core/services/monetization.service';
import { LanguageService } from '../../core/services/language.service';
import { ProductListItem } from '../../core/models/catalog.models';
import { PromotionDTO, TrendingQueryDTO } from '../../core/models/monetization.models';

interface PromptChip {
  readonly en: string;
  readonly ar: string;
  readonly enQuery: string;
  readonly arQuery: string;
}

interface JourneyStep {
  readonly number: string;
  readonly enTitle: string;
  readonly arTitle: string;
  readonly enBody: string;
  readonly arBody: string;
}

interface TrustNote {
  readonly value: string;
  readonly arValue: string;
  readonly enLabel: string;
  readonly arLabel: string;
}

interface LandingProduct {
  readonly id?: number;
  readonly name: string;
  readonly nameAr?: string;
  readonly store: string;
  readonly storeAr?: string;
  readonly category: string;
  readonly categoryAr?: string;
  readonly price: number;
  readonly image: string;
  readonly sponsored?: boolean;
  readonly promotionId?: number;
}

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css'
})
export class LandingPage implements OnInit, AfterViewInit {
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  private readonly products = inject(ProductService);
  private readonly stores = inject(StoreService);
  private readonly monetization = inject(MonetizationService);
  private readonly language = inject(LanguageService);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  query = '';

  readonly storeCount = signal(0);
  readonly productPreview = signal<LandingProduct[]>([]);
  readonly sponsoredPreview = signal<LandingProduct[]>([]);
  readonly trendingQueries = signal<TrendingQueryDTO[]>([]);
  readonly loadingPreview = signal(true);
  readonly toast = signal('');

  private toastTimer?: ReturnType<typeof setTimeout>;

  readonly promptChips: PromptChip[] = [
    {
      en: 'under 500 EGP',
      ar: 'أقل من ٥٠٠ جنيه',
      enQuery: 'summer dress under 500 EGP',
      arQuery: 'فستان صيفي أقل من ٥٠٠ جنيه'
    },
    {
      en: 'near me today',
      ar: 'قريب مني النهارده',
      enQuery: 'women outfit near me today',
      arQuery: 'لبس خروجة قريب مني النهارده'
    },
    {
      en: 'formal black shirt',
      ar: 'قميص أسود رسمي',
      enQuery: 'formal black shirt in stock',
      arQuery: 'قميص أسود رسمي متاح دلوقتي'
    }
  ];

  readonly journeySteps: JourneyStep[] = [
    {
      number: '01',
      enTitle: 'Describe what you need',
      arTitle: 'قولنا محتاج إيه',
      enBody: 'Use natural language, budget, color, occasion, size, or a photo idea.',
      arBody: 'اكتب اللون، الميزانية، المناسبة، المقاس، أو ارفع صورة شبه اللي في بالك.'
    },
    {
      number: '02',
      enTitle: 'Compare local stock',
      arTitle: 'قارن المتاح قريب منك',
      enBody: 'Dokkana filters real products from nearby stores before sending you wider web options.',
      arBody: 'دكانة بتطلع منتجات من محلات قريبة الأول، وبعدها تقترح بدائل أونلاين لو محتاج.'
    },
    {
      number: '03',
      enTitle: 'Reserve or visit',
      arTitle: 'احجز أو روح للمحل',
      enBody: 'Open the store, check availability, add to cart, or keep shopping by category.',
      arBody: 'افتح صفحة المحل، شوف التوفر، ضيف للسلة، أو كمل تصفح حسب الفئة.'
    }
  ];

  readonly trustNotes: TrustNote[] = [
    {
      value: 'AI',
      arValue: 'ذكي',
      enLabel: 'search that understands style, color, and budget',
      arLabel: 'بحث بيفهم الستايل واللون والميزانية'
    },
    {
      value: 'Local',
      arValue: 'محلي',
      enLabel: 'store pages, products, and pickup-first journeys',
      arLabel: 'صفحات محلات ومنتجات ورحلة سهلة للاستلام'
    },
    {
      value: 'Bilingual',
      arValue: 'عربي + إنجليزي',
      enLabel: 'Arabic and English prompts across the same flow',
      arLabel: 'اكتب بالعربي أو الإنجليزي في نفس التجربة'
    }
  ];

  private readonly productNamesAr: Record<string, string> = {
    'Black Polo Shirt': 'بولو أسود',
    'Running Shoes': 'كوتشي جري',
    'Performance T-Shirt': 'تيشيرت رياضي',
    'Floral Maxi Dress': 'فستان طويل منقوش',
    'Summer Sundress': 'فستان صيفي',
    'Wide-Leg Linen Pants': 'بنطلون كتان واسع'
  };

  private readonly categoriesAr: Record<string, string> = {
    Activewear: 'لبس رياضي',
    Dresses: 'فساتين',
    Linen: 'كتان',
    Summer: 'صيفي',
    Tops: 'تيشيرتات وقمصان'
  };

  private readonly storesAr: Record<string, string> = {
    'Dokkana Sample Store': 'محل دكانة التجريبي'
  };

  readonly fallbackProducts: LandingProduct[] = [
    {
      name: 'Floral Maxi Dress',
      nameAr: 'فستان طويل منقوش',
      store: 'Dokkana Sample Store',
      storeAr: 'محل دكانة التجريبي',
      category: 'Dresses',
      categoryAr: 'فساتين',
      price: 450,
      image: '/hero.jpg'
    },
    {
      name: 'Summer Sundress',
      nameAr: 'فستان صيفي',
      store: 'Dokkana Sample Store',
      storeAr: 'محل دكانة التجريبي',
      category: 'Summer',
      categoryAr: 'صيفي',
      price: 250,
      image: '/about.png'
    },
    {
      name: 'Wide-Leg Linen Pants',
      nameAr: 'بنطلون كتان واسع',
      store: 'Dokkana Sample Store',
      storeAr: 'محل دكانة التجريبي',
      category: 'Linen',
      categoryAr: 'كتان',
      price: 310,
      image: '/shop.png'
    }
  ];

  ngOnInit(): void {
    this.title.setTitle('Dokkana | Shop local fashion with AI');
    void this.loadStoreCount();
    void this.loadProductPreview();
    void this.loadSponsoredPreview();
    void this.loadTrendingQueries();
  }

  ngAfterViewInit(): void {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const revealEls = Array.from(this.host.nativeElement.querySelectorAll('[data-reveal]')) as HTMLElement[];

    if (reduceMotion || !('IntersectionObserver' in window)) {
      revealEls.forEach((el: HTMLElement) => el.classList.add('is-visible'));
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add('is-visible');
          observer.unobserve(entry.target);
        });
      },
      { threshold: 0.16 }
    );

    revealEls.forEach((el: HTMLElement) => observer.observe(el));
  }

  L(en: string, ar: string): string {
    return this.language.t(en, ar);
  }

  searchPlaceholder(): string {
    return this.L('linen summer dress under 500 EGP', 'فستان صيفي كتان أقل من ٥٠٠ جنيه');
  }

  openAiSearch(search = this.query): void {
    const q = search.trim() || this.searchPlaceholder();
    void this.router.navigate(['/ai-search'], {
      queryParams: q ? { q } : undefined
    });
  }

  usePrompt(chip: PromptChip): void {
    const q = this.language.isAr() ? chip.arQuery : chip.enQuery;
    this.query = q;
    this.openAiSearch(q);
  }

  useTrend(trend: TrendingQueryDTO): void {
    this.query = trend.query;
    this.openAiSearch(trend.query);
  }

  viewStores(): void {
    void this.router.navigate(['/stores']);
  }

  openShop(): void {
    this.viewStores();
  }

  openSignup(): void {
    void this.router.navigate(['/signup']);
  }

  openProduct(product: LandingProduct): void {
    if (product.promotionId) {
      this.monetization.trackPromotionClick(product.promotionId).subscribe({ error: () => {} });
    }
    if (!product.id) {
      this.showToast(this.L('Open AI Search to find similar items from live stock.', 'افتح البحث الذكي عشان تلاقي قطع مشابهة من المتاح دلوقتي.'));
      return;
    }

    void this.router.navigate(['/products', product.id]);
  }

  onPreviewImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = '/about.png';
  }

  private async loadStoreCount(): Promise<void> {
    try {
      const result = await firstValueFrom(this.stores.getApproved());
      this.storeCount.set(result.length);
    } catch {
      this.storeCount.set(0);
    }
  }

  private async loadProductPreview(): Promise<void> {
    this.loadingPreview.set(true);
    try {
      const result = await firstValueFrom(this.products.getAll({ page: 1, pageSize: 6, maxPrice: 500 }));
      const items = (result.items ?? []).slice(0, 3).map((item) => this.mapPreviewProduct(item));
      this.productPreview.set(items.length ? items : this.fallbackProducts);
    } catch {
      this.productPreview.set(this.fallbackProducts);
    } finally {
      this.loadingPreview.set(false);
    }
  }

  private async loadSponsoredPreview(): Promise<void> {
    try {
      const result = await firstValueFrom(this.monetization.sponsoredProducts(2));
      this.sponsoredPreview.set(result.map(item => this.mapSponsoredPreviewProduct(item)).filter(item => item.id));
    } catch {
      this.sponsoredPreview.set([]);
    }
  }

  private async loadTrendingQueries(): Promise<void> {
    try {
      const result = await firstValueFrom(this.monetization.getTrending(5));
      this.trendingQueries.set(result);
    } catch {
      this.trendingQueries.set([]);
    }
  }

  private mapPreviewProduct(item: ProductListItem): LandingProduct {
    const name = item.title;
    const store = item.storeName || 'Local store';
    const category = item.categoryName || 'Featured';

    return {
      id: item.id,
      name,
      nameAr: this.productNamesAr[name],
      store,
      storeAr: this.storesAr[store] || (store === 'Local store' ? 'محل قريب منك' : undefined),
      category,
      categoryAr: this.categoriesAr[category] || (category === 'Featured' ? 'مختار' : undefined),
      price: item.price,
      image: item.imageUrl || '/about.png'
    };
  }

  private mapSponsoredPreviewProduct(item: PromotionDTO): LandingProduct {
    const name = item.productTitle || item.title || 'Sponsored pick';
    const store = item.storeName || 'Local store';

    return {
      id: item.productId ?? undefined,
      name,
      nameAr: item.productTitle ? undefined : 'اختيار ممول',
      store,
      storeAr: this.storesAr[store] || (store === 'Local store' ? 'محل قريب منك' : undefined),
      category: 'Sponsored',
      categoryAr: 'إعلان',
      price: item.productPrice ?? 0,
      image: item.productImageUrl || '/shop.png',
      sponsored: true,
      promotionId: item.id,
    };
  }

  private showToast(message: string): void {
    window.clearTimeout(this.toastTimer);
    this.toast.set(message);
    this.toastTimer = window.setTimeout(() => this.toast.set(''), 2600);
  }
}
