import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { AuthService } from '../../core/services/auth.service';
import { EngagementService } from '../../core/services/engagement.service';
import { ProductService } from '../../core/services/product.service';
import { ProductDetails } from '../../core/models/catalog.models';
import { ProductReviewDTO } from '../../core/models/engagement.models';

// Product details page (/products/:id) backed by GET /api/products/{id}.
@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css',
})
export class ProductDetailPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private cartState = inject(CartService);
  private cartApi = inject(CartApiService);
  private auth = inject(AuthService);
  private engagement = inject(EngagementService);
  lang = inject(LanguageService);

  readonly product = signal<ProductDetails | null>(null);
  readonly reviews = signal<ProductReviewDTO[]>([]);
  readonly loading = signal(true);
  readonly reviewsLoading = signal(false);
  readonly savingReview = signal(false);
  readonly savingWishlist = signal(false);
  readonly error = signal('');
  readonly toast = signal('');
  reviewRating = 5;
  reviewComment = '';

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.L('Product not found.', 'المنتج غير موجود.'));
      this.loading.set(false);
      return;
    }
    this.productService.getById(id).subscribe({
      next: p => {
        this.product.set(p);
        this.loading.set(false);
        this.loadReviews(p.id);
      },
      error: () => {
        this.error.set(this.L('Could not load this product.', 'تعذّر تحميل هذا المنتج.'));
        this.loading.set(false);
      },
    });
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
}
