using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Contracts.Products;

public sealed record CreateProductRequest
{
    [Required(ErrorMessage = "O SKU é obrigatório.")]
    [StringLength(64, MinimumLength = 3)]
    public string Sku { get; init; } = string.Empty;

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(0.01, 9_999_999, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Price { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "A moeda deve ter 3 letras (ISO 4217).")]
    public string Currency { get; init; } = "BRL";

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int StockQuantity { get; init; }

    [Required]
    public Guid CategoryId { get; init; }
}

public sealed record UpdateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(0.01, 9_999_999)]
    public decimal Price { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "BRL";

    [Required]
    public Guid CategoryId { get; init; }
}

public sealed record UpdateStockRequest
{
    [Range(0, int.MaxValue)]
    public int Quantity { get; init; }
}

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    Guid CategoryId,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
