using FinanzasInteligentes.Api.Contratos.Seguridad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Seguridad;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.Seguridad;

public static class SeguridadYDispositivosEndpoints
{
    private const string TagSeguridad = "Seguridad, OTP, biometría y auditoría";
    private const string TagDispositivos = "Endpoints operativos del cliente";

    public static IEndpointRouteBuilder MapSeguridadYDispositivos(this IEndpointRouteBuilder e)
    {
        e.MapGet("/eventos-seguridad", ListarSeguridad)
            .WithName("getEventosSeguridad").WithTags(TagSeguridad)
            .Produces<PaginaEventoSeguridadResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        e.MapGet("/eventos-auditoria", ListarAuditoria)
            .WithName("getEventosAuditoria").WithTags(TagSeguridad)
            .Produces<PaginaEventoAuditoriaResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);
        e.MapPost("/dispositivos", CrearDispositivo)
            .WithName("postDispositivos").WithTags(TagDispositivos)
            .Produces<DispositivoResponse>(201).ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        e.MapPatch("/dispositivos/{dispositivoId:guid}", ActualizarDispositivo)
            .WithName("patchDispositivosByDispositivoId").WithTags(TagDispositivos)
            .Produces<DispositivoResponse>().ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(404).ProducesProblem(412).ProducesProblem(422).ProducesProblem(500);
        e.MapDelete("/dispositivos/{dispositivoId:guid}", EliminarDispositivo)
            .WithName("deleteDispositivosByDispositivoId").WithTags(TagDispositivos)
            .Produces(204).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(412).ProducesProblem(500);
        return e;
    }

    private static async Task<IResult> ListarSeguridad(
        string? cursor, long? limite, string? tipo, DateOnly? desde, DateOnly? hasta,
        SeguridadYDispositivosHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarSeguridad(
            c.UsuarioId(), cursor, limite ?? 20, tipo, desde, hasta, ct));

    private static async Task<IResult> ListarAuditoria(
        string? cursor, long? limite, string? recurso, Guid? usuarioId,
        Guid? grupoFamiliarId, DateOnly? desde, DateOnly? hasta,
        SeguridadYDispositivosHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarAuditoria(
            c.UsuarioId(), cursor, limite ?? 20, recurso, usuarioId,
            grupoFamiliarId, desde, hasta, ct));

    private static async Task<IResult> CrearDispositivo(
        PostDispositivosRequest request, SeguridadYDispositivosHandler h,
        HttpContext c, CancellationToken ct)
    {
        var response = await h.CrearDispositivo(
            c.UsuarioId(), request.IdentificadorInstalacion, request.Nombre,
            request.Plataforma, request.VersionSistema, request.VersionAplicacion,
            request.TokenPush, request.ZonaHoraria, c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/dispositivos/{response.Id}", response);
    }

    private static async Task<IResult> ActualizarDispositivo(
        Guid dispositivoId, DispositivoPatchRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        SeguridadYDispositivosHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(c);
        var response = await h.ActualizarDispositivo(
            c.UsuarioId(), dispositivoId, version, request.Nombre,
            request.VersionAplicacion, request.TokenPush, request.ZonaHoraria,
            c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> EliminarDispositivo(
        Guid dispositivoId, [FromHeader(Name = "If-Match")] string ifMatch,
        SeguridadYDispositivosHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(c);
        await h.EliminarDispositivo(
            c.UsuarioId(), dispositivoId, version, c.TraceIdentifier, ct);
        return Results.NoContent();
    }
}