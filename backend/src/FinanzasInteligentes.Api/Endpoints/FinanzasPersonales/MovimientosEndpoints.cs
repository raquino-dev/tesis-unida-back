using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ActualizarMovimiento;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.AnularMovimiento;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.CrearMovimiento;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ListarMovimientos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ObtenerMovimiento;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class MovimientosEndpoints
{
    public static IEndpointRouteBuilder MapMovimientos(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/movimientos", Listar).WithTags("Movimientos privados");
        endpoints.MapPost("/movimientos", Crear).WithTags("Movimientos privados");
        endpoints.MapGet("/movimientos/{movimientoId:guid}", Obtener).WithTags("Movimientos privados");
        endpoints.MapPatch("/movimientos/{movimientoId:guid}", Actualizar)
            .WithName("patchMovimientosByMovimientoId")
            .WithTags("Movimientos privados")
            .Produces<MovimientoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapDelete("/movimientos/{movimientoId:guid}", Anular).WithTags("Movimientos privados");
        return endpoints;
    }

    private static async Task<IResult> Listar(
        Guid? cuentaId,
        DateOnly? desde,
        DateOnly? hasta,
        string? cursor,
        int? limite,
        ListarMovimientosHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var solicitudLegada = limite is null && cursor is null && cuentaId is null &&
            desde is null && hasta is null;
        return Results.Ok(await handler.Handle(new ListarMovimientosQuery(
            context.UsuarioId(), cuentaId, desde, hasta, cursor, limite ?? 100,
            solicitudLegada), cancellationToken));
    }

    private static async Task<IResult> Crear(
        CrearMovimientoRequest request,
        CrearMovimientoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var command = new CrearMovimientoCommand(
            context.UsuarioId(), context.TraceIdentifier, request.Ambito, request.CuentaId,
            request.Tipo, request.Monto, request.Descripcion, request.Fecha, request.Hora,
            request.CategoriaIds, request.DocumentoId, request.MovimientoRecurrenteId,
            request.GrupoFamiliarId, request.Id, request.TarjetaCreditoId,
            request.OperacionTarjeta);

        var response = await handler.Handle(command, cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Created($"/api/v1/movimientos/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid movimientoId,
        ObtenerMovimientoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new ObtenerMovimientoQuery(context.UsuarioId(), movimientoId),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Ok(response);
    }

    private static async Task<IResult> Anular(
        Guid movimientoId,
        AnularMovimientoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);

        await handler.Handle(
            new AnularMovimientoCommand(context.UsuarioId(), movimientoId, version, context.TraceIdentifier),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> Actualizar(
        Guid movimientoId,
        PatchMovimientosByMovimientoIdRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        ActualizarMovimientoHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Handle(new(
            context.UsuarioId(), movimientoId, version, request.Descripcion,
            request.CategoriaIds, request.DocumentoId, context.TraceIdentifier,
            request.CuentaId, request.Tipo, request.Monto, request.Fecha,
            request.Hora), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }
}
