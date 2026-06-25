import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { OrderAdminSummaryDTO, OrderDTO, OrderSummaryDTO, StoreOrderDTO } from '../models/order.models';
import { toNullableMediaUrl } from './media-url';

// Orders / reservations (/api/orders). Checkout turns the cart into a
// reservation and returns a pickup code.
@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/orders`;

  checkout(): Observable<ApiResult<OrderDTO>> {
    return this.http
      .post<ApiResult<OrderDTO>>(`${this.base}/checkout`, {})
      .pipe(map(r => this.normalizeOrderResult(r)));
  }

  getMine(): Observable<OrderSummaryDTO[]> {
    return this.http.get<ApiResult<OrderSummaryDTO[]>>(this.base).pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<OrderDTO> {
    return this.http
      .get<ApiResult<OrderDTO>>(`${this.base}/${id}`)
      .pipe(map(r => this.normalizeOrder(r.data!)));
  }

  // Admin
  getAllAdmin(): Observable<OrderAdminSummaryDTO[]> {
    return this.http
      .get<ApiResult<OrderAdminSummaryDTO[]>>(`${this.base}/all`)
      .pipe(map(r => r.data ?? []));
  }

  updateStatus(id: number, status: string): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}/status`, { status });
  }

  // Store owner
  getStoreOrders(): Observable<StoreOrderDTO[]> {
    return this.http
      .get<ApiResult<StoreOrderDTO[]>>(`${this.base}/store`)
      .pipe(map(r => (r.data ?? []).map(order => this.normalizeStoreOrder(order))));
  }

  updateStoreOrderStatus(id: number, status: string): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/store/${id}/status`, { status });
  }

  private normalizeOrderResult(result: ApiResult<OrderDTO>): ApiResult<OrderDTO> {
    return result.data
      ? { ...result, data: this.normalizeOrder(result.data) }
      : result;
  }

  private normalizeOrder(order: OrderDTO): OrderDTO {
    return {
      ...order,
      items: order.items.map(item => ({
        ...item,
        imageUrl: toNullableMediaUrl(item.imageUrl),
      })),
    };
  }

  private normalizeStoreOrder(order: StoreOrderDTO): StoreOrderDTO {
    return {
      ...order,
      items: order.items.map(item => ({
        ...item,
        imageUrl: toNullableMediaUrl(item.imageUrl),
      })),
    };
  }
}
