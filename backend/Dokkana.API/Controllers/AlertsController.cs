using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    [Authorize(Policy = "ShopperOnly")]
    public class AlertsController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public AlertsController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var alerts = await _monetization.GetAlertsAsync(UserId);
            return Ok(GeneralResult<IReadOnlyList<AlertDTO>>.Ok(alerts));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAlertDTO dto)
        {
            var alert = await _monetization.CreateAlertAsync(dto, UserId);
            return alert is null
                ? BadRequest(GeneralResult.Fail("Choose a product or search query."))
                : Ok(GeneralResult<AlertDTO>.Ok(alert, "Alert saved."));
        }
    }
}
