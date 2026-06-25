export type PromotionType = 'FeaturedProduct' | 'SponsoredNotification' | 'FeaturedStore';
export type PromotionStatus = 'Pending' | 'Approved' | 'Rejected' | 'Active' | 'Ended';
export type PromotionAudience = 'Followers' | 'AllShoppers' | 'WishlistMatches' | 'RecentSearch' | 'Nearby';

export interface PromotionDTO {
  id: number;
  storeId: number;
  storeName: string;
  type: PromotionType | string;
  status: PromotionStatus | string;
  productId?: number | null;
  productTitle?: string | null;
  productPrice?: number | null;
  productOriginalPrice?: number | null;
  isProductOnSale?: boolean;
  productImageUrl?: string | null;
  title?: string | null;
  message?: string | null;
  audience: PromotionAudience | string;
  budget: number;
  impressions: number;
  clicks: number;
  recipientsCount: number;
  rejectionReason?: string | null;
  createdAt: string;
  approvedAt?: string | null;
  startAt?: string | null;
  endAt?: string | null;
}

export interface CreateFeaturedPromotionRequest {
  productId?: number | null;
  budget: number;
  durationDays: number;
}

export interface CreateNotificationPromotionRequest {
  title: string;
  message: string;
  audience: PromotionAudience | string;
  budget: number;
  durationDays: number;
}

export interface BillingSummaryDTO {
  balance: number;
  transactions: WalletTransactionDTO[];
}

export interface WalletTransactionDTO {
  id: number;
  amount: number;
  type: string;
  note?: string | null;
  createdAt: string;
}

export interface MerchantLeadDTO {
  id: number;
  storeId: number;
  storeName: string;
  productId?: number | null;
  productTitle?: string | null;
  type: string;
  customerName?: string | null;
  customerEmail?: string | null;
  createdAt: string;
}

export interface CreateLeadRequest {
  storeId: number;
  productId?: number | null;
  type: string;
}

export interface AlertDTO {
  id: number;
  type: string;
  productId?: number | null;
  productTitle?: string | null;
  query?: string | null;
  targetPrice?: number | null;
  isActive: boolean;
  createdAt: string;
  lastTriggeredAt?: string | null;
}

export interface CreateAlertRequest {
  productId?: number | null;
  type: string;
  query?: string | null;
  targetPrice?: number | null;
}

export interface NotificationPreferencesDTO {
  sponsoredNotificationsEnabled: boolean;
}

export interface TrendingQueryDTO {
  query: string;
  count: number;
}

export interface AiProductSuggestionDTO {
  title: string;
  description: string;
  color: string;
  size: string;
  brand: string;
  gender: string;
  categoryName: string;
}

export interface ScheduleSaleRequest {
  salePrice: number;
  startsAt?: string | null;
  endsAt?: string | null;
}

// MM3 — Pro-only merchant insights.
export interface DemandForecastDTO {
  productId: number;
  productTitle: string;
  stock: number;
  unitsSold30d: number;
  daysToSellout?: number | null;
  severity: string; // urgent | soon | ok
}

export interface PricingHintDTO {
  productId: number;
  productTitle: string;
  price: number;
  marketLow: number;
  marketHigh: number;
  marketMedian: number;
  verdict: string; // high | low | fair
}

export interface MerchantInsightsDTO {
  isPro: boolean;
  forecasts: DemandForecastDTO[];
  pricingHints: PricingHintDTO[];
}
