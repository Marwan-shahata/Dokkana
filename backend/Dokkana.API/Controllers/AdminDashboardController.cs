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

        // Platform KPIs for the admin home.
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var stats = await _dashboardManager.GetAdminStatsAsync();
            return Ok(GeneralResult<AdminStatsDTO>.Ok(stats));
        }
    }
}
