using Dokkana.BLL.DTOs.Category;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryManager _categoryManager;

        public CategoriesController(ICategoryManager categoryManager)
        {
            _categoryManager = categoryManager;
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
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                GeneralResult<CategoryDTO>.Ok(created));
        }

        // Admin only: update a category.
        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, CategoryCreateDTO dto)
        {
            var ok = await _categoryManager.UpdateAsync(id, dto);
            return ok
                ? Ok(GeneralResult.Ok("Category updated."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }

        // Admin only: delete a category.
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _categoryManager.DeleteAsync(id);
            return ok
                ? Ok(GeneralResult.Ok("Category deleted."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }
    }
}
