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
}

// Mirrors StoreDetailsDTO.
export interface StoreDetails extends Store {
  ownerName?: string | null;
  productCount: number;
}

export interface StoreCreateRequest {
  name: string;
  description?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
}

export interface StoreUpdateRequest {
  name?: string | null;
  description?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
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

// Mirrors AiSearchRequestDTO / AiSearchResponseDTO.
export interface AiSearchRequest {
  query: string;
  storeId?: number;
}

export interface AiSearchResponse {
  query: string;
  answer: string;
  products: ProductListItem[];
  totalMatches: number;
}
