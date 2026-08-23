using Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Infrastructure.Persistence;

public static class DatabaseStartupExtensions
{
    /// <summary>
    /// Aplica migrations e popula dados de exemplo. APENAS em desenvolvimento.
    /// Em produção migration é passo de pipeline (Módulo 14), não do processo da API:
    /// com N réplicas subindo juntas você teria N migrations concorrentes.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogDbContext>>();

        logger.LogInformation("Aplicando migrations pendentes...");
        await db.Database.MigrateAsync();

        if (await db.Categories.AnyAsync())
        {
            logger.LogInformation("Banco já possui dados. Seed ignorado.");
            return;
        }

        var eletronicos = new Category("Eletrônicos", "eletronicos");
        var livros = new Category("Livros", "livros");

        db.Categories.AddRange(eletronicos, livros);
        db.Products.AddRange(
            new Product("SKU-KB-001", "Teclado Mecânico 75%", "Switches lineares, hot-swap.", 549.90m, "BRL", 25, eletronicos.Id),
            new Product("SKU-MS-002", "Mouse sem fio 8K", "Sensor óptico de 26.000 DPI.", 399.00m, "BRL", 40, eletronicos.Id),
            new Product("SKU-BK-003", "Domain-Driven Design", "Eric Evans, edição em inglês.", 289.90m, "BRL", 10, livros.Id));

        await db.SaveChangesAsync();
        logger.LogInformation("Seed concluído.");
    }
}
