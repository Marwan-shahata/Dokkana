using System.Security.Claims;
using Dokkana.BLL.DTOs.Cart;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ShopperOnly")] // Only shoppers can reserve items.
    public class CartController : ControllerBase
    {
        private readonly ICartManager _cartManager;

        public CartController(ICartManager cartManager)
        {
            _cartManager = cartManager;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cart = await _cartManager.GetCartAsync(UserId);
            return Ok(GeneralResult<CartDTO>.Ok(cart));
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemDTO dto)
        {
            var result = await _cartManager.AddItemAsync(UserId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("items/{productId:int}")]
        public async Task<IActionResult> UpdateItem(int productId, UpdateCartItemDTO dto)
        {
            var result = await _cartManager.UpdateItemAsync(UserId, productId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("items/{productId:int}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var result = await _cartManager.RemoveItemAsync(UserId, productId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            var cart = await _cartManager.ClearAsync(UserId);
            return Ok(GeneralResult<CartDTO>.Ok(cart, "Cart cleared."));
        }
    }
}
