using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.TarjetasCredito;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class TarjetasCreditoEndpoints
{
    public static IEndpointRouteBuilder MapTarjetasCredito(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tarjetas-credito", Listar)
            .WithName("getTarjetasCredito")
            .WithTags("Tarjetas de crédito")
            .Produces<PaginaTarjetaCreditoResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapPost("/tarjetas-credito", Crear)
            .WithName("postTarjetasCredito")
            .WithTags("Tarjetas de crédito")
            .Produces<TarjetaCreditoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapGet("/tarjetas-credito/{tarjetaId:guid}", Obtener)
            .WithName("getTarjetasCreditoByTarjetaId")
            .WithTags("Tarjetas de crédito")
            .Produces<TarjetaCreditoResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapPatch("/tarjetas-credito/{tarjetaId:guid}", Actualizar)
            .WithName("patchTarjetasCreditoByTarjetaId")
            .WithTags("Tarjetas de crédito")
            .Produces<TarjetaCreditoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapDelete("/tarjetas-credito/{tarjetaId:guid}", Eliminar)
            .WithName("deleteTarjetasCreditoByTarjetaId")
            .WithTags("Tarjetas de crédito")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        return endpoints;
    }

    private static async Task<IResult> Listar(
        string? cursor,
        long? limite,
        ListarTarjetasCreditoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.Handle(
            new(context.UsuarioId(), cursor, limite ?? 20), cancellationToken));

    private static async Task<IResult> Crear(
        TarjetaCreditoRequest request,
        CrearTarjetaCreditoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new(
            context.UsuarioId(), context.TraceIdentifier, request.Alias,
            request.CuentaPagoId, request.DiaCierre,
            request.DiaVencimiento, request.LimiteCredito, request.Moneda, request.Color,
            request.Id),
            cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/tarjetas-credito/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid tarjetaId,
        ObtenerTarjetaCreditoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new(context.UsuarioId(), tarjetaId), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Actualizar(
        Guid tarjetaId,
        TarjetaCreditoPatchRequest request,
        ActualizarTarjetaCreditoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Handle(new(
            context.UsuarioId(), tarjetaId, version, request.Alias,
            request.CuentaPagoId, request.DiaCierre,
            request.DiaVencimiento, request.LimiteCredito, request.Moneda, request.Color),
            cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid tarjetaId,
        EliminarTarjetaCreditoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        await handler.Handle(
            new(context.UsuarioId(), tarjetaId, version), cancellationToken);
        return Results.NoContent();
    }
}
