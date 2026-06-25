using Dokkana.BLL.DTOs.Engagement;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Policy = "AdminOnly")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogManager _auditLogs;

        public AuditLogsController(IAuditLogManager auditLogs)
        {
            _auditLogs = auditLogs;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int limit = 100)
        {
            var logs = await _auditLogs.GetRecentAsync(limit);
            return Ok(GeneralResult<IReadOnlyList<AuditLogDTO>>.Ok(logs));
        }
    }
}
