using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Common;

/// <summary>
/// Um único lugar traduz exceção para resposta HTTP. Sem isto, cada endpoint
/// vira um try/catch e o formato de erro diverge entre rotas.
/// Todas as respostas seguem RFC 9457 (ProblemDetails).
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Falha não tratada em {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Regra de negócio violada em {Method} {Path}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // Detalhe da exceção só vaza em 4xx (mensagem escrita por nós).
                // Em 500 a mensagem interna fica no log, não na resposta.
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "Ocorreu um erro inesperado. Consulte os logs com o traceId informado."
                    : exception.Message
            }
        });
    }
}
