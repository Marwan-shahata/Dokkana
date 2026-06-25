import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { CartDTO } from '../models/order.models';
import { toNullableMediaUrl } from './media-url';

// Backend-persisted cart (/api/cart). Requires auth - the JWT interceptor adds
// the bearer token. The local signal-based CartService remains for the navbar
// badge; this service is the source of truth for the cart page + checkout.
@Injectable({ providedIn: 'root' })
export class CartApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/cart`;

  get(): Observable<CartDTO> {
    return this.http
      .get<ApiResult<CartDTO>>(this.base)
      .pipe(map(r => this.normalizeCart(r.data!)));
  }

  addItem(productId: number, quantity = 1): Observable<ApiResult<CartDTO>> {
    return this.http
      .post<ApiResult<CartDTO>>(`${this.base}/items`, { productId, quantity })
      .pipe(map(r => this.normalizeCartResult(r)));
  }

  updateItem(productId: number, quantity: number): Observable<ApiResult<CartDTO>> {
    return this.http
      .put<ApiResult<CartDTO>>(`${this.base}/items/${productId}`, { quantity })
      .pipe(map(r => this.normalizeCartResult(r)));
  }

  removeItem(productId: number): Observable<ApiResult<CartDTO>> {
    return this.http
      .delete<ApiResult<CartDTO>>(`${this.base}/items/${productId}`)
      .pipe(map(r => this.normalizeCartResult(r)));
  }

  clear(): Observable<ApiResult<CartDTO>> {
    return this.http
      .delete<ApiResult<CartDTO>>(this.base)
      .pipe(map(r => this.normalizeCartResult(r)));
  }

  private normalizeCartResult(result: ApiResult<CartDTO>): ApiResult<CartDTO> {
    return result.data
      ? { ...result, data: this.normalizeCart(result.data) }
      : result;
  }

  private normalizeCart(cart: CartDTO): CartDTO {
    return {
      ...cart,
      items: cart.items.map(item => ({
        ...item,
        imageUrl: toNullableMediaUrl(item.imageUrl),
      })),
    };
  }
}
