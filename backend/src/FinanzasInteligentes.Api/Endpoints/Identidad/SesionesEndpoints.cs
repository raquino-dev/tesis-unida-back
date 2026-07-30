using FinanzasInteligentes.Api.Contratos.Identidad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.AdministrarSesiones;
using FinanzasInteligentes.Aplicacion.Identidad.CrearSesion;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class SesionesEndpoints
{
    public static IEndpointRouteBuilder MapSesiones(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/sesiones", Crear).AllowAnonymous().WithTags("Sesiones");
        endpoints.MapGet("/sesiones", Listar).WithTags("Sesiones");
        endpoints.MapPost("/sesiones/renovaciones", Renovar).AllowAnonymous().WithTags("Sesiones");
        endpoints.MapDelete("/sesiones/{sesionId:guid}", Revocar).WithTags("Sesiones");
        return endpoints;
    }

    private static async Task<IResult> Crear(
        CrearSesionCommand command,
        CrearSesionHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(command, cancellationToken);
        return Results.Created($"/api/v1/sesiones/{response.Id}", response);
    }

    private static async Task<IResult> Listar(
        ListarSesionesHandler handler,
        HttpContext context,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.Handle(
            new(context.UsuarioId(), context.SesionId()), cancellationToken));

    private static async Task<IResult> Renovar(
        RenovarSesionRequest request,
        RenovarSesionHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new(request.RefreshToken, request.IdentificadorDispositivo),
            cancellationToken);
        return Results.Created($"/api/v1/sesiones/{response.Id}", response);
    }

    private static async Task<IResult> Revocar(
        Guid sesionId,
        RevocarSesionHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new(context.UsuarioId(), sesionId), cancellationToken);
        return Results.NoContent();
    }
}