using Microsoft.AspNetCore.Identity;

namespace Dokkana.DAL.Models
{
    // Extends Identity's user. A StoreOwner is linked to one Store.
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? Location { get; set; }
        public string? AvatarUrl { get; set; }

        // Set only when this user owns a store (role = StoreOwner).
        public int? StoreId { get; set; }
        public Store? Store { get; set; }

        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();
        public ICollection<StoreFollow> FollowedStores { get; set; } = new List<StoreFollow>();
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}
