import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { OrderAdminSummaryDTO, OrderDTO, OrderSummaryDTO } from '../models/order.models';

// Orders / reservations (/api/orders). Checkout turns the cart into a
// reservation and returns a pickup code.
@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/orders`;

  checkout(): Observable<ApiResult<OrderDTO>> {
    return this.http.post<ApiResult<OrderDTO>>(`${this.base}/checkout`, {});
  }

  getMine(): Observable<OrderSummaryDTO[]> {
    return this.http.get<ApiResult<OrderSummaryDTO[]>>(this.base).pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<OrderDTO> {
    return this.http.get<ApiResult<OrderDTO>>(`${this.base}/${id}`).pipe(map(r => r.data!));
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
}
