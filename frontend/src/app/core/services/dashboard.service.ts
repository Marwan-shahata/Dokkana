import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { AdminStatsDTO, SellerStatsDTO } from '../models/dashboard.models';

// Server-computed dashboard KPIs (/api/admin/stats, /api/seller/stats).
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getAdminStats(): Observable<AdminStatsDTO | null> {
    return this.http
      .get<ApiResult<AdminStatsDTO>>(`${this.base}/admin/stats`)
      .pipe(map(res => res.data ?? null));
  }

  getSellerStats(): Observable<SellerStatsDTO | null> {
    return this.http
      .get<ApiResult<SellerStatsDTO>>(`${this.base}/seller/stats`)
      .pipe(map(res => res.data ?? null));
  }
}
