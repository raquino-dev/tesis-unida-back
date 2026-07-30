using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Seguridad;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Api.Middleware;

public sealed class AuditoriaHttpMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditoriaHttpMiddleware> logger)
{
    private static readonly HashSet<string> MetodosAuditables =
        new(["POST", "PUT", "PATCH", "DELETE"], StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1") ||
            !MetodosAuditables.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        Exception? error = null;
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            try
            {
                await Registrar(context, error);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo registrar la auditoría HTTP {CorrelationId}",
                    context.TraceIdentifier);
            }
        }
    }

    private async Task Registrar(HttpContext context, Exception? error)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISeguridadRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnidadDeTrabajo>();
        var usuarioId = Guid.TryParse(
            context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var parsed) ? parsed : (Guid?)null;
        var recursoId = context.Request.RouteValues.Values
            .Select(x => Guid.TryParse(x?.ToString(), out var id) ? id : Guid.Empty)
            .FirstOrDefault(x => x != Guid.Empty);
        var ruta = context.Request.Path.Value ?? "/api/v1";
        var recurso = ruta.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Skip(2).FirstOrDefault() ?? "api";
        var status = error is null ? context.Response.StatusCode : 500;
        var ipHash = HashOrigen(context.Connection.RemoteIpAddress?.ToString());

        repository.Agregar(EventoAuditoria.Crear(
            usuarioId, null, context.Request.Method.ToLowerInvariant(),
            recurso, recursoId,
            null,
            new
            {
                ruta,
                estadoHttp = status,
                exitoso = error is null && status < 400,
                origenHash = ipHash
            },
            context.TraceIdentifier));
        await unitOfWork.GuardarCambios(context.RequestAborted.IsCancellationRequested
            ? CancellationToken.None
            : context.RequestAborted);
    }

    private static string? HashOrigen(string? origen)
    {
        if (string.IsNullOrWhiteSpace(origen)) return null;
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(origen)))[..16];
    }
}
