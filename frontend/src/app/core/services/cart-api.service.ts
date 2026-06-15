import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { CartDTO } from '../models/order.models';

// Backend-persisted cart (/api/cart). Requires auth - the JWT interceptor adds
// the bearer token. The local signal-based CartService remains for the navbar
// badge; this service is the source of truth for the cart page + checkout.
@Injectable({ providedIn: 'root' })
export class CartApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/cart`;

  get(): Observable<CartDTO> {
    return this.http.get<ApiResult<CartDTO>>(this.base).pipe(map(r => r.data!));
  }

  addItem(productId: number, quantity = 1): Observable<ApiResult<CartDTO>> {
    return this.http.post<ApiResult<CartDTO>>(`${this.base}/items`, { productId, quantity });
  }

  updateItem(productId: number, quantity: number): Observable<ApiResult<CartDTO>> {
    return this.http.put<ApiResult<CartDTO>>(`${this.base}/items/${productId}`, { quantity });
  }

  removeItem(productId: number): Observable<ApiResult<CartDTO>> {
    return this.http.delete<ApiResult<CartDTO>>(`${this.base}/items/${productId}`);
  }

  clear(): Observable<ApiResult<CartDTO>> {
    return this.http.delete<ApiResult<CartDTO>>(this.base);
  }
}
