using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [Authorize(Policy = "StoreOwnerOnly")]
    public class BillingController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public BillingController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var summary = await _monetization.GetBillingAsync(UserId);
            return summary is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<BillingSummaryDTO>.Ok(summary));
        }

        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp(TopUpWalletDTO dto)
        {
            var summary = await _monetization.TopUpAsync(UserId, dto);
            return summary is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<BillingSummaryDTO>.Ok(summary, "Wallet topped up."));
        }
    }
}
