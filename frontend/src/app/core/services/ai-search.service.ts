import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { AiSearchRequest, AiSearchResponse } from '../models/catalog.models';

// Talks to POST /api/ai/search (natural-language shopping search).
@Injectable({ providedIn: 'root' })
export class AiSearchService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/ai`;

  search(payload: AiSearchRequest): Observable<AiSearchResponse> {
    return this.http
      .post<ApiResult<AiSearchResponse>>(`${this.base}/search`, payload)
      .pipe(map(res => res.data!));
  }
}
