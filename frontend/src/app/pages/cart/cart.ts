import {
  Component,
  OnDestroy,
  HostListener,
  effect,
  inject,
  signal,
  computed,
  untracked,
 } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartApiService } from '../../core/services/cart-api.service';
import { OrderService } from '../../core/services/order.service';
import { CartService } from '../../core/services/cart.service';
import { CartDrawerService } from '../../core/services/cart-drawer.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { CartItemDTO } from '../../core/models/order.models';

export interface CartItem {
  id: number;
  name: string;
  brand: string;
  category: string;
  price: number;
  currency: string;
  quantity: number;
  availableStock: number;
  imageUrl?: string | null;
  size?: string;
  color?: string;
  gradient: string;
  removing?: boolean;
}

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
})
export class Cart implements OnDestroy {
  private cartApi = inject(CartApiService);
  private orders = inject(OrderService);
  private cartState = inject(CartService);
  private router = inject(Router);
  private cartDrawer = inject(CartDrawerService);
  auth = inject(AuthService);
  lang = inject(LanguageService);

  private readonly drawerOpenEffect = effect(() => {
    // Only react to cart-icon clicks (openRequests). The open/load work reads
    // auth signals, so run it untracked - otherwise the effect would also fire
    // on login/logout and pop the drawer open unexpectedly.
    const openRequest = this.cartDrawer.openRequests();

    if (openRequest > 0) {
      untracked(() => {
        this.openCart();
        this.loadCart();
      });
    }
  });

  // Drawer state
  isOpen    = signal(false);
  isLeaving = signal(false);
  loading   = signal(true);
  checkingOut = signal(false);

  // Checkout result (pickup code)
  pickupCode = signal<string | null>(null);

  // Toast
  toastMsg  = signal<string | null>(null);
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // Promo
  promoCode  = '';
  promoApplied = signal(false);
  promoError   = signal(false);
  readonly VALID_PROMO = 'DOKKANA10';
  readonly PROMO_DISCOUNT = 0.10; // 10%

  // Delivery
  readonly FREE_DELIVERY_THRESHOLD = 500;
  readonly DELIVERY_FEE = 49;
  readonly MAX_CART_QUANTITY = 20;
  readonly skeletonRows = [0, 1, 2];

  // Cart items (loaded from the backend cart)
  items = signal<CartItem[]>([]);

  private readonly gradients = [
    'linear-gradient(135deg, oklch(0.62 0.15 42/0.65), oklch(0.5 0.12 39/0.8))',
    'linear-gradient(135deg, oklch(0.55 0.08 220/0.6), oklch(0.42 0.07 210/0.75))',
    'linear-gradient(135deg, oklch(0.68 0.1 55/0.65), oklch(0.55 0.12 45/0.8))',
    'linear-gradient(135deg, oklch(0.6 0.13 25/0.62), oklch(0.45 0.1 20/0.78))',
  ];

  // Maps the backend cart line into the drawer's rich CartItem shape.
  private toCartItem(dto: CartItemDTO, index: number): CartItem {
    return {
      id: dto.productId,
      name: dto.title,
      brand: dto.storeName,
      category: '',
      price: dto.unitPrice,
      currency: 'EGP',
      quantity: dto.quantity,
      availableStock: dto.availableStock,
      imageUrl: dto.imageUrl,
      gradient: this.gradients[index % this.gradients.length],
      removing: false,
    };
  }

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  currencyLabel(): string {
    return this.lang.isAr() ? 'ج.م' : 'EGP';
  }

  maxQuantity(item: CartItem): number {
    const stock = Number.isFinite(item.availableStock) && item.availableStock > 0
      ? item.availableStock
      : this.MAX_CART_QUANTITY;
    return Math.max(1, Math.min(stock, this.MAX_CART_QUANTITY));
  }

  // Computed totals
  subtotal = computed(() =>
    this.items().reduce((sum, i) => sum + i.price * i.quantity, 0)
  );

  discount = computed(() =>
    this.promoApplied() ? Math.round(this.subtotal() * this.PROMO_DISCOUNT) : 0
  );

  deliveryFee = computed(() =>
    this.subtotal() - this.discount() >= this.FREE_DELIVERY_THRESHOLD ? 0 : this.DELIVERY_FEE
  );

  total = computed(() =>
    this.subtotal() - this.discount() + this.deliveryFee()
  );

  itemCount = computed(() =>
    this.items().reduce((sum, i) => sum + i.quantity, 0)
  );

  freeDeliveryRemaining = computed(() => {
    const rem = this.FREE_DELIVERY_THRESHOLD - (this.subtotal() - this.discount());
    return rem > 0 ? rem : 0;
  });

  freeDeliveryProgress = computed(() => {
    const pct = ((this.subtotal() - this.discount()) / this.FREE_DELIVERY_THRESHOLD) * 100;
    return Math.min(pct, 100);
  });

