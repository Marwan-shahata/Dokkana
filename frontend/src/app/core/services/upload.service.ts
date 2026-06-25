import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';

interface UploadResponse {
  url: string;
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/uploads`;

  uploadImage(file: File, folder = 'products'): Observable<string> {
    const form = new FormData();
    form.append('file', file);
    return this.http
      .post<ApiResult<UploadResponse>>(`${this.base}/images?folder=${encodeURIComponent(folder)}`, form)
      .pipe(map(res => res.data!.url));
  }
}
