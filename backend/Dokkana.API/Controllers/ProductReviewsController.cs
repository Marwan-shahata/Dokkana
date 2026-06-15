using System.Security.Claims;
using Dokkana.BLL.DTOs.Engagement;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/reviews")]
    public class ProductReviewsController : ControllerBase
    {
        private readonly IReviewManager _reviews;

        public ProductReviewsController(IReviewManager reviews)
        {
            _reviews = reviews;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int productId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = await _reviews.GetByProductAsync(productId, currentUserId);
            return Ok(GeneralResult<IReadOnlyList<ProductReviewDTO>>.Ok(reviews));
        }

        [HttpPost]
        [Authorize(Policy = "ShopperOnly")]
        public async Task<IActionResult> Save(int productId, ProductReviewCreateDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _reviews.CreateOrUpdateAsync(userId, productId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{reviewId:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var result = await _reviews.DeleteAsync(userId, isAdmin, reviewId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
