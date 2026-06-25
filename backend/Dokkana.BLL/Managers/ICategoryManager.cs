using Dokkana.BLL.DTOs.Category;

namespace Dokkana.BLL.Managers
{
    public interface ICategoryManager
    {
        Task<IEnumerable<CategoryDTO>> GetAllAsync();
        Task<CategoryDTO?> GetByIdAsync(int id);
        Task<CategoryDTO> CreateAsync(CategoryCreateDTO dto);
        Task<bool> UpdateAsync(int id, CategoryCreateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
