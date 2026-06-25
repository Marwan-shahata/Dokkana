using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    // Pro-only merchant insights (MM3): demand forecast + pricing hints.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "StoreOwnerOnly")]
    public class InsightsController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public InsightsController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        // GET /api/insights — forecast + pricing hints for the caller's store.
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var insights = await _monetization.GetInsightsAsync(ownerId);
            return insights is null
                ? NotFound(GeneralResult.Fail("Create your store profile first."))
                : Ok(GeneralResult<MerchantInsightsDTO>.Ok(insights));
        }
    }
}
