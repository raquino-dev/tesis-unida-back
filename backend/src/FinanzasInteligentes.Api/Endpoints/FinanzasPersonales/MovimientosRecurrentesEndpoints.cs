using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Recurrencias;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class MovimientosRecurrentesEndpoints
{
    private const string Tag = "Movimientos recurrentes";

    public static IEndpointRouteBuilder MapMovimientosRecurrentes(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/movimientos-recurrentes", Listar)
            .WithName("getMovimientosRecurrentes").WithTags(Tag)
            .Produces<PaginaMovimientoRecurrenteResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        endpoints.MapPost("/movimientos-recurrentes", Crear)
            .WithName("postMovimientosRecurrentes").WithTags(Tag)
            .Produces<MovimientoRecurrenteResponse>(201)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapGet("/movimientos-recurrentes/{recurrenteId:guid}", Obtener)
            .WithName("getMovimientosRecurrentesByRecurrenteId").WithTags(Tag)
            .Produces<MovimientoRecurrenteResponse>()
            .ProducesProblem(401).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapPatch("/movimientos-recurrentes/{recurrenteId:guid}", Actualizar)
            .WithName("patchMovimientosRecurrentesByRecurrenteId").WithTags(Tag)
            .Produces<MovimientoRecurrenteResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(412).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapDelete("/movimientos-recurrentes/{recurrenteId:guid}", Eliminar)
            .WithName("deleteMovimientosRecurrentesByRecurrenteId").WithTags(Tag)
            .Produces(204).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(412).ProducesProblem(500);
        return endpoints;
    }

    private static async Task<IResult> Listar(
        string? cursor, long? limite, string? estado, string? tipo,
        MovimientosRecurrentesHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.Listar(
            context.UsuarioId(), cursor, limite ?? 20, estado, tipo, ct));

    private static async Task<IResult> Crear(
        MovimientoRecurrenteRequest request,
        [FromQuery] string? estado,
        [FromQuery] string? tipo,
        MovimientosRecurrentesHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.Crear(
            context.UsuarioId(), request.CuentaId, request.Tipo, request.Monto,
            request.CategoriaIds, request.Descripcion, request.FechaInicio,
            request.FechaFin, request.Frecuencia, request.CantidadOcurrencias,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/movimientos-recurrentes/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid recurrenteId, MovimientosRecurrentesHandler handler,
        HttpContext context, CancellationToken ct)
    {
        var response = await handler.Obtener(context.UsuarioId(), recurrenteId, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Actualizar(
        Guid recurrenteId, MovimientoRecurrentePatchRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        MovimientosRecurrentesHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Actualizar(
            context.UsuarioId(), recurrenteId, version, request.CuentaId,
            request.Tipo, request.Monto, request.CategoriaIds, request.Descripcion,
            request.FechaInicio, request.FechaFin, request.FechaFinEspecificada,
            request.Frecuencia, request.CantidadOcurrencias,
            request.CantidadOcurrenciasEspecificada, request.Estado, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid recurrenteId,
        [FromHeader(Name = "If-Match")] string ifMatch,
        MovimientosRecurrentesHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        await handler.Eliminar(context.UsuarioId(), recurrenteId, version, ct);
        return Results.NoContent();
    }
}
