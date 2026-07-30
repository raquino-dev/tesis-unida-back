using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.CrearUsuario;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuarios(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/usuarios", Crear).AllowAnonymous().WithTags("Usuarios");
        return endpoints;
    }

    private static async Task<IResult> Crear(
        CrearUsuarioCommand command,
        CrearUsuarioHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(command, cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/usuarios/{response.Id}", response);
    }
}