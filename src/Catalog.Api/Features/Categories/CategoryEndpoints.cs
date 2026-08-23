using Catalog.Api.Common;
using Catalog.Api.Contracts.Categories;
using Catalog.Api.Domain;
using Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories")
            .WithTags("Categories");

        group.MapGet("/", GetCategories)
            .WithSummary("Lista todas as categorias.");

        group.MapGet("/{id:guid}", GetCategoryById)
            .WithSummary("Obtém uma categoria pelo id.");

        group.MapPost("/", CreateCategory)
            .WithSummary("Cria uma categoria.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<CategoryResponse>>> GetCategories(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<CategoryResponse>>(categories);
    }

    private static async Task<Results<Ok<CategoryResponse>, NotFound>> GetCategoryById(
        Guid id,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return category is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(category);
    }

    private static async Task<Results<Created<CategoryResponse>, ValidationProblem, ProblemHttpResult>> CreateCategory(
        CreateCategoryRequest request,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        if (!RequestValidator.TryValidate(request, out var errors))
            return TypedResults.ValidationProblem(errors);

        var slugTaken = await db.Categories
            .AnyAsync(c => c.Slug == request.Slug.ToLower(), cancellationToken);

        if (slugTaken)
        {
            return TypedResults.Problem(
                title: "Slug já utilizado",
                detail: $"Já existe uma categoria com o slug '{request.Slug}'.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var category = new Category(request.Name, request.Slug);

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/v1/categories/{category.Id}", category.ToResponse());
    }
}
