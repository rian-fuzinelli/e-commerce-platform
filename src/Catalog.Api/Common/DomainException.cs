namespace Catalog.Api.Common;

/// <summary>
/// Erro de regra de negócio. Diferente de um erro de infraestrutura:
/// significa que o estado pedido é inválido para o domínio, não que algo quebrou.
/// É traduzido para HTTP 400 pelo GlobalExceptionHandler.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
