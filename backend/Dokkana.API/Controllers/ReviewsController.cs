using System.Security.Claims;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ShopperOnly")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewManager _reviews;

        public ReviewsController(IReviewManager reviews)
        {
            _reviews = reviews;
        }

        [HttpGet("mine/count")]
        public async Task<IActionResult> MyCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _reviews.CountByUserAsync(userId);
            return Ok(GeneralResult<object>.Ok(new { count }));
        }
    }
}
