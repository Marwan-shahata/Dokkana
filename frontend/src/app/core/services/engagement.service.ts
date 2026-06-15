import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  AuditLogDTO,
  FollowedStoreDTO,
  ProductReviewCreateRequest,
  ProductReviewDTO,
  ReviewCountDTO,
  WishlistItemDTO,
} from '../models/engagement.models';

@Injectable({ providedIn: 'root' })
export class EngagementService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getWishlist(): Observable<WishlistItemDTO[]> {
    return this.http
      .get<ApiResult<WishlistItemDTO[]>>(`${this.base}/wishlist`)
      .pipe(map(res => res.data ?? []));
  }

  addToWishlist(productId: number): Observable<ApiResult<unknown>> {
    return this.http.post<ApiResult<unknown>>(`${this.base}/wishlist/${productId}`, {});
  }

  removeFromWishlist(productId: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/wishlist/${productId}`);
  }

  getSavedStores(): Observable<FollowedStoreDTO[]> {
    return this.http
      .get<ApiResult<FollowedStoreDTO[]>>(`${this.base}/saved-stores`)
      .pipe(map(res => res.data ?? []));
  }

  followStore(storeId: number): Observable<ApiResult<unknown>> {
    return this.http.post<ApiResult<unknown>>(`${this.base}/saved-stores/${storeId}`, {});
  }

  unfollowStore(storeId: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/saved-stores/${storeId}`);
  }

  getProductReviews(productId: number): Observable<ProductReviewDTO[]> {
    return this.http
      .get<ApiResult<ProductReviewDTO[]>>(`${this.base}/products/${productId}/reviews`)
      .pipe(map(res => res.data ?? []));
  }

  saveProductReview(productId: number, payload: ProductReviewCreateRequest): Observable<ApiResult<ProductReviewDTO>> {
    return this.http.post<ApiResult<ProductReviewDTO>>(`${this.base}/products/${productId}/reviews`, payload);
  }

  deleteProductReview(productId: number, reviewId: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/products/${productId}/reviews/${reviewId}`);
  }

  getMyReviewCount(): Observable<number> {
    return this.http
      .get<ApiResult<ReviewCountDTO>>(`${this.base}/reviews/mine/count`)
      .pipe(map(res => res.data?.count ?? 0));
  }

  getAuditLogs(limit = 100): Observable<AuditLogDTO[]> {
    return this.http
      .get<ApiResult<AuditLogDTO[]>>(`${this.base}/audit-logs?limit=${limit}`)
      .pipe(map(res => res.data ?? []));
  }
}
