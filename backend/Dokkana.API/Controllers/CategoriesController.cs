using Dokkana.BLL.DTOs.Category;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryManager _categoryManager;
        private readonly IAuditLogManager _audit;

        public CategoriesController(ICategoryManager categoryManager, IAuditLogManager audit)
        {
            _categoryManager = categoryManager;
            _audit = audit;
        }

        // Public: browse categories.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryManager.GetAllAsync();
            return Ok(GeneralResult<IEnumerable<CategoryDTO>>.Ok(categories));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryManager.GetByIdAsync(id);
            return category is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<CategoryDTO>.Ok(category));
        }

        // Admin only: create a category.
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CategoryCreateDTO dto)
        {
            var created = await _categoryManager.CreateAsync(dto);
            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "CreateCategory",
                "Category",
                created.Id.ToString(),
                created.Name);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                GeneralResult<CategoryDTO>.Ok(created));
        }

        // Admin only: update a category.
        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, CategoryCreateDTO dto)
        {
            var ok = await _categoryManager.UpdateAsync(id, dto);
            if (!ok) return NotFound(GeneralResult.Fail(Errors.NotFound));

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "UpdateCategory",
                "Category",
                id.ToString(),
                dto.Name);

            return Ok(GeneralResult.Ok("Category updated."));
        }

        // Admin only: delete a category.
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _categoryManager.DeleteAsync(id);
            if (!ok) return NotFound(GeneralResult.Fail(Errors.NotFound));

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "DeleteCategory",
                "Category",
                id.ToString());

            return Ok(GeneralResult.Ok("Category deleted."));
        }
    }
}
