import { Injectable, signal } from '@angular/core';
import { CartDTO } from '../models/order.models';

export interface CartItem {
  id: number;
  name: string;
  store: string;
  price: number;
  image: string;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  readonly items = signal<CartItem[]>([]);
  readonly count = signal(0);
  readonly total = signal(0);

  setFromCart(cart: CartDTO): void {
    this.items.set(cart.items.map(i => ({
      id: i.productId,
      name: i.title,
      store: i.storeName,
      price: i.unitPrice,
      image: i.imageUrl ?? '',
    })));
    this.count.set(cart.totalItems);
    this.total.set(cart.totalAmount);
  }

  add(item: CartItem): void {
    this.items.update(list => [...list, item]);
    this.count.update(v => v + 1);
    this.total.update(v => v + item.price);
  }

  clear(): void {
    this.items.set([]);
    this.count.set(0);
    this.total.set(0);
  }
}
