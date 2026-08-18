using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ActualizarCategoria;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.CrearCategoria;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.EliminarCategoria;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ListarCategorias;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ObtenerCategoria;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class CategoriasEndpoints
{
    public static IEndpointRouteBuilder MapCategorias(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/categorias", Listar).WithTags("Categorías");
        endpoints.MapPost("/categorias", Crear).WithTags("Categorías");
        endpoints.MapGet("/categorias/{categoriaId:guid}", Obtener).WithTags("Categorías");
        endpoints.MapPatch("/categorias/{categoriaId:guid}", Actualizar).WithTags("Categorías");
        endpoints.MapDelete("/categorias/{categoriaId:guid}", Eliminar).WithTags("Categorías");
        return endpoints;
    }

    private static async Task<IResult> Listar(
        ListarCategoriasHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.Handle(new ListarCategoriasQuery(context.UsuarioId()), cancellationToken));
    }

    private static async Task<IResult> Crear(
        CrearCategoriaRequest request,
        CrearCategoriaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CrearCategoriaCommand(
                context.UsuarioId(), request.Nombre, request.Tipo,
                request.Icono, request.Color, request.Id),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Created($"/api/v1/categorias/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid categoriaId,
        ObtenerCategoriaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new ObtenerCategoriaQuery(context.UsuarioId(), categoriaId),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Actualizar(
        Guid categoriaId,
        ActualizarCategoriaRequest request,
        ActualizarCategoriaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);

        var response = await handler.Handle(
            new ActualizarCategoriaCommand(
                context.UsuarioId(), categoriaId, version,
                request.Nombre, request.Tipo, request.Icono, request.Color),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid categoriaId,
        EliminarCategoriaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);

        await handler.Handle(
            new EliminarCategoriaCommand(context.UsuarioId(), categoriaId, version),
            cancellationToken);

        return Results.NoContent();
    }
}
