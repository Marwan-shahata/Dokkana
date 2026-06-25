import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { ProductService } from '../../core/services/product.service';
import { StoreService } from '../../core/services/store.service';
import { CategoryService } from '../../core/services/category.service';
import { NoticeService } from '../../core/services/notice.service';
import { MonetizationService } from '../../core/services/monetization.service';
import { ProductListItem, Store } from '../../core/models/catalog.models';
import { PromotionDTO, TrendingQueryDTO } from '../../core/models/monetization.models';

interface DiscoverCard {
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
  category: string;
}

// Signed-in shopper home: a single useful first screen after login. Pulls real
// catalog/store/category data; every section degrades to an empty state.
@Component({
  selector: 'app-discover',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './discover.html',
  styleUrl: './discover.css',
})
export class DiscoverPage implements OnInit {
  private router = inject(Router);
  private title = inject(Title);
  private auth = inject(AuthService);
  private productService = inject(ProductService);
  private storeService = inject(StoreService);
  private categoryService = inject(CategoryService);
  private monetization = inject(MonetizationService);
  private cartApi = inject(CartApiService);
  private notice = inject(NoticeService);
  lang = inject(LanguageService);
  cart = inject(CartService);

  query = '';

  readonly categories = signal<string[]>([]);
  readonly products = signal<DiscoverCard[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly sponsoredProducts = signal<DiscoverCard[]>([]);
  readonly trends = signal<TrendingQueryDTO[]>([]);
  readonly productsLoading = signal(true);
  readonly storesLoading = signal(true);

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  firstName(): string {
    return this.auth.currentUser()?.firstName?.trim() || this.L('there', 'بيك');
  }

  ngOnInit(): void {
    this.title.setTitle(this.L('Dokkana | Discover', 'دكانة | اكتشف'));
    this.loadCategories();
    this.loadProducts();
    this.loadStores();
    this.loadTrending();
    this.loadSponsoredProducts();
  }

  private loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: cats => this.categories.set(cats.map(c => c.name).slice(0, 8)),
      error: () => this.categories.set([]),
    });
  }

  private loadProducts(): void {
    this.productsLoading.set(true);
    this.productService.getAll({ page: 1, pageSize: 12 }).subscribe({
      next: page => {
        this.products.set(page.items.map(p => this.toCard(p)));
        this.productsLoading.set(false);
      },
      error: () => this.productsLoading.set(false),
    });
  }

  private loadStores(): void {
    this.storesLoading.set(true);
    this.storeService.getApproved().subscribe({
      next: stores => {
        this.stores.set(stores.slice(0, 4));
        this.storesLoading.set(false);
      },
      error: () => this.storesLoading.set(false),
    });
  }

  private toCard(p: ProductListItem): DiscoverCard {
    return {
      id: p.id,
      storeId: p.storeId,
      title: p.title,
      store: p.storeName,
      price: p.price,
      originalPrice: p.originalPrice,
      salePrice: p.salePrice,
      isOnSale: p.isOnSale,
      image: p.imageUrl ?? '',
      category: p.categoryName,
    };
  }

  // The search box and category chips both lead into the AI assistant.
  runAiSearch(): void {
    const q = this.query.trim();
    this.router.navigate(['/ai-search'], { queryParams: q ? { q } : {} });
  }

  openCategory(cat: string): void {
    this.router.navigate(['/ai-search'], { queryParams: { q: cat } });
  }

  openTrend(trend: TrendingQueryDTO): void {
    this.router.navigate(['/ai-search'], { queryParams: { q: trend.query } });
  }

  openProduct(card: DiscoverCard): void {
    if (card.promotionId) {
      this.monetization.trackPromotionClick(card.promotionId).subscribe({ error: () => {} });
    }
    this.router.navigate(['/products', card.id]);
  }

  openStore(store: Store): void {
    this.router.navigate(['/stores', store.id]);
  }

  viewReservations(): void {
    this.router.navigate(['/profile']);
  }

  browseStores(): void {
    this.router.navigate(['/stores']);
  }

  addToCart(card: DiscoverCard, event: Event): void {
    event.stopPropagation();
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

  initials(name: string): string {
    return name.split(' ').filter(Boolean).slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  onImgError(e: Event): void { (e.target as HTMLElement).style.display = 'none'; }

  displayPrice(card: DiscoverCard): number {
    return card.isOnSale && card.salePrice != null ? card.salePrice : card.price;
  }

  private loadSponsoredProducts(): void {
    this.monetization.sponsoredProducts(4).subscribe({
      next: campaigns => this.sponsoredProducts.set(campaigns.map(c => this.toSponsoredCard(c))),
      error: () => this.sponsoredProducts.set([]),
    });
  }

  private loadTrending(): void {
    this.monetization.getTrending(8).subscribe({
      next: trends => this.trends.set(trends),
      error: () => this.trends.set([]),
    });
  }

  private toSponsoredCard(c: PromotionDTO): DiscoverCard {
    return {
      id: c.productId ?? 0,
      storeId: c.storeId,
      title: c.productTitle ?? c.title ?? this.L('Sponsored pick', 'اختيار ممول'),
      store: c.storeName,
      price: c.productPrice ?? 0,
      originalPrice: c.productOriginalPrice,
      isOnSale: c.isProductOnSale,
      image: c.productImageUrl ?? '',
      category: this.L('Sponsored', 'إعلان'),
      isSponsored: true,
      promotionId: c.id,
    };
  }
}
