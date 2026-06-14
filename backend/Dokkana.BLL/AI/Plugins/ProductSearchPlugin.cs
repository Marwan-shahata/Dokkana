using System.ComponentModel;
using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.Product;
using Dokkana.BLL.Managers.Products;
using Microsoft.SemanticKernel;

namespace Dokkana.BLL.AI.Plugins;

public sealed class ProductSearchPlugin
{
    private readonly IProductManager _productManager;

    public ProductSearchPlugin(IProductManager productManager)
    {
        _productManager = productManager;
    }

    [KernelFunction("search_products")]
    [Description("Search Dokkana local fashion products using filters extracted from natural language.")]
    public async Task<IReadOnlyList<ProductSearchResultDto>> SearchProductsAsync(
        [Description("Free text search from the user query. Example: cotton shirt, summer dress.")]
        string? searchText = null,

        [Description("Product category. Example: t-shirt, shirt, jeans, dress, shoes.")]
        string? category = null,

        [Description("Product color. Example: black, white, red, blue.")]
        string? color = null,

        [Description("Product size. Example: S, M, L, XL, 42.")]
        string? size = null,

        [Description("Gender target. Example: men, women, kids, unisex.")]
        string? gender = null,

        [Description("Minimum price in EGP.")]
        decimal? minPrice = null,

        [Description("Maximum price in EGP.")]
        decimal? maxPrice = null,

        CancellationToken cancellationToken = default)
    {
        var filter = new ProductFilterDto
        {
            SearchText = searchText,
            Category = category,
            Color = color,
            Size = size,
            Gender = gender,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            PageNumber = 1,
            PageSize = 10
        };

        var products = await _productManager.GetProductsAsync(filter, cancellationToken);

        return products.Select(p => new ProductSearchResultDto
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            Color = p.Color,
            Size = p.Size,
            StoreName = p.StoreName,
            ImageUrl = p.ImageUrl
        }).ToList();
    }
}