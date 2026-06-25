using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/trends")]
    public class TrendsController : ControllerBase
    {
        private readonly IMonetizationManager _monetization;

        public TrendsController(IMonetizationManager monetization)
        {
            _monetization = monetization;
        }

        [HttpGet]
        public async Task<IActionResult> Trending([FromQuery] int limit = 8)
        {
            var trends = await _monetization.GetTrendingAsync(limit);
            return Ok(GeneralResult<IReadOnlyList<TrendingQueryDTO>>.Ok(trends));
        }
    }
}
