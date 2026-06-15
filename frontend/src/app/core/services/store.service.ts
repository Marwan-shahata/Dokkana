import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  Store,
  StoreCreateRequest,
  StoreDetails,
  StoreUpdateRequest,
} from '../models/catalog.models';

// Talks to /api/stores. Public reads (approved list + details) plus the
// owner's own store and merchant profile mutations.
@Injectable({ providedIn: 'root' })
export class StoreService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/stores`;

  // Admin only: all stores including unapproved (GET /api/stores).
  getAll(): Observable<Store[]> {
    return this.http
      .get<ApiResult<Store[]>>(this.base)
      .pipe(map(res => res.data ?? []));
  }

  // Admin only: approve a pending store.
  approve(id: number): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}/approve`, {});
  }

  // Admin only: delete a store.
  delete(id: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/${id}`);
  }

  getApproved(): Observable<Store[]> {
    return this.http
      .get<ApiResult<Store[]>>(`${this.base}/approved`)
      .pipe(map(res => res.data ?? []));
  }

  getById(id: number): Observable<StoreDetails> {
    return this.http
      .get<ApiResult<StoreDetails>>(`${this.base}/${id}`)
      .pipe(map(res => res.data!));
  }

  // StoreOwner only: requires a valid JWT (added by the auth interceptor).
  getMy(): Observable<Store> {
    return this.http
      .get<ApiResult<Store>>(`${this.base}/my`)
      .pipe(map(res => res.data!));
  }

  create(payload: StoreCreateRequest): Observable<Store> {
    return this.http
      .post<ApiResult<Store>>(this.base, payload)
      .pipe(map(res => res.data!));
  }

  update(id: number, payload: StoreUpdateRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}`, payload);
  }
}
