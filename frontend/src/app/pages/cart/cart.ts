import { 
  Component,
  OnDestroy,
  HostListener,
  signal,
  computed,
  Output,
  EventEmitter,
  Input,
 } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';

export interface CartItem {
  id: number;
  name: string;
  brand: string;
  category: string;
  price: number;
  currency: string;
  quantity: number;
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
    constructor(private title: Title) {}

  // ── Drawer state ──────────────────────────────────────────
  @Input() isOpen    = signal(false);
  isLeaving = signal(false);

  // ── Toast ─────────────────────────────────────────────────
  toastMsg  = signal<string | null>(null);
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Promo ─────────────────────────────────────────────────
  promoCode  = '';
  promoApplied = signal(false);
  promoError   = signal(false);
  readonly VALID_PROMO = 'DOKKANA10';
  readonly PROMO_DISCOUNT = 0.10; // 10%

  // ── Delivery ──────────────────────────────────────────────
  readonly FREE_DELIVERY_THRESHOLD = 500;
  readonly DELIVERY_FEE = 49;

  // ── Cart items ────────────────────────────────────────────
  items = signal<CartItem[]>([
    {
      id: 1,
      name: 'Linen Summer Dress',
      brand: 'Boutique El Nour',
      category: 'Dresses',
      price: 480,
      currency: 'EGP',
      quantity: 1,
      size: 'M',
      color: '#c89f82',
      gradient: 'linear-gradient(135deg, oklch(0.62 0.15 42/0.65), oklch(0.5 0.12 39/0.8))',
    },
    {
      id: 2,
      name: 'Canvas Sneakers',
      brand: 'Step & Style',
      category: 'Shoes',
      price: 650,
      currency: 'EGP',
      quantity: 1,
      size: '39',
      color: '#2c2c2c',
      gradient: 'linear-gradient(135deg, oklch(0.55 0.08 220/0.6), oklch(0.42 0.07 210/0.75))',
    },
    {
      id: 3,
      name: 'Tan Leather Tote',
      brand: 'Cairo Craft',
      category: 'Bags',
      price: 920,
      currency: 'EGP',
      quantity: 1,
      color: '#b8935a',
      gradient: 'linear-gradient(135deg, oklch(0.68 0.1 55/0.65), oklch(0.55 0.12 45/0.8))',
    },
  ]);

  // ── Computed totals ───────────────────────────────────────
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

  // ── Lifecycle ─────────────────────────────────────────────
  ngOnDestroy(): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen()) this.closeCart();
  }

  // ── Open / Close ──────────────────────────────────────────
  openCart(): void {
    this.isOpen.set(true);
    this.isLeaving.set(false);
    document.body.style.overflow = 'hidden';
  }

 
 @Output() close = new EventEmitter<void>();
 
  closeCart(): void {
    this.close.emit();
    this.isLeaving.set(true);
    setTimeout(() => {
      this.isOpen.set(false);
      this.isLeaving.set(false);
      document.body.style.overflow = '';
    }, 380);
  }

  // ── Item management ───────────────────────────────────────
  addItem(item: Omit<CartItem, 'removing'>): void {
    const existing = this.items().find(i => i.id === item.id);
    if (existing) {
      this.increaseQuantity(existing.id);
    } else {
      this.items.update(list => [...list, { ...item, removing: false }]);
    }
    this.toast(`${item.name} added to cart`);
  }

  removeItem(id: number): void {
    // Animate out first
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, removing: true } : i)
    );
    const name = this.items().find(i => i.id === id)?.name ?? 'Item';
    setTimeout(() => {
      this.items.update(list => list.filter(i => i.id !== id));
    }, 340);
    this.toast(`${name} removed`);
  }

  increaseQuantity(id: number): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, quantity: Math.min(i.quantity + 1, 20) } : i)
    );
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
    }
  }

  calculateTotal(): number {
    return this.total();
  }

  clearCart(): void {
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
    this.toast('Cart cleared');
  }

  applyPromo(): void {
    if (this.promoCode.trim().toUpperCase() === this.VALID_PROMO) {
      this.promoApplied.set(true);
      this.promoError.set(false);
      this.toast('Promo code applied — 10% off!');
    } else {
      this.promoError.set(true);
      setTimeout(() => this.promoError.set(false), 2800);
    }
  }

  removePromo(): void {
    this.promoApplied.set(false);
    this.promoCode = '';
    this.toast('Promo code removed');
  }

  checkout(): void {
    this.toast('Redirecting to checkout…');
    setTimeout(() => this.closeCart(), 1200);
  }

  startShopping(): void {
    this.closeCart();
  }

  trackById(_index: number, item: CartItem): number {
    return item.id;
  }

  private toast(msg: string): void {
    this.toastMsg.set(msg);
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toastMsg.set(null), 3000);
  }

  ngOnInit(): void  {
    this.title.setTitle('Dokana | Cart');
  }


}
