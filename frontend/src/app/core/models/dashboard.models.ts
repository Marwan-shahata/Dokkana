// Mirrors Dokkana.BLL.DTOs.Dashboard.AdminStatsDTO
export interface AdminStatsDTO {
  usersCount: number;
  storesCount: number;
  pendingStoresCount: number;
  productsCount: number;
  ordersCount: number;
  revenue: number;
}

// Mirrors Dokkana.BLL.DTOs.Dashboard.SellerStatsDTO
export interface SellerStatsDTO {
  storeId: number;
  storeName: string;
  isApproved: boolean;
  productsCount: number;
  ordersCount: number;
  unitsSold: number;
  revenue: number;
  lowStockProductsCount: number;
}
