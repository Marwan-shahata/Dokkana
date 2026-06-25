namespace Dokkana.DAL.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? ActorUserId { get; set; }
        public string? ActorEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Actor { get; set; }
    }
}
