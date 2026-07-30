using FinanzasInteligentes.Api.Contratos.Documentos;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Documentos;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.Documentos;

public static class ExportacionesEndpoints
{
    private const string Tag = "Exportaciones";

    public static IEndpointRouteBuilder MapExportaciones(this IEndpointRouteBuilder e)
    {
        e.MapGet("/exportaciones", Listar).WithName("getExportaciones").WithTags(Tag)
            .Produces<PaginaExportacionResponse>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        e.MapPost("/exportaciones", Crear).WithName("postExportaciones").WithTags(Tag)
            .Produces<ExportacionResponse>(202).ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409)
            .ProducesProblem(422).ProducesProblem(429).ProducesProblem(503);
        e.MapGet("/exportaciones/{exportacionId:guid}", Obtener)
            .WithName("getExportacionesByExportacionId").WithTags(Tag)
            .Produces<ExportacionResponse>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);
        e.MapDelete("/exportaciones/{exportacionId:guid}", Eliminar)
            .WithName("deleteExportacionesByExportacionId").WithTags(Tag)
            .Produces(204).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(412);
        e.MapPost("/exportaciones/{exportacionId:guid}/descargas", Descargar)
            .WithName("postExportacionesByExportacionIdDescargas").WithTags(Tag)
            .Produces<DescargaResponse>(201).ProducesProblem(401).ProducesProblem(403)
            .ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);
        return e;
    }

    private static async Task<IResult> Listar(
        string? cursor, long? limite, string? estado, string? formato,
        DateOnly? desde, DateOnly? hasta, ExportacionesHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.Listar(c.UsuarioId(), cursor, limite ?? 20, estado, formato, desde, hasta, ct));

    private static async Task<IResult> Crear(
        ExportacionRequest request,
        [FromQuery] string? estado, [FromQuery] string? formato,
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        ExportacionesHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.Crear(
            c.UsuarioId(), request.Formato, request.Ambito, request.GrupoFamiliarId,
            request.Desde, request.Hasta, request.Filtros, idempotencyKey,
            c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        c.Response.Headers.Location = $"/api/v1/exportaciones/{response.Id}";
        return Results.Accepted(c.Response.Headers.Location, response);
    }

    private static async Task<IResult> Obtener(
        Guid exportacionId, ExportacionesHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.Obtener(c.UsuarioId(), exportacionId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid exportacionId, [FromHeader(Name = "If-Match")] string ifMatch,
        ExportacionesHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var version)) return ETagExtensions.IfMatchInvalido(c);
        await h.Eliminar(c.UsuarioId(), exportacionId, version, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Descargar(
        Guid exportacionId, ExportacionesHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.Descargar(c.UsuarioId(), exportacionId, ct);
        c.Response.Headers.Location = response.Url;
        return Results.Created(response.Url, response);
    }
}