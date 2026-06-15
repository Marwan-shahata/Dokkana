namespace Dokkana.DAL.Models
{
    // A fashion product belonging to a store. Fashion attributes (Color, Size, Brand,
    // Gender) are what the AI Shopping Assistant filters on.
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string? ImageUrl { get; set; }

        // Fashion attributes for AI search.
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? Brand { get; set; }
        public string? Gender { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public ICollection<FavoriteProduct> Favorites { get; set; } = new List<FavoriteProduct>();
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }
}
