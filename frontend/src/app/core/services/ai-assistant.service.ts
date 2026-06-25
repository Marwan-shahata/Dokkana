import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  BusinessCopilotResponse,
  CatalogQuestionResponse,
  CatalogUploadResponse,
} from '../models/ai-assistant.models';

// Talks to the merchant AI endpoints: Business Copilot and Catalog RAG.
@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/ai`;

  // POST /api/ai/copilot/ask — store-owner sales/stock question.
  askCopilot(storeId: number, question: string): Observable<BusinessCopilotResponse> {
    return this.http
      .post<ApiResult<BusinessCopilotResponse>>(`${this.base}/copilot/ask`, { storeId, question })
      .pipe(map(res => res.data!));
  }

  // POST /api/ai/catalog/upload — multipart PDF upload for the store.
  uploadCatalog(storeId: number, file: File): Observable<CatalogUploadResponse> {
    const form = new FormData();
    form.append('storeId', String(storeId));
    form.append('file', file);
    return this.http
      .post<ApiResult<CatalogUploadResponse>>(`${this.base}/catalog/upload`, form)
      .pipe(map(res => res.data!));
  }

  // POST /api/ai/catalog/ask — ask a question over the store's uploaded catalog.
  askCatalog(storeId: number, question: string, maxChunks = 5): Observable<CatalogQuestionResponse> {
    return this.http
      .post<ApiResult<CatalogQuestionResponse>>(`${this.base}/catalog/ask`, { storeId, question, maxChunks })
      .pipe(map(res => res.data!));
  }
}
