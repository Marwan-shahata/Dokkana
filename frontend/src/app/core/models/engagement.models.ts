export interface WishlistItemDTO {
  productId: number;
  title: string;
  price: number;
  imageUrl?: string | null;
  storeName: string;
  createdAt: string;
}

export interface FollowedStoreDTO {
  storeId: number;
  name: string;
  description?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  productCount: number;
  createdAt: string;
}

export interface ProductReviewDTO {
  id: number;
  productId: number;
  userName: string;
  rating: number;
  comment?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  canEdit: boolean;
}

export interface ProductReviewCreateRequest {
  rating: number;
  comment?: string | null;
}

export interface ReviewCountDTO {
  count: number;
}

export interface AuditLogDTO {
  id: number;
  actorEmail?: string | null;
  action: string;
  entityType: string;
  entityId?: string | null;
  details?: string | null;
  createdAt: string;
}
