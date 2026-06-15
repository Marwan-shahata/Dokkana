using System.Security.Claims;
using Dokkana.BLL.DTOs.Product;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductManager _productManager;

        public ProductsController(IProductManager productManager)
        {
            _productManager = productManager;
        }

        // Public: browse products with filtering + pagination.
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? color,
            [FromQuery] string? gender,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _productManager.GetAllAsync(search, categoryId, minPrice, maxPrice, color, gender, page, pageSize);
            return Ok(GeneralResult<PagedResult<ProductListDTO>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productManager.GetByIdAsync(id);
            return product is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<ProductDetailsDTO>.Ok(product));
        }

        // Public: get products for a specific store.
        [HttpGet("store/{storeId:int}")]
        public async Task<IActionResult> GetByStore(int storeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _productManager.GetByStoreAsync(storeId, page, pageSize);
            return Ok(GeneralResult<PagedResult<ProductListDTO>>.Ok(result));
        }

        // StoreOwner: create a product for their own store (StoreId from the
        // body is ignored - the product is always bound to the caller's store).
        [HttpPost]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Create(ProductCreateDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (result, created) = await _productManager.CreateAsync(dto, userId);
            return result switch
            {
                OwnershipResult.Success => CreatedAtAction(nameof(GetById), new { id = created!.Id },
                    GeneralResult<ProductDetailsDTO>.Ok(created)),
                OwnershipResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    GeneralResult.Fail("You do not have a store to add products to.")),
                _ => NotFound(GeneralResult.Fail(Errors.NotFound)),
            };
        }

        // StoreOwner: update their own product. Admin may update any product.
        [HttpPut("{id:int}")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Update(int id, ProductUpdateDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _productManager.UpdateAsync(id, dto, userId, User.IsInRole("Admin"));
            return ToActionResult(result, "Product updated.");
        }

        // StoreOwner (owning) or Admin: delete a product.
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _productManager.DeleteAsync(id, userId, User.IsInRole("Admin"));
            return ToActionResult(result, "Product deleted.");
        }

        // Maps an ownership outcome to the right HTTP response.
        private IActionResult ToActionResult(OwnershipResult result, string successMessage) => result switch
        {
            OwnershipResult.Success => Ok(GeneralResult.Ok(successMessage)),
            OwnershipResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                GeneralResult.Fail("You do not own this resource.")),
            _ => NotFound(GeneralResult.Fail(Errors.NotFound)),
        };
    }
}
