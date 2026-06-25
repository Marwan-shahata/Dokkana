import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  AiProductResult,
  AiRankedResult,
  AiSearchRequest,
  AiSearchResponse,
} from '../models/catalog.models';
import { toNullableMediaUrl } from './media-url';

// Talks to POST /api/ai/search (natural-language shopping search).
@Injectable({ providedIn: 'root' })
export class AiSearchService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/ai`;

  search(payload: AiSearchRequest): Observable<AiSearchResponse> {
    return this.http
      .post<ApiResult<AiSearchResponse>>(`${this.base}/search`, payload)
      .pipe(map(res => this.normalizeResponse(res.data!)));
  }

  // Visual search: upload a photo, get the most visually-similar products.
  searchByImage(file: File, userLat?: number, userLng?: number): Observable<AiSearchResponse> {
    const form = new FormData();
    form.append('image', file);
    if (userLat != null) form.append('userLat', String(userLat));
    if (userLng != null) form.append('userLng', String(userLng));
    return this.http
      .post<ApiResult<AiSearchResponse>>(`${this.base}/search/image`, form)
      .pipe(map(res => this.normalizeResponse(res.data!)));
  }

  private normalizeResponse(response: AiSearchResponse): AiSearchResponse {
    const localResults = response.localResults.map(product => this.normalizeProduct(product));

    return {
      ...response,
      localResults,
      rankedResults: response.rankedResults.map(result => this.normalizeRankedResult(result)),
    };
  }

  private normalizeProduct(product: AiProductResult): AiProductResult {
    return {
      ...product,
      imageUrl: toNullableMediaUrl(product.imageUrl),
    };
  }

  private normalizeRankedResult(result: AiRankedResult): AiRankedResult {
    const product = result.product ? this.normalizeProduct(result.product) : result.product;
    return {
      ...result,
      imageUrl: toNullableMediaUrl(result.imageUrl),
      product,
    };
  }
}
