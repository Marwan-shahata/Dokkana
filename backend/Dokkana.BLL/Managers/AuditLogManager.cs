using Dokkana.BLL.DTOs.Engagement;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class AuditLogManager : IAuditLogManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(
            string? actorUserId,
            string? actorEmail,
            string action,
            string entityType,
            string? entityId,
            string? details = null)
        {
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AuditLogDTO>> GetRecentAsync(int limit = 100)
        {
            limit = Math.Clamp(limit, 1, 500);
            return await _unitOfWork.AuditLogs.Query()
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new AuditLogDTO
                {
                    Id = a.Id,
                    ActorEmail = a.ActorEmail,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }
    }
}
