using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public PromotionsController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("mine")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Mine()
        {
            var campaigns = await _monetization.GetMerchantCampaignsAsync(UserId);
            return Ok(GeneralResult<IReadOnlyList<PromotionDTO>>.Ok(campaigns));
        }

        [HttpPost("featured")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> CreateFeatured(CreateFeaturedPromotionDTO dto)
        {
            var (result, campaign, message) = await _monetization.CreateFeaturedAsync(dto, UserId);
            return ToCampaignResult(result, campaign, message);
        }

        [HttpPost("notification")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> CreateNotification(CreateNotificationPromotionDTO dto)
        {
            var (result, campaign, message) = await _monetization.CreateNotificationAsync(dto, UserId);
            return ToCampaignResult(result, campaign, message);
        }

        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminList([FromQuery] string? status)
        {
            var campaigns = await _monetization.GetAdminCampaignsAsync(status);
            return Ok(GeneralResult<IReadOnlyList<PromotionDTO>>.Ok(campaigns));
        }

        [HttpPut("{id:int}/approve")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Approve(int id, CampaignDecisionDTO dto)
        {
            var campaign = await _monetization.ApproveCampaignAsync(id, dto.Reason);
            return campaign is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<PromotionDTO>.Ok(campaign, "Campaign approved."));
        }

        [HttpPut("{id:int}/reject")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Reject(int id, CampaignDecisionDTO dto)
        {
            var ok = await _monetization.RejectCampaignAsync(id, dto.Reason);
            return ok
                ? Ok(GeneralResult.Ok("Campaign rejected."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }

        [HttpGet("sponsored-products")]
        public async Task<IActionResult> SponsoredProducts([FromQuery] int limit = 3)
        {
            var campaigns = await _monetization.GetSponsoredProductsAsync(limit);
            return Ok(GeneralResult<IReadOnlyList<PromotionDTO>>.Ok(campaigns));
        }

        [HttpGet("sponsored-stores")]
        public async Task<IActionResult> SponsoredStores([FromQuery] int limit = 3)
        {
            var campaigns = await _monetization.GetSponsoredStoresAsync(limit);
            return Ok(GeneralResult<IReadOnlyList<PromotionDTO>>.Ok(campaigns));
        }

        [HttpPost("{id:int}/click")]
        public async Task<IActionResult> TrackClick(int id)
        {
            var ok = await _monetization.TrackPromotionClickAsync(id);
            return ok
                ? Ok(GeneralResult.Ok("Promotion click tracked."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }

        private IActionResult ToCampaignResult(OwnershipResult result, PromotionDTO? campaign, string? message)
        {
            return result switch
            {
                OwnershipResult.Success => Ok(GeneralResult<PromotionDTO>.Ok(campaign!, message ?? "Campaign saved.")),
                OwnershipResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, GeneralResult.Fail(message ?? "Not allowed.")),
                _ => NotFound(GeneralResult.Fail(message ?? Errors.NotFound))
            };
        }
    }
}
