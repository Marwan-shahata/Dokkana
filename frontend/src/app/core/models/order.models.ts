// Cart + order types mirroring the backend DTOs in Dokkana.BLL/DTOs/Cart and /Order.

export interface CartItemDTO {
  productId: number;
  title: string;
  imageUrl?: string | null;
  storeName: string;
  unitPrice: number;
  quantity: number;
  availableStock: number;
  lineTotal: number;
}

export interface CartDTO {
  items: CartItemDTO[];
  totalItems: number;
  totalAmount: number;
}

export interface OrderItemDTO {
  productId?: number | null;
  productTitle: string;
  imageUrl?: string | null;
  storeName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderDTO {
  id: number;
  createdAt: string;
  status: string;
  pickupCode: string;
  totalAmount: number;
  items: OrderItemDTO[];
}

export interface OrderSummaryDTO {
  id: number;
  createdAt: string;
  status: string;
  pickupCode: string;
  totalAmount: number;
  itemCount: number;
}

// Admin order list (includes customer info). Mirrors OrderAdminSummaryDTO.
export interface OrderAdminSummaryDTO {
  id: number;
  createdAt: string;
  status: string;
  pickupCode: string;
  totalAmount: number;
  itemCount: number;
  customerName: string;
  customerEmail: string;
}

// Store-owner order view (items filtered to this store). Mirrors StoreOrderDTO.
export interface StoreOrderDTO {
  id: number;
  createdAt: string;
  status: string;
  pickupCode: string;
  customerName: string;
  storeTotal: number;
  items: OrderItemDTO[];
}

// Admin user list. Mirrors UserAdminDTO.
export interface UserAdminDTO {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'StoreOwner' | 'Shopper';
  isDisabled: boolean;
  createdAt: string;
}
