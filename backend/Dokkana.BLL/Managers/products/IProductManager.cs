using Dokkana.BLL.DTOs.Product;

namespace Dokkana.BLL.Managers.Products;

public interface IProductManager
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        ProductFilterDto filter,
        CancellationToken cancellationToken = default);
}