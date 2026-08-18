using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Planificacion;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class PlanificacionEndpoints
{
    private const string TagPresupuestos = "Presupuestos privados";
    private const string TagMetas = "Metas de ahorro";

    public static IEndpointRouteBuilder MapPlanificacion(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/presupuestos", ListarPresupuestos)
            .WithName("getPresupuestos").WithTags(TagPresupuestos)
            .Produces<PaginaPresupuestoResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        endpoints.MapPost("/presupuestos", CrearPresupuesto)
            .WithName("postPresupuestos").WithTags(TagPresupuestos)
            .Produces<PresupuestoResponse>(201)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapGet("/presupuestos/{presupuestoId:guid}", ObtenerPresupuesto)
            .WithName("getPresupuestosByPresupuestoId").WithTags(TagPresupuestos)
            .Produces<PresupuestoResponse>()
            .ProducesProblem(401).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapPatch("/presupuestos/{presupuestoId:guid}", ActualizarPresupuesto)
            .WithName("patchPresupuestosByPresupuestoId").WithTags(TagPresupuestos)
            .Produces<PresupuestoResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(412).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapDelete("/presupuestos/{presupuestoId:guid}", EliminarPresupuesto)
            .WithName("deletePresupuestosByPresupuestoId").WithTags(TagPresupuestos)
            .Produces(204).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(412).ProducesProblem(500);
        endpoints.MapGet("/resumen-presupuestario", ObtenerResumen)
            .WithName("getResumenPresupuestario").WithTags(TagPresupuestos)
            .Produces<ResumenPresupuestarioResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);

        endpoints.MapGet("/metas-ahorro", ListarMetas)
            .WithName("getMetasAhorro").WithTags(TagMetas)
            .Produces<PaginaMetaAhorroResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(500);
        endpoints.MapPost("/metas-ahorro", CrearMeta)
            .WithName("postMetasAhorro").WithTags(TagMetas)
            .Produces<MetaAhorroResponse>(201)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403)
            .ProducesProblem(404).ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapGet("/metas-ahorro/{metaId:guid}", ObtenerMeta)
            .WithName("getMetasAhorroByMetaId").WithTags(TagMetas)
            .Produces<MetaAhorroResponse>()
            .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapPatch("/metas-ahorro/{metaId:guid}", ActualizarMeta)
            .WithName("patchMetasAhorroByMetaId").WithTags(TagMetas)
            .Produces<MetaAhorroResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403)
            .ProducesProblem(404).ProducesProblem(412).ProducesProblem(422).ProducesProblem(500);
        endpoints.MapDelete("/metas-ahorro/{metaId:guid}", EliminarMeta)
            .WithName("deleteMetasAhorroByMetaId").WithTags(TagMetas)
            .Produces(204).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(412).ProducesProblem(500);
        endpoints.MapGet("/metas-ahorro/{metaId:guid}/aportes", ListarAportes)
            .WithName("getMetasAhorroByMetaIdAportes").WithTags(TagMetas)
            .Produces<PaginaAporteMetaResponse>()
            .ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        endpoints.MapPost("/metas-ahorro/{metaId:guid}/aportes", CrearAporte)
            .WithName("postMetasAhorroByMetaIdAportes").WithTags(TagMetas)
            .Produces<AporteMetaResponse>(201)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403)
            .ProducesProblem(404).ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);

        return endpoints;
    }

    private static async Task<IResult> ListarPresupuestos(
        string? cursor, long? limite, string? periodo, string? estado, Guid? categoriaId,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ListarPresupuestos(
            context.UsuarioId(), cursor, limite ?? 20, periodo, estado, categoriaId, ct));

    private static async Task<IResult> CrearPresupuesto(
        PresupuestoRequest request,
        [FromQuery] string? periodo,
        [FromQuery] string? estado,
        [FromQuery] Guid? categoriaId,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.CrearPresupuesto(
            context.UsuarioId(), request.Ambito, request.GrupoFamiliarId,
            request.Nombre, request.Monto, request.Periodo, request.CategoriaIds,
            context.TraceIdentifier, request.Id, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/presupuestos/{response.Id}", response);
    }

    private static async Task<IResult> ObtenerPresupuesto(
        Guid presupuestoId, PlanificacionFinancieraHandler handler,
        HttpContext context, CancellationToken ct)
    {
        var response = await handler.ObtenerPresupuesto(context.UsuarioId(), presupuestoId, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ActualizarPresupuesto(
        Guid presupuestoId, PresupuestoPatchRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.ActualizarPresupuesto(
            context.UsuarioId(), presupuestoId, version, request.Ambito,
            request.GrupoFamiliarId, request.Nombre, request.Monto,
            request.Periodo, request.CategoriaIds, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> EliminarPresupuesto(
        Guid presupuestoId,
        [FromHeader(Name = "If-Match")] string ifMatch,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        await handler.EliminarPresupuesto(context.UsuarioId(), presupuestoId, version, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ObtenerResumen(
        DateOnly? desde, DateOnly? hasta, PlanificacionFinancieraHandler handler,
        HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ObtenerResumen(context.UsuarioId(), desde, hasta, ct));

    private static async Task<IResult> ListarMetas(
        string? cursor, long? limite, string? ambito, Guid? grupoFamiliarId,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ListarMetas(
            context.UsuarioId(), cursor, limite ?? 20, ambito, grupoFamiliarId, ct));

    private static async Task<IResult> CrearMeta(
        MetaAhorroRequest request,
        [FromQuery] string? ambito,
        [FromQuery] Guid? grupoFamiliarId,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.CrearMeta(
            context.UsuarioId(), request.Ambito, request.GrupoFamiliarId,
            request.Nombre, request.MontoObjetivo, request.FechaObjetivo,
            request.CuentaId, context.TraceIdentifier, request.Id, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/metas-ahorro/{response.Id}", response);
    }

    private static async Task<IResult> ObtenerMeta(
        Guid metaId, PlanificacionFinancieraHandler handler,
        HttpContext context, CancellationToken ct)
    {
        var response = await handler.ObtenerMeta(context.UsuarioId(), metaId, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ActualizarMeta(
        Guid metaId, MetaAhorroPatchRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.ActualizarMeta(
            context.UsuarioId(), metaId, version, request.Nombre,
            request.MontoObjetivo, request.FechaObjetivo, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> EliminarMeta(
        Guid metaId,
        [FromHeader(Name = "If-Match")] string ifMatch,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        await handler.EliminarMeta(context.UsuarioId(), metaId, version, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarAportes(
        Guid metaId, string? cursor, long? limite,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ListarAportes(
            context.UsuarioId(), metaId, cursor, limite ?? 20, ct));

    private static async Task<IResult> CrearAporte(
        Guid metaId, AporteMetaRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        PlanificacionFinancieraHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.CrearAporte(
            context.UsuarioId(), metaId, request.Monto, request.CuentaOrigenId,
            request.Descripcion, idempotencyKey, context.TraceIdentifier, ct);
        return Results.Created(
            $"/api/v1/metas-ahorro/{metaId}/aportes/{response.Id}", response);
    }
}
