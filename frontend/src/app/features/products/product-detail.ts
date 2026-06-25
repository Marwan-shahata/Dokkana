import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { AuthService } from '../../core/services/auth.service';
import { EngagementService } from '../../core/services/engagement.service';
import { ProductService } from '../../core/services/product.service';
import { StoreService } from '../../core/services/store.service';
import { MonetizationService } from '../../core/services/monetization.service';
import { ProductDetails, ProductListItem, StoreDetails } from '../../core/models/catalog.models';
import { ProductReviewDTO } from '../../core/models/engagement.models';
import { StoreMapComponent } from '../../shared/components/store-map/store-map';

// Product details page (/products/:id) backed by GET /api/products/{id}.
@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, StoreMapComponent],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css',
})
export class ProductDetailPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private productService = inject(ProductService);
  private storeService = inject(StoreService);
  private cartState = inject(CartService);
  private cartApi = inject(CartApiService);
  private auth = inject(AuthService);
  private engagement = inject(EngagementService);
  private monetization = inject(MonetizationService);
  lang = inject(LanguageService);

  readonly product = signal<ProductDetails | null>(null);
  readonly reviews = signal<ProductReviewDTO[]>([]);
  readonly related = signal<ProductListItem[]>([]);
  readonly storeDetails = signal<StoreDetails | null>(null);
  readonly loading = signal(true);
  readonly reviewsLoading = signal(false);
  readonly savingReview = signal(false);
  readonly savingWishlist = signal(false);
  readonly savingAlert = signal(false);
  readonly locatingDistance = signal(false);
  readonly distanceKm = signal<number | null>(null);
  readonly distanceError = signal('');
  readonly error = signal('');
  readonly toast = signal('');
  reviewRating = 5;
  reviewComment = '';

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  ngOnInit(): void {
    // React to :id changes too, so clicking a related product reloads the page
    // instead of leaving stale data (the route/component is reused).
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => this.loadProduct(Number(params.get('id'))));
  }

  private loadProduct(id: number): void {
    if (!id) {
      this.error.set(this.L('Product not found.', 'المنتج غير موجود.'));
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.related.set([]);
    this.reviews.set([]);
    this.storeDetails.set(null);
    this.distanceKm.set(null);
    this.distanceError.set('');
    this.productService.getById(id).subscribe({
      next: p => {
        this.product.set(p);
        this.loading.set(false);
        this.loadStoreDetails(p.storeId);
        this.loadReviews(p.id);
        this.loadRelated(p.storeId, p.id);
      },
      error: () => {
        this.error.set(this.L('Could not load this product.', 'تعذّر تحميل هذا المنتج.'));
        this.loading.set(false);
      },
    });
  }

  private loadStoreDetails(storeId: number): void {
    this.storeService.getById(storeId).subscribe({
      next: store => this.storeDetails.set(store),
      error: () => this.storeDetails.set(null),
    });
  }

  // "More from this store": other products from the same store, current excluded.
  private loadRelated(storeId: number, currentId: number): void {
    this.productService.getByStore(storeId, 1, 12).subscribe({
      next: page => this.related.set(page.items.filter(p => p.id !== currentId).slice(0, 6)),
      error: () => this.related.set([]),
    });
  }

  openRelated(p: ProductListItem): void {
    this.router.navigate(['/products', p.id]);
  }

  get inStock(): boolean {
    return (this.product()?.count ?? 0) > 0;
  }

  canUseShopperActions(): boolean {
    return !this.auth.isLoggedIn() || this.auth.isShopper();
  }

  isBusinessAccount(): boolean {
    return this.auth.isLoggedIn() && !this.auth.isShopper();
  }

  addToCart(): void {
    const p = this.product();
    if (!p || !this.inStock) return;

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
          this.cartState.setFromCart(res.data);
          this.monetization.createLead({ storeId: p.storeId, productId: p.id, type: 'Reservation' }).subscribe({ error: () => {} });
          this.showToast(this.L('Added to reservation', 'تمت الإضافة للحجز'));
        } else {
          this.showToast(res.message || this.L('Could not add item', 'تعذّرت إضافة المنتج'));
        }
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not add item', 'تعذّرت إضافة المنتج')),
    });
  }

  addToWishlist(): void {
    const p = this.product();
    if (!p) return;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.showToast(this.L('Shopper accounts only can save wishlist items', 'حسابات المتسوقين فقط يمكنها حفظ المنتجات'));
      return;
    }

    this.savingWishlist.set(true);
    this.engagement.addToWishlist(p.id).subscribe({
      next: res => {
        this.savingWishlist.set(false);
        this.showToast(res.message || this.L('Saved to wishlist', 'تم الحفظ في المفضلة'));
      },
      error: err => {
        this.savingWishlist.set(false);
        this.showToast(err?.error?.message || this.L('Could not save item', 'تعذّر حفظ المنتج'));
      },
    });
  }

  createProductAlert(): void {
    const p = this.product();
    if (!p || this.savingAlert()) return;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.showToast(this.L('Shopper accounts only can create alerts', 'حسابات المتسوقين فقط يمكنها إنشاء التنبيهات'));
      return;
    }

    this.savingAlert.set(true);
    this.monetization.createAlert({
      productId: p.id,
      type: this.inStock ? 'PriceDrop' : 'BackInStock',
      targetPrice: this.inStock ? Math.max(1, Math.floor(p.price * 0.9)) : null,
    }).subscribe({
      next: () => {
        this.savingAlert.set(false);
        this.showToast(this.inStock
          ? this.L('Price drop alert saved.', 'تم حفظ تنبيه انخفاض السعر.')
          : this.L('Back-in-stock alert saved.', 'تم حفظ تنبيه عودة التوفر.'));
      },
      error: err => {
        this.savingAlert.set(false);
        this.showToast(err?.error?.message || this.L('Could not save alert.', 'تعذّر حفظ التنبيه.'));
      },
    });
  }

  openWhatsApp(): void {
    const p = this.product();
    const store = this.storeDetails();
    if (!p || !store?.whatsAppPhone) return;

    this.monetization.createLead({ storeId: store.id, productId: p.id, type: 'WhatsApp' }).subscribe({ error: () => {} });
    const phone = store.whatsAppPhone.replace(/[^\d]/g, '');
    if (!phone) {
      this.showToast(this.L('This store has no valid WhatsApp number yet.', 'المحل لم يضف رقم واتساب صحيح بعد.'));
      return;
    }

    const message = encodeURIComponent(this.L(
      `Hi ${store.name}, I found "${p.title}" on Dokkana. Is it available?`,
      `أهلاً ${store.name}، لقيت "${p.title}" على دكانة. هل المنتج متاح؟`,
    ));
    window.open(`https://wa.me/${phone}?text=${message}`, '_blank', 'noopener,noreferrer');
  }

  saveReview(): void {
    const p = this.product();
    if (!p) return;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.showToast(this.L('Shopper accounts only can review products', 'حسابات المتسوقين فقط يمكنها التقييم'));
      return;
    }

    this.savingReview.set(true);
    this.engagement.saveProductReview(p.id, {
      rating: this.reviewRating,
      comment: this.reviewComment.trim() || null,
    }).subscribe({
      next: res => {
        this.savingReview.set(false);
        if (res.success && res.data) {
          this.reviews.update(items => [res.data!, ...items.filter(item => item.id !== res.data!.id)]);
          this.reviewComment = '';
          this.showToast(this.L('Review saved', 'تم حفظ التقييم'));
        }
      },
      error: err => {
        this.savingReview.set(false);
        this.showToast(err?.error?.message || this.L('Could not save review', 'تعذّر حفظ التقييم'));
      },
    });
  }

  deleteReview(review: ProductReviewDTO): void {
    const p = this.product();
    if (!p || !review.canEdit) return;
    this.engagement.deleteProductReview(p.id, review.id).subscribe({
      next: () => {
        this.reviews.update(items => items.filter(item => item.id !== review.id));
        this.showToast(this.L('Review deleted', 'تم حذف التقييم'));
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not delete review', 'تعذّر حذف التقييم')),
    });
  }

  private loadReviews(productId: number): void {
    this.reviewsLoading.set(true);
    this.engagement.getProductReviews(productId).subscribe({
      next: reviews => {
        this.reviews.set(reviews);
        this.reviewsLoading.set(false);
      },
      error: () => this.reviewsLoading.set(false),
    });
  }

  openStore(): void {
    const p = this.product();
    if (p) this.router.navigate(['/stores', p.storeId]);
  }

  showDistance(): void {
    const store = this.storeDetails();
    if (!store || store.latitude == null || store.longitude == null) {
      this.distanceError.set(
        this.L(
          'Exact distance is available when this store pins its map location.',
          'المسافة الدقيقة تظهر لما المحل يحدد موقعه على الخريطة.',
        ),
      );
      return;
    }

    if (!('geolocation' in navigator)) {
      this.distanceError.set(this.L('Location is not supported on this device.', 'تحديد الموقع غير مدعوم على هذا الجهاز.'));
      return;
    }

    this.locatingDistance.set(true);
    this.distanceError.set('');
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.locatingDistance.set(false);
        this.distanceKm.set(this.calculateDistanceKm(
          pos.coords.latitude,
          pos.coords.longitude,
          store.latitude!,
          store.longitude!,
        ));
      },
      err => {
        this.locatingDistance.set(false);
        this.distanceError.set(
          err.code === err.PERMISSION_DENIED
            ? this.L('Allow location access to calculate the distance.', 'اسمح بالوصول للموقع عشان نحسب المسافة.')
            : this.L('Could not get your location. Try again.', 'تعذّر تحديد موقعك. حاول مرة أخرى.'),
        );
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 },
    );
  }

  distanceLabel(): string {
    const distance = this.distanceKm();
    if (distance == null) return '';
    const locale = this.lang.isAr() ? 'ar-EG' : 'en';
    const value = new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(distance);
    return this.L(`About ${value} km away`, `حوالي ${value} كم منك`);
  }

  displayPrice(p: ProductDetails | ProductListItem): number {
    return p.isOnSale && p.salePrice != null ? p.salePrice : p.price;
  }

  back(): void {
    this.router.navigate(['/stores']);
  }

  onImgError(e: Event): void {
    (e.target as HTMLElement).style.visibility = 'hidden';
  }

  private toastTimer: ReturnType<typeof setTimeout> | undefined;
  private showToast(msg: string): void {
    this.toast.set(msg);
    clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toast.set(''), 2200);
  }

  private calculateDistanceKm(fromLat: number, fromLng: number, toLat: number, toLng: number): number {
    const toRadians = (value: number) => value * Math.PI / 180;
    const earthRadiusKm = 6371;
    const dLat = toRadians(toLat - fromLat);
    const dLng = toRadians(toLng - fromLng);
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(toRadians(fromLat)) * Math.cos(toRadians(toLat)) * Math.sin(dLng / 2) ** 2;
    return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }
}
