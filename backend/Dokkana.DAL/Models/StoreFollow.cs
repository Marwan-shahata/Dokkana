namespace Dokkana.DAL.Models
{
    public class StoreFollow
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public Store? Store { get; set; }
    }
}
