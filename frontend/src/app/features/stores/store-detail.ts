import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/services/auth.service';
import { EngagementService } from '../../core/services/engagement.service';
import { StoreService } from '../../core/services/store.service';
import { ProductService } from '../../core/services/product.service';
import { ProductListItem, StoreDetails } from '../../core/models/catalog.models';

// Store details page (/stores/:id): shows the store header plus its products,
// backed by GET /api/stores/{id} and GET /api/products/store/{storeId}.
@Component({
  selector: 'app-store-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './store-detail.html',
  styleUrl: './store-detail.css',
})
export class StoreDetailPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);
  private engagement = inject(EngagementService);
  private storeService = inject(StoreService);
  private productService = inject(ProductService);
  lang = inject(LanguageService);

  readonly store = signal<StoreDetails | null>(null);
  readonly products = signal<ProductListItem[]>([]);
  readonly loading = signal(true);
  readonly productsLoading = signal(true);
  readonly savingStore = signal(false);
  readonly toast = signal('');
  readonly error = signal('');

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  canUseShopperActions(): boolean {
    return !this.auth.isLoggedIn() || this.auth.isShopper();
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.L('Store not found.', 'المحل غير موجود.'));
      this.loading.set(false);
      this.productsLoading.set(false);
      return;
    }
    this.loadStore(id);
    this.loadProducts(id);
  }

  private loadStore(id: number): void {
    this.loading.set(true);
    this.storeService.getById(id).subscribe({
      next: store => {
        this.store.set(store);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.L('Could not load this store.', 'تعذّر تحميل هذا المحل.'));
        this.loading.set(false);
      },
    });
  }

  private loadProducts(id: number): void {
    this.productsLoading.set(true);
    this.productService.getByStore(id, 1, 24).subscribe({
      next: page => {
        this.products.set(page.items);
        this.productsLoading.set(false);
      },
      error: () => this.productsLoading.set(false),
    });
  }

  openProduct(p: ProductListItem): void {
    this.router.navigate(['/products', p.id]);
  }

  saveStore(): void {
    const store = this.store();
    if (!store) return;
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.auth.isShopper()) {
      this.showToast(this.L('Shopper accounts only can save stores', 'حسابات المتسوقين فقط يمكنها حفظ المحلات'));
      return;
    }

    this.savingStore.set(true);
    this.engagement.followStore(store.id).subscribe({
      next: res => {
        this.savingStore.set(false);
        this.showToast(res.message || this.L('Store saved', 'تم حفظ المحل'));
      },
      error: err => {
        this.savingStore.set(false);
        this.showToast(err?.error?.message || this.L('Could not save store', 'تعذّر حفظ المحل'));
      },
    });
  }

  back(): void {
    this.router.navigate(['/stores']);
  }

  onImgError(e: Event): void {
    (e.target as HTMLElement).style.visibility = 'hidden';
  }

  private toastTimer: ReturnType<typeof setTimeout> | undefined;
  private showToast(message: string): void {
    this.toast.set(message);
    clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toast.set(''), 2200);
  }
}
