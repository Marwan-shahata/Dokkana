using Dokkana.BLL.DTOs.Product;
using Dokkana.Common;
// OwnershipResult lives in Dokkana.Common.

namespace Dokkana.BLL.Managers
{
    public interface IProductManager
    {
        Task<PagedResult<ProductListDTO>> GetAllAsync(string? search, int? categoryId,
            decimal? minPrice, decimal? maxPrice, string? color, string? gender, int page, int pageSize);
        Task<ProductDetailsDTO?> GetByIdAsync(int id);
        Task<PagedResult<ProductListDTO>> GetByStoreAsync(int storeId, int page, int pageSize);

        // Creates a product under the authenticated owner's store. The request
        // body's StoreId is ignored; Forbidden is returned if the user has no store.
        Task<(OwnershipResult Result, ProductDetailsDTO? Product)> CreateAsync(ProductCreateDTO dto, string ownerId);

        // Ownership-guarded. Admins bypass the owner check.
        Task<OwnershipResult> UpdateAsync(int id, ProductUpdateDTO dto, string userId, bool isAdmin);
        Task<OwnershipResult> DeleteAsync(int id, string userId, bool isAdmin);
    }
}
