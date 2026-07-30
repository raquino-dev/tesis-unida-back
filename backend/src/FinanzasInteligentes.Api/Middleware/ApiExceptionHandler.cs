using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Api.Middleware;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            DomainException domain => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio no satisfecha", domain.Code),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Conflicto", conflict.Code),
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Recurso no encontrado", notFound.Code),
            AuthenticationException authentication => (StatusCodes.Status401Unauthorized, "No autorizado", authentication.Code),
            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, "Prohibido", forbidden.Code),
            PreconditionFailedException precondition => (StatusCodes.Status412PreconditionFailed, "Versión desactualizada", precondition.Code),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado", "no_autorizado"),
            DbUpdateConcurrencyException => (StatusCodes.Status412PreconditionFailed, "Versión desactualizada", "etag_desactualizado"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflicto de persistencia", "conflicto"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "error_interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado. CorrelationId {CorrelationId}", context.TraceIdentifier);

        context.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? "Ocurrió un error inesperado." : exception.Message,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["codigo"] = code,
                    ["correlationId"] = context.TraceIdentifier
                }
            }
        });
    }
}