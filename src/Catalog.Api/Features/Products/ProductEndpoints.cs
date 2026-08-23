using Catalog.Api.Common;
using Catalog.Api.Contracts.Common;
using Catalog.Api.Contracts.Products;
using Catalog.Api.Domain;
using Catalog.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products");

        group.MapGet("/", GetProducts)
            .WithSummary("Lista produtos com filtro e paginação.");

        group.MapGet("/{id:guid}", GetProductById)
            .WithSummary("Obtém um produto pelo id.");

        group.MapPost("/", CreateProduct)
            .WithSummary("Cria um produto.");

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithSummary("Atualiza os dados de um produto.");

        group.MapPatch("/{id:guid}/stock", UpdateStock)
            .WithSummary("Define a quantidade em estoque.");

        group.MapDelete("/{id:guid}", DeactivateProduct)
            .WithSummary("Desativa um produto (soft delete).");

        return app;
    }

    private static async Task<Ok<PagedResult<ProductResponse>>> GetProducts(
        CatalogDbContext db,
        CancellationToken cancellationToken,
        string? search = null,
        Guid? categoryId = null,
        bool? onlyActive = null,
        int? page = null,
        int? pageSize = null)
    {
        var (currentPage, currentSize) = PageRequest.Normalize(page, pageSize);

        // AsNoTracking: consulta de leitura não precisa do change tracker.
        // Menos alocação, menos memória, resposta mais rápida.
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike = LIKE case-insensitive do PostgreSQL.
            // Serve para o Módulo 1; no Módulo 6 isto vira full text search com índice GIN.
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern)
                                     || EF.Functions.ILike(p.Sku, pattern));
        }

        if (categoryId is not null)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (onlyActive == true)
            query = query.Where(p => p.IsActive);

        var totalItems = await query.LongCountAsync(cancellationToken);

        var items = await query
            // Ordenação estável é obrigatória em paginação: sem ORDER BY determinístico
            // o PostgreSQL pode repetir ou pular linhas entre páginas.
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenBy(p => p.Id)
            .Skip((currentPage - 1) * currentSize)
            .Take(currentSize)
            .Select(p => new ProductResponse(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Price,
                p.Currency,
                p.StockQuantity,
                p.IsActive,
                p.CategoryId,
                p.Category!.Name,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedResult<ProductResponse>(items, currentPage, currentSize, totalItems));
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetProductById(
        Guid id,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Price,
                p.Currency,
                p.StockQuantity,
                p.IsActive,
                p.CategoryId,
                p.Category!.Name,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product);
    }

    private static async Task<Results<Created<ProductResponse>, ValidationProblem, ProblemHttpResult>> CreateProduct(
        CreateProductRequest request,
        CatalogDbContext db,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (!RequestValidator.TryValidate(request, out var errors))
            return TypedResults.ValidationProblem(errors);

        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await db.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
        {
            return TypedResults.Problem(
                title: "SKU já cadastrado",
                detail: $"Já existe um produto com o SKU '{sku}'.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return TypedResults.Problem(
                title: "Categoria inexistente",
                detail: $"Não existe categoria com o id '{request.CategoryId}'.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var product = new Product(
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.StockQuantity,
            request.CategoryId);

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        // Log estruturado: os valores viram campos pesquisáveis, não texto interpolado.
        // Escreva "{ProductId}", nunca $"{product.Id}".
        logger.LogInformation("Produto {ProductId} criado com SKU {Sku}", product.Id, product.Sku);

        return TypedResults.Created($"/api/v1/products/{product.Id}", product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        if (!RequestValidator.TryValidate(request, out var errors))
            return TypedResults.ValidationProblem(errors);

        // Aqui a entidade é rastreada de propósito: vamos alterá-la.
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return TypedResults.Problem(
                title: "Categoria inexistente",
                detail: $"Não existe categoria com o id '{request.CategoryId}'.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        product.Rename(request.Name);
        product.ChangeDescription(request.Description);
        product.ChangePrice(request.Price, request.Currency);
        product.MoveToCategory(request.CategoryId);

        // Não existe db.Update(): o change tracker já sabe o que mudou.
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound, ValidationProblem>> UpdateStock(
        Guid id,
        UpdateStockRequest request,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        if (!RequestValidator.TryValidate(request, out var errors))
            return TypedResults.ValidationProblem(errors);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        product.SetStock(request.Quantity);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateProduct(
        Guid id,
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        // Soft delete: catálogo de e-commerce guarda histórico de pedidos.
        // Apagar a linha quebraria pedidos antigos que referenciam o produto.
        product.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
