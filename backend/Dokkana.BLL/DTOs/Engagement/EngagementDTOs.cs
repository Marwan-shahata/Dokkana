namespace Dokkana.BLL.DTOs.Engagement
{
    public class WishlistItemDTO
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class FollowedStoreDTO
    {
        public int StoreId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductReviewDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
    }

    public class ProductReviewCreateDTO
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class AuditLogDTO
    {
        public int Id { get; set; }
        public string? ActorEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
