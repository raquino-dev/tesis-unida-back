using FinanzasInteligentes.Api.Contratos.Identidad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.Administracion;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class AdministracionUsuariosEndpoints
{
    public static IEndpointRouteBuilder MapAdministracionUsuarios(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/administracion/usuarios")
            .RequireAuthorization("administrador")
            .WithTags("Administración de usuarios");

        grupo.MapGet("/", Listar).WithName("getAdministracionUsuarios");
        grupo.MapPut("/{usuarioId:guid}/estado", CambiarEstado)
            .WithName("putAdministracionUsuarioEstado");
        grupo.MapPut("/{usuarioId:guid}/rol", CambiarRol)
            .WithName("putAdministracionUsuarioRol");
        return endpoints;
    }

    private static async Task<IResult> Listar(
        string? estado, string? busqueda, int? limite,
        ListarUsuariosAdministracionHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(await handler.Handle(
            new(estado, busqueda, limite ?? 50), cancellationToken));

    private static async Task<IResult> CambiarEstado(
        Guid usuarioId, CambiarEstadoUsuarioRequest request,
        CambiarEstadoUsuarioHandler handler, HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new(
            context.UsuarioId(), usuarioId, request.Estado,
            request.Motivo, context.TraceIdentifier), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> CambiarRol(
        Guid usuarioId, CambiarRolUsuarioRequest request,
        CambiarRolUsuarioHandler handler, HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new(
            context.UsuarioId(), usuarioId, request.Rol,
            request.Motivo, context.TraceIdentifier), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }
}
