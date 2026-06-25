using System.Security.Claims;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public PreferencesController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("notifications")]
        public async Task<IActionResult> Notifications()
        {
            var prefs = await _monetization.GetNotificationPreferencesAsync(UserId);
            return prefs is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<NotificationPreferencesDTO>.Ok(prefs));
        }

        [HttpPut("notifications")]
        public async Task<IActionResult> SetNotifications(NotificationPreferencesDTO dto)
        {
            var prefs = await _monetization.SetNotificationPreferencesAsync(UserId, dto);
            return prefs is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<NotificationPreferencesDTO>.Ok(prefs, "Preferences updated."));
        }
    }
}
