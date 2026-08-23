using Catalog.Api.Contracts.Categories;
using Catalog.Api.Contracts.Products;
using Catalog.Api.Domain;

namespace Catalog.Api.Common;

/// <summary>
/// Mapeamento manual entidade -> DTO. É verboso de propósito:
/// AutoMapper esconde erros de mapeamento até virarem bug em runtime.
/// </summary>
public static class MappingExtensions
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Price,
        product.Currency,
        product.StockQuantity,
        product.IsActive,
        product.CategoryId,
        product.Category?.Name,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);

    public static CategoryResponse ToResponse(this Category category) => new(
        category.Id,
        category.Name,
        category.Slug,
        category.CreatedAtUtc);
}
