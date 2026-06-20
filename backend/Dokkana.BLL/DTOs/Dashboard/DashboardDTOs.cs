namespace Dokkana.BLL.DTOs.Dashboard
{
    public class AdminStatsDTO
    {
        public int UsersCount { get; set; }
        public int StoresCount { get; set; }
        public int PendingStoresCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SellerStatsDTO
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public int LowStockProductsCount { get; set; }
    }

    public class UserSummaryDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
