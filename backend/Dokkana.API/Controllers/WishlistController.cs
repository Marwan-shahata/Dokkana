using System.Security.Claims;
using Dokkana.BLL.DTOs.Engagement;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ShopperOnly")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistManager _wishlist;

        public WishlistController(IWishlistManager wishlist)
        {
            _wishlist = wishlist;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var items = await _wishlist.GetAsync(UserId);
            return Ok(GeneralResult<IReadOnlyList<WishlistItemDTO>>.Ok(items));
        }

        [HttpPost("{productId:int}")]
        public async Task<IActionResult> Add(int productId)
        {
            var result = await _wishlist.AddAsync(UserId, productId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> Remove(int productId)
        {
            var result = await _wishlist.RemoveAsync(UserId, productId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
