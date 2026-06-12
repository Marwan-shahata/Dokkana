using Dokkana.BLL.DTOs.Product;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IProductManager
    {
        Task<PagedResult<ProductListDTO>> GetAllAsync(string? search, int? categoryId,
            decimal? minPrice, decimal? maxPrice, string? color, string? gender, int page, int pageSize);
        Task<ProductDetailsDTO?> GetByIdAsync(int id);
        Task<PagedResult<ProductListDTO>> GetByStoreAsync(int storeId, int page, int pageSize);
        Task<ProductDetailsDTO> CreateAsync(ProductCreateDTO dto);
        Task<bool> UpdateAsync(int id, ProductUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
