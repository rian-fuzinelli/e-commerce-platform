using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Contracts.Categories;

public sealed record CreateCategoryRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "O slug é obrigatório.")]
    [StringLength(140, MinimumLength = 2)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "O slug deve conter apenas letras minúsculas, números e hífens.")]
    public string Slug { get; init; } = string.Empty;
}

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAtUtc);
