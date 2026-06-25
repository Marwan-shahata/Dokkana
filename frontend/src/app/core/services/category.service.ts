import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { Category, CategoryCreateRequest } from '../models/catalog.models';
import { toNullableMediaUrl } from './media-url';

// Talks to /api/categories. Reads are public; create/update/delete require Admin
// (enforced server-side; the JWT interceptor attaches the token).
@Injectable({ providedIn: 'root' })
export class CategoryService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/categories`;

  getAll(): Observable<Category[]> {
    return this.http
      .get<ApiResult<Category[]>>(this.base)
      .pipe(map(res => (res.data ?? []).map(category => this.normalizeCategory(category))));
  }

  getById(id: number): Observable<Category> {
    return this.http
      .get<ApiResult<Category>>(`${this.base}/${id}`)
      .pipe(map(res => this.normalizeCategory(res.data!)));
  }

  create(payload: CategoryCreateRequest): Observable<Category> {
    return this.http
      .post<ApiResult<Category>>(this.base, payload)
      .pipe(map(res => this.normalizeCategory(res.data!)));
  }

  update(id: number, payload: CategoryCreateRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<ApiResult<unknown>> {
    return this.http.delete<ApiResult<unknown>>(`${this.base}/${id}`);
  }

  private normalizeCategory(category: Category): Category {
    return {
      ...category,
      imageUrl: toNullableMediaUrl(category.imageUrl),
    };
  }
}
