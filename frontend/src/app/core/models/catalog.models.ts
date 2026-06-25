// Catalog-related types mirroring the backend DTOs in Dokkana.BLL/DTOs.
// Used by the product/category/store/AI services.

// Mirrors Dokkana.Common.PagedResult<T>.
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Mirrors ProductListDTO.
export interface ProductListItem {
  id: number;
  title: string;
  price: number;
  count: number;
  imageUrl?: string | null;
  originalPrice?: number | null;
  salePrice?: number | null;
  saleEndsAt?: string | null;
  isOnSale?: boolean;
  color?: string | null;
  size?: string | null;
  brand?: string | null;
  gender?: string | null;
  categoryName: string;
  storeId: number;
  storeName: string;
}

// Mirrors ProductDetailsDTO.
export interface ProductDetails {
  id: number;
  title: string;
  description?: string | null;
  price: number;
  count: number;
  imageUrl?: string | null;
  originalPrice?: number | null;
  salePrice?: number | null;
  saleStartsAt?: string | null;
  saleEndsAt?: string | null;
  isOnSale?: boolean;
  color?: string | null;
  size?: string | null;
  brand?: string | null;
  gender?: string | null;
  categoryId: number;
  categoryName: string;
  storeId: number;
  storeName: string;
}

// Mirrors CategoryDTO.
export interface Category {
  id: number;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
}

// A store's commercial plan. Mirrors Dokkana.DAL.Models.StoreTier
// (serialized by the API as the string "Free" or "Pro").
export type StoreTier = 'Free' | 'Pro';

// Mirrors StoreDTO.
export interface Store {
  id: number;
  name: string;
  description?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  isApproved: boolean;
  tier?: StoreTier;
  subscriptionStatus?: string | null;
  subscriptionRenewsAt?: string | null;
  walletBalance?: number;
  whatsAppPhone?: string | null;
  productCount?: number;
}

// Mirrors StoreDetailsDTO.
export interface StoreDetails extends Store {
  ownerName?: string | null;
  productCount: number;
}

export interface StoreCreateRequest {
  name: string;
  description?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  whatsAppPhone?: string | null;
}

export interface StoreUpdateRequest {
  name?: string | null;
  description?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  whatsAppPhone?: string | null;
}

// Body for PUT /api/stores/{id}/tier — sets the store's plan.
export interface StoreTierUpdateRequest {
  tier: StoreTier;
}

// Mirrors CategoryCreateDTO (used for both create and update).
export interface CategoryCreateRequest {
  name: string;
  description?: string | null;
  imageUrl?: string | null;
}

// Query params accepted by GET /api/products.
export interface ProductQuery {
  search?: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  color?: string;
  gender?: string;
  page?: number;
  pageSize?: number;
}

export interface ProductCreateRequest {
  title: string;
  description?: string | null;
  price: number;
  count: number;
  imageUrl?: string | null;
  color?: string | null;
  size?: string | null;
  brand?: string | null;
  gender?: string | null;
  categoryId: number;
  storeId: number;
}

export interface ProductUpdateRequest {
  title?: string | null;
  description?: string | null;
  price?: number | null;
  count?: number | null;
  imageUrl?: string | null;
  color?: string | null;
  size?: string | null;
  brand?: string | null;
  gender?: string | null;
  categoryId?: number | null;
}

// Mirrors AiSearchRequestDto (Dokkana.BLL/DTOs/AI). Natural-language shopping
// search in Arabic / English / Franco-Arabic.
export interface AiSearchRequest {
  query: string;
  includeWeb?: boolean;
  pageNumber?: number;
  pageSize?: number;
  userLat?: number;
  userLng?: number;
}

// Mirrors AiSearchIntentDto — what the AI understood from the query.
export interface AiSearchIntent {
  rawQuery: string;
  normalizedArabic?: string | null;
  normalizedEnglish?: string | null;
  searchTerms: string[];
  productType?: string | null;
  category?: string | null;
  color?: string | null;
  size?: string | null;
  gender?: string | null;
  brand?: string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  languageDetected: string;
}

// Mirrors AiProductResultDto — a matched product from the local catalog.
export interface AiProductResult {
  id: number;
  title: string;
  price: number;
  color?: string | null;
  size?: string | null;
  storeName?: string | null;
  imageUrl?: string | null;
  originalPrice?: number | null;
  salePrice?: number | null;
  isOnSale?: boolean;
  // Where to buy + how far (km) from the shopper, when coordinates were sent.
  storeId: number;
  storeAddress?: string | null;
  storeLatitude?: number | null;
  storeLongitude?: number | null;
  distanceKm?: number | null;
  // Average shopper rating (0-5) and review count.
  rating: number;
  reviewCount: number;
  score: number;
  reason: string;
  isSponsored?: boolean;
  promotionId?: number | null;
}

// Mirrors AiWebResultDto — an optional web result (Serper).
export interface AiWebResult {
  title: string;
  url: string;
  snippet?: string | null;
  score: number;
  reason: string;
}

// Mirrors AiRankedResultDto — merged + ranked local/web result.
export interface AiRankedResult {
  source: string; // 'local' | 'web'
  title: string;
  description?: string | null;
  price?: number | null;
  originalPrice?: number | null;
  salePrice?: number | null;
  isOnSale?: boolean;
  imageUrl?: string | null;
  storeName?: string | null;
  url?: string | null;
  score: number;
  reason: string;
  isSponsored?: boolean;
  promotionId?: number | null;
  product?: AiProductResult | null;
  webResult?: AiWebResult | null;
}

// Mirrors AiSearchResponseDto.
export interface AiSearchResponse {
  query: string;
  intent: AiSearchIntent;
  answer: string;
  localResults: AiProductResult[];
  webResults: AiWebResult[];
  rankedResults: AiRankedResult[];
}
