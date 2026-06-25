import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  PagedResult,
  ProductCreateRequest,
  ProductDetails,
  ProductListItem,
  ProductQuery,
  ProductUpdateRequest,
} from '../models/catalog.models';
import { toNullableMediaUrl } from './media-url';

// Talks to /api/products. Unwraps the GeneralResult<T> envelope so callers
// receive plain data and let the HTTP error path surface failures.
@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/products`;

  getAll(query: ProductQuery = {}): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<ApiResult<PagedResult<ProductListItem>>>(this.base, { params })
      .pipe(map(res => this.normalizePage(res.data!)));
  }

  getById(id: number): Observable<ProductDetails> {
    return this.http
      .get<ApiResult<ProductDetails>>(`${this.base}/${id}`)
      .pipe(map(res => this.normalizeDetails(res.data!)));
  }

  getByStore(
    storeId: number,
    page = 1,
    pageSize = 10,
  ): Observable<PagedResult<ProductListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http
      .get<ApiResult<PagedResult<ProductListItem>>>(`${this.base}/store/${storeId}`, { params })
      .pipe(map(res => this.normalizePage(res.data!)));
  }

  create(payload: ProductCreateRequest): Observable<ProductDetails> {
    return this.http
      .post<ApiResult<ProductDetails>>(this.base, payload)
      .pipe(map(res => this.normalizeDetails(res.data!)));
  }

  update(id: number, payload: ProductUpdateRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/${id}`);
  }

  private normalizePage(page: PagedResult<ProductListItem>): PagedResult<ProductListItem> {
    return {
      ...page,
      items: page.items.map(item => this.normalizeListItem(item)),
    };
  }

  private normalizeListItem(item: ProductListItem): ProductListItem {
    return {
      ...item,
      imageUrl: toNullableMediaUrl(item.imageUrl),
    };
  }

  private normalizeDetails(product: ProductDetails): ProductDetails {
    return {
      ...product,
      imageUrl: toNullableMediaUrl(product.imageUrl),
    };
  }
}