  private loadCart(): void {
    this.loading.set(true);
    this.pickupCode.set(null);

    if (!this.auth.isLoggedIn()) {
      this.items.set([]);
      this.cartState.clear();
      this.resetPromo();
      this.loading.set(false);
      return;
    }

    this.cartApi.get().subscribe({
      next: cart => {
        const items = cart.items.map((i, idx) => this.toCartItem(i, idx));
        this.items.set(items);
        this.cartState.setFromCart(cart);
        if (items.length === 0) this.resetPromo();
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.cartState.clear();
        this.resetPromo();
        this.loading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen()) this.closeCart();
  }

  // Open / Close
  openCart(): void {
    this.isOpen.set(true);
    this.isLeaving.set(false);
    this.pickupCode.set(null);
    document.body.style.overflow = 'hidden';
  }

  closeCart(): void {
    this.isLeaving.set(true);
    setTimeout(() => {
      this.isOpen.set(false);
      this.isLeaving.set(false);
      document.body.style.overflow = '';
    }, 380);
  }

  // Item management
  addItem(item: Omit<CartItem, 'removing'>): void {
    const existing = this.items().find(i => i.id === item.id);
    if (existing) {
      this.increaseQuantity(existing.id);
    } else {
      this.items.update(list => [...list, { ...item, removing: false }]);
    }
    this.toast(this.L(`${item.name} added to cart`, `تمت إضافة ${item.name} إلى السلة`));
  }

  removeItem(id: number): void {
    // Animate out first
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, removing: true } : i)
    );
    const name = this.items().find(i => i.id === id)?.name ?? this.L('Item', 'منتج');
    setTimeout(() => {
      this.items.update(list => list.filter(i => i.id !== id));
      if (this.items().length === 0) this.resetPromo();
    }, 340);
    // Persist removal to the backend cart (id == productId).
    this.cartApi.removeItem(id).subscribe({
      next: res => {
        if (res.data) this.cartState.setFromCart(res.data);
      },
      error: () => this.loadCart(),
    });
    this.toast(this.L(`${name} removed`, `تم حذف ${name}`));
  }

  increaseQuantity(id: number): void {
    const item = this.items().find(i => i.id === id);
    if (!item) return;
    const max = this.maxQuantity(item);
    if (item.quantity >= max) {
      this.toast(this.L(`Only ${max} in stock`, `المتاح فقط ${max}`));
      return;
    }
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, quantity: Math.min(i.quantity + 1, max) } : i)
    );
    this.syncQuantity(id);
  }

  decreaseQuantity(id: number): void {
    const item = this.items().find(i => i.id === id);
    if (!item) return;
    if (item.quantity <= 1) {
      this.removeItem(id);
    } else {
      this.items.update(list =>
        list.map(i => i.id === id ? { ...i, quantity: i.quantity - 1 } : i)
      );
      this.syncQuantity(id);
    }
  }

  // Pushes the new quantity to the backend; reloads from server on rejection
  // (e.g. exceeding available stock).
  private syncQuantity(id: number): void {
    const qty = this.items().find(i => i.id === id)?.quantity;
    if (qty == null) return;
    this.cartApi.updateItem(id, qty).subscribe({
      next: res => {
        if (!res.success) {
          this.toast(res.message || 'Could not update quantity.');
          this.loadCart();
        } else if (res.data) {
          this.cartState.setFromCart(res.data);
        }
      },
      error: () => this.loadCart(),
    });
  }

  calculateTotal(): number {
    return this.total();
  }

  clearCart(): void {
    if (this.items().length === 0) return;
    // Stagger removal animations
    const ids = this.items().map(i => i.id);
    ids.forEach((id, idx) => {
      setTimeout(() => {
        this.items.update(list =>
          list.map(i => i.id === id ? { ...i, removing: true } : i)
        );
        setTimeout(() => {
          this.items.update(list => list.filter(i => i.id !== id));
        }, 340);
      }, idx * 80);
    });
    this.cartApi.clear().subscribe({
      next: res => {
        if (res.data) this.cartState.setFromCart(res.data);
      },
      error: () => this.loadCart(),
    });
    this.resetPromo();
    this.toast(this.L('Cart cleared', 'تم تفريغ السلة'));
  }

  applyPromo(): void {
    if (this.promoCode.trim().toUpperCase() === this.VALID_PROMO) {
      this.promoApplied.set(true);
      this.promoError.set(false);
      this.toast(this.L('Promo code applied - 10% off!', 'تم تطبيق الخصم - ١٠٪'));
    } else {
      this.promoError.set(true);
      setTimeout(() => this.promoError.set(false), 2800);
    }
  }

  removePromo(): void {
    this.promoApplied.set(false);
    this.promoCode = '';
    this.toast(this.L('Promo code removed', 'تم حذف كود الخصم'));
  }

  checkout(): void {
    if (this.items().length === 0 || this.checkingOut()) return;
    this.checkingOut.set(true);
    this.toast(this.L('Placing your reservation...', 'جار تأكيد الحجز...'));
    this.orders.checkout().subscribe({
      next: res => {
        this.checkingOut.set(false);
        if (res.success && res.data) {
          // Reservation placed - show the pickup code and empty the cart view.
          this.pickupCode.set(res.data.pickupCode);
          this.items.set([]);
          this.cartState.clear();
          this.resetPromo();
          this.toast(this.L('Reservation confirmed!', 'تم تأكيد الحجز!'));
        } else {
          this.toast(res.message || this.L('Checkout failed.', 'فشل تأكيد الحجز.'));
        }
      },
      error: err => {
        this.checkingOut.set(false);
        this.toast(err?.error?.message || this.L('Checkout failed.', 'فشل تأكيد الحجز.'));
      },
    });
  }

  viewOrders(): void {
    this.closeCart();
    this.router.navigate(['/profile']);
  }

  startShopping(): void {
    this.closeCart();
  }

  goLogin(): void {
    const returnUrl = this.router.url || '/';
    this.closeCart();
    this.router.navigate(['/login'], { queryParams: { returnUrl } });
  }

  onItemImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.style.display = 'none';
    img.parentElement?.classList.add('image-failed');
  }

  trackById(_index: number, item: CartItem): number {
    return item.id;
  }

  private toast(msg: string): void {
    this.toastMsg.set(msg);
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toastMsg.set(null), 3000);
  }

  private resetPromo(): void {
    this.promoCode = '';
    this.promoApplied.set(false);
    this.promoError.set(false);
  }
}
