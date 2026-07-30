using FinanzasInteligentes.Aplicacion.Abstracciones;
using System.Security.Claims;

namespace FinanzasInteligentes.Api.Middleware;

public sealed class UsuarioActivoMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IIdentidadRepository identidad)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(
                context.User.FindFirstValue("sub") ??
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var usuarioId) &&
            !await identidad.UsuarioEstaActivo(usuarioId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                title = "Sesión no válida",
                status = StatusCodes.Status401Unauthorized,
                codigo = "usuario_inactivo",
                correlationId = context.TraceIdentifier
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
