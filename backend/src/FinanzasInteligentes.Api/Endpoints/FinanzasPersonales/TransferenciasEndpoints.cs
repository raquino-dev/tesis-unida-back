using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Transferencias;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class TransferenciasEndpoints
{
    private const string Tag = "Transferencias internas entre cuentas";

    public static IEndpointRouteBuilder MapTransferencias(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/transferencias", Listar)
            .WithName("getTransferencias").WithTags(Tag)
            .Produces<PaginaTransferenciaResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        endpoints.MapPost("/transferencias", Crear)
            .WithName("postTransferencias").WithTags(Tag)
            .Produces<TransferenciaResponse>(201)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapGet("/transferencias/{transferenciaId:guid}", Obtener)
            .WithName("getTransferenciasByTransferenciaId").WithTags(Tag)
            .Produces<TransferenciaResponse>()
            .ProducesProblem(401).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapDelete("/transferencias/{transferenciaId:guid}", Anular)
            .WithName("deleteTransferenciasByTransferenciaId").WithTags(Tag)
            .Produces(204).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(412).ProducesProblem(500);
        return endpoints;
    }

    private static async Task<IResult> Listar(
        string? cursor, long? limite, Guid? cuentaOrigenId, Guid? cuentaDestinoId,
        DateOnly? desde, DateOnly? hasta, TransferenciasHandler handler,
        HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.Listar(
            context.UsuarioId(), cursor, limite ?? 20, cuentaOrigenId,
            cuentaDestinoId, desde, hasta, ct));

    private static async Task<IResult> Crear(
        TransferenciaRequest request,
        [FromQuery] Guid? cuentaOrigenId,
        [FromQuery] Guid? cuentaDestinoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        TransferenciasHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.Crear(
            context.UsuarioId(), request.CuentaOrigenId, request.CuentaDestinoId,
            request.Monto, request.Fecha, request.Descripcion, idempotencyKey,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/transferencias/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid transferenciaId, TransferenciasHandler handler,
        HttpContext context, CancellationToken ct)
    {
        var response = await handler.Obtener(context.UsuarioId(), transferenciaId, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Anular(
        Guid transferenciaId,
        [FromHeader(Name = "If-Match")] string ifMatch,
        TransferenciasHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        await handler.Anular(
            context.UsuarioId(), transferenciaId, version, context.TraceIdentifier, ct);
        return Results.NoContent();
    }
}