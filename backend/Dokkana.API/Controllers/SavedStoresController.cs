using System.Security.Claims;
using Dokkana.BLL.DTOs.Engagement;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/saved-stores")]
    [Authorize(Policy = "ShopperOnly")]
    public class SavedStoresController : ControllerBase
    {
        private readonly IStoreFollowManager _follows;

        public SavedStoresController(IStoreFollowManager follows)
        {
            _follows = follows;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var stores = await _follows.GetAsync(UserId);
            return Ok(GeneralResult<IReadOnlyList<FollowedStoreDTO>>.Ok(stores));
        }

        [HttpPost("{storeId:int}")]
        public async Task<IActionResult> Follow(int storeId)
        {
            var result = await _follows.FollowAsync(UserId, storeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{storeId:int}")]
        public async Task<IActionResult> Unfollow(int storeId)
        {
            var result = await _follows.UnfollowAsync(UserId, storeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
