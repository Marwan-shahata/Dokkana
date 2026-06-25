import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import {
  AlertDTO,
  AiProductSuggestionDTO,
  BillingSummaryDTO,
  CreateAlertRequest,
  CreateFeaturedPromotionRequest,
  CreateLeadRequest,
  CreateNotificationPromotionRequest,
  MerchantInsightsDTO,
  MerchantLeadDTO,
  NotificationPreferencesDTO,
  PromotionDTO,
  ScheduleSaleRequest,
  TrendingQueryDTO,
} from '../models/monetization.models';
import { toNullableMediaUrl } from './media-url';

@Injectable({ providedIn: 'root' })
export class MonetizationService {
  private http = inject(HttpClient);
  private api = environment.apiUrl;

  getMyCampaigns(): Observable<PromotionDTO[]> {
    return this.http
      .get<ApiResult<PromotionDTO[]>>(`${this.api}/promotions/mine`)
      .pipe(map(res => (res.data ?? []).map(c => this.normalizeCampaign(c))));
  }

  createFeatured(payload: CreateFeaturedPromotionRequest): Observable<PromotionDTO> {
    return this.http
      .post<ApiResult<PromotionDTO>>(`${this.api}/promotions/featured`, payload)
      .pipe(map(res => this.normalizeCampaign(res.data!)));
  }

  createNotification(payload: CreateNotificationPromotionRequest): Observable<PromotionDTO> {
    return this.http
      .post<ApiResult<PromotionDTO>>(`${this.api}/promotions/notification`, payload)
      .pipe(map(res => this.normalizeCampaign(res.data!)));
  }

  getAdminCampaigns(status?: string): Observable<PromotionDTO[]> {
    const suffix = status ? `?status=${encodeURIComponent(status)}` : '';
    return this.http
      .get<ApiResult<PromotionDTO[]>>(`${this.api}/promotions/admin${suffix}`)
      .pipe(map(res => (res.data ?? []).map(c => this.normalizeCampaign(c))));
  }

  approveCampaign(id: number): Observable<PromotionDTO> {
    return this.http
      .put<ApiResult<PromotionDTO>>(`${this.api}/promotions/${id}/approve`, {})
      .pipe(map(res => this.normalizeCampaign(res.data!)));
  }

  rejectCampaign(id: number, reason?: string): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.api}/promotions/${id}/reject`, { reason });
  }

  sponsoredProducts(limit = 3): Observable<PromotionDTO[]> {
    return this.http
      .get<ApiResult<PromotionDTO[]>>(`${this.api}/promotions/sponsored-products?limit=${limit}`)
      .pipe(map(res => (res.data ?? []).map(c => this.normalizeCampaign(c))));
  }

  sponsoredStores(limit = 3): Observable<PromotionDTO[]> {
    return this.http
      .get<ApiResult<PromotionDTO[]>>(`${this.api}/promotions/sponsored-stores?limit=${limit}`)
      .pipe(map(res => (res.data ?? []).map(c => this.normalizeCampaign(c))));
  }

  trackPromotionClick(id: number): Observable<ApiResult<unknown>> {
    return this.http.post<ApiResult<unknown>>(`${this.api}/promotions/${id}/click`, {});
  }

  getBilling(): Observable<BillingSummaryDTO> {
    return this.http.get<ApiResult<BillingSummaryDTO>>(`${this.api}/billing`).pipe(map(res => res.data!));
  }

  topUp(amount: number): Observable<BillingSummaryDTO> {
    return this.http
      .post<ApiResult<BillingSummaryDTO>>(`${this.api}/billing/top-up`, { amount, note: 'Demo top-up' })
      .pipe(map(res => res.data!));
  }

  createLead(payload: CreateLeadRequest): Observable<MerchantLeadDTO> {
    return this.http.post<ApiResult<MerchantLeadDTO>>(`${this.api}/leads`, payload).pipe(map(res => res.data!));
  }

  getMyLeads(): Observable<MerchantLeadDTO[]> {
    return this.http.get<ApiResult<MerchantLeadDTO[]>>(`${this.api}/leads/mine`).pipe(map(res => res.data ?? []));
  }

  createAlert(payload: CreateAlertRequest): Observable<AlertDTO> {
    return this.http.post<ApiResult<AlertDTO>>(`${this.api}/alerts`, payload).pipe(map(res => res.data!));
  }

  getAlerts(): Observable<AlertDTO[]> {
    return this.http.get<ApiResult<AlertDTO[]>>(`${this.api}/alerts`).pipe(map(res => res.data ?? []));
  }

  getNotificationPreferences(): Observable<NotificationPreferencesDTO> {
    return this.http
      .get<ApiResult<NotificationPreferencesDTO>>(`${this.api}/preferences/notifications`)
      .pipe(map(res => res.data!));
  }

  setNotificationPreferences(payload: NotificationPreferencesDTO): Observable<NotificationPreferencesDTO> {
    return this.http
      .put<ApiResult<NotificationPreferencesDTO>>(`${this.api}/preferences/notifications`, payload)
      .pipe(map(res => res.data!));
  }

  getTrending(limit = 8): Observable<TrendingQueryDTO[]> {
    return this.http.get<ApiResult<TrendingQueryDTO[]>>(`${this.api}/trends?limit=${limit}`).pipe(map(res => res.data ?? []));
  }

  aiSuggest(imageUrl?: string | null, fileName?: string | null): Observable<AiProductSuggestionDTO> {
    return this.http
      .post<ApiResult<AiProductSuggestionDTO>>(`${this.api}/products/ai-suggest`, { imageUrl, fileName })
      .pipe(map(res => res.data!));
  }

  scheduleSale(productId: number, payload: ScheduleSaleRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.api}/products/${productId}/sale`, payload);
  }

  // MM3 — Pro-only demand forecast + pricing hints for the owner's store.
  getInsights(): Observable<MerchantInsightsDTO> {
    return this.http.get<ApiResult<MerchantInsightsDTO>>(`${this.api}/insights`).pipe(map(res => res.data!));
  }

  private normalizeCampaign(c: PromotionDTO): PromotionDTO {
    return {
      ...c,
      productImageUrl: toNullableMediaUrl(c.productImageUrl),
    };
  }
}
