using System.Diagnostics;
using Catalog.Api.Common;
using Catalog.Api.Features.Categories;
using Catalog.Api.Features.Products;
using Catalog.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// A connection string vem de appsettings, variável de ambiente ou user-secrets.
// Nunca fica hard-coded no código. A ordem de precedência é: env var > user-secrets
// > appsettings.{Environment}.json > appsettings.json.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException("ConnectionStrings:Catalog não configurada.");

// ---------------------------------------------------------------------------
// Dependency Injection
// AddDbContext registra o CatalogDbContext com tempo de vida Scoped:
// uma instância por requisição HTTP. DbContext não é thread-safe — nunca Singleton.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MigrationsHistoryTable("__ef_migrations_history", "catalog");
    });

    if (builder.Environment.IsDevelopment())
    {
        // Mostra os parâmetros das queries no log. JAMAIS habilite em produção:
        // dados pessoais e senhas acabariam no arquivo de log.
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// ---------------------------------------------------------------------------
// Cross-cutting
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance =
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        // traceId permite correlacionar a resposta de erro com a linha do log.
        // No Módulo 12 isto evolui para CorrelationId propagado entre serviços.
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>("postgres");

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline (a ordem importa: cada middleware envolve os seguintes)
// ---------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar/v1 (UI de documentação)

    await app.Services.ApplyMigrationsAsync();
}

app.MapHealthChecks("/health");

app.MapProductEndpoints();
app.MapCategoryEndpoints();

app.Run();
