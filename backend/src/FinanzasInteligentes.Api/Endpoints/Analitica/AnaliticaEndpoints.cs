using FinanzasInteligentes.Api.Contratos.Analitica;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Analitica;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.Analitica;

public static class AnaliticaEndpoints
{
    private const string Tag = "Predicciones, alertas y score";

    public static IEndpointRouteBuilder MapAnalitica(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/proyecciones-gastos", Proyectar)
            .WithName("getProyeccionesGastos").WithTags(Tag)
            .Produces<ProyeccionResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403)
            .ProducesProblem(422).ProducesProblem(500);
        endpoints.MapGet("/alertas-financieras", ListarAlertas)
            .WithName("getAlertasFinancieras").WithTags(Tag)
            .Produces<PaginaAlertaResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        endpoints.MapGet("/alertas-financieras/{alertaId:guid}", ObtenerAlerta)
            .WithName("getAlertasFinancierasByAlertaId").WithTags(Tag)
            .Produces<AlertaResponse>()
            .ProducesProblem(401).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapPatch("/alertas-financieras/{alertaId:guid}", ActualizarAlerta)
            .WithName("patchAlertasFinancierasByAlertaId").WithTags(Tag)
            .Produces<AlertaResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(412).ProducesProblem(500);
        endpoints.MapGet("/score-financiero", ObtenerScore)
            .WithName("getScoreFinanciero").WithTags(Tag)
            .Produces<ScoreResponse>()
            .ProducesProblem(401).ProducesProblem(422).ProducesProblem(500);
        return endpoints;
    }

    private static async Task<IResult> Proyectar(
        string? ambito, string? periodo, AnaliticaHandler handler,
        HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.Proyectar(context.UsuarioId(), ambito, periodo, ct));

    private static async Task<IResult> ListarAlertas(
        string? cursor, long? limite, string? nivel, bool? leida,
        DateOnly? desde, DateOnly? hasta, AnaliticaHandler handler,
        HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ListarAlertas(
            context.UsuarioId(), cursor, limite ?? 20, nivel, leida, desde, hasta, ct));

    private static async Task<IResult> ObtenerAlerta(
        Guid alertaId, AnaliticaHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.ObtenerAlerta(context.UsuarioId(), alertaId, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ActualizarAlerta(
        Guid alertaId, PatchAlertasFinancierasByAlertaIdRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        AnaliticaHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.ActualizarAlerta(
            context.UsuarioId(), alertaId, version, request.Leida, request.Archivada, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ObtenerScore(
        AnaliticaHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ObtenerScore(context.UsuarioId(), ct));
}