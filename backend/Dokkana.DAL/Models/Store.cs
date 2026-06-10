namespace Dokkana.DAL.Models
{
    // A local fashion store. Owned by a StoreOwner, must be approved by an Admin
    // before it becomes visible to shoppers.
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsApproved { get; set; }

        // Owner (Identity user id is a string).
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
