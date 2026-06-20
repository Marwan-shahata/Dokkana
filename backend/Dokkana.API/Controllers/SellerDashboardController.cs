using System.Security.Claims;
using Dokkana.BLL.DTOs.Dashboard;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/seller")]
    [Authorize(Policy = "StoreOwnerOnly")]
    public class SellerDashboardController : ControllerBase
    {
        private readonly IDashboardManager _dashboardManager;

        public SellerDashboardController(IDashboardManager dashboardManager)
        {
            _dashboardManager = dashboardManager;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var stats = await _dashboardManager.GetSellerStatsAsync(ownerId);
            return stats is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<SellerStatsDTO>.Ok(stats));
        }
    }
}
