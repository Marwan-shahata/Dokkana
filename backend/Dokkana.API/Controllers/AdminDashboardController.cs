using Dokkana.BLL.DTOs.Dashboard;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardManager _dashboardManager;

        public AdminDashboardController(IDashboardManager dashboardManager)
        {
            _dashboardManager = dashboardManager;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var stats = await _dashboardManager.GetAdminStatsAsync();
            return Ok(GeneralResult<AdminStatsDTO>.Ok(stats));
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _dashboardManager.GetUsersAsync();
            return Ok(GeneralResult<IEnumerable<UserSummaryDTO>>.Ok(users));
        }
    }
}
