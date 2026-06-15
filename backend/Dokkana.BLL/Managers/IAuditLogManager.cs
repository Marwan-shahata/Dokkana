using Dokkana.BLL.DTOs.Engagement;

namespace Dokkana.BLL.Managers
{
    public interface IAuditLogManager
    {
        Task LogAsync(string? actorUserId, string? actorEmail, string action, string entityType, string? entityId, string? details = null);
        Task<IReadOnlyList<AuditLogDTO>> GetRecentAsync(int limit = 100);
    }
}
