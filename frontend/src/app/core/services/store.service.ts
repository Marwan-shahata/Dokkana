import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  Store,
  StoreCreateRequest,
  StoreDetails,
  StoreTier,
  StoreUpdateRequest,
} from '../models/catalog.models';
import { toNullableMediaUrl } from './media-url';

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
      .pipe(map(res => (res.data ?? []).map(store => this.normalizeStore(store))));
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
      .pipe(map(res => (res.data ?? []).map(store => this.normalizeStore(store))));
  }

  getById(id: number): Observable<StoreDetails> {
    return this.http
      .get<ApiResult<StoreDetails>>(`${this.base}/${id}`)
      .pipe(map(res => this.normalizeStoreDetails(res.data!)));
  }

  // StoreOwner only: requires a valid JWT (added by the auth interceptor).
  getMy(): Observable<Store> {
    return this.http
      .get<ApiResult<Store>>(`${this.base}/my`)
      .pipe(map(res => this.normalizeStore(res.data!)));
  }

  create(payload: StoreCreateRequest): Observable<Store> {
    return this.http
      .post<ApiResult<Store>>(this.base, payload)
      .pipe(map(res => this.normalizeStore(res.data!)));
  }

  update(id: number, payload: StoreUpdateRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}`, payload);
  }

  // StoreOwner upgrades/downgrades their own plan ("Upgrade to Pro");
  // Admin may set any store's plan. PUT /api/stores/{id}/tier.
  setTier(id: number, tier: StoreTier): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}/tier`, { tier });
  }

  private normalizeStore(store: Store): Store {
    return {
      ...store,
      logoUrl: toNullableMediaUrl(store.logoUrl),
    };
  }

  private normalizeStoreDetails(store: StoreDetails): StoreDetails {
    return {
      ...store,
      logoUrl: toNullableMediaUrl(store.logoUrl),
    };
  }
}
