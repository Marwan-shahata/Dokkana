using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/leads")]
    public class LeadsController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public LeadsController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateLeadDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lead = await _monetization.CreateLeadAsync(dto, userId);
            return lead is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<MerchantLeadDTO>.Ok(lead, "Lead tracked."));
        }

        [HttpGet("mine")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Mine()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var leads = await _monetization.GetMerchantLeadsAsync(ownerId);
            return Ok(GeneralResult<IReadOnlyList<MerchantLeadDTO>>.Ok(leads));
        }
    }
}
