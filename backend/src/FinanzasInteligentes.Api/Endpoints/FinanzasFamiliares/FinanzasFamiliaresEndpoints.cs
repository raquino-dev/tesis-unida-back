using FinanzasInteligentes.Api.Contratos.FinanzasFamiliares;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasFamiliares;
using FinanzasInteligentes.Aplicacion.FinanzasFamiliares.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasFamiliares;

public static class FinanzasFamiliaresEndpoints
{
    private const string Tag = "Finanzas familiares";

    public static IEndpointRouteBuilder MapFinanzasFamiliares(this IEndpointRouteBuilder e)
    {
        e.MapGet("/grupos-familiares", ListarGrupos).WithName("getGruposFamiliares").WithTags(Tag)
            .Produces<PaginaGrupoFamiliarResponse>();
        e.MapPost("/grupos-familiares", CrearGrupo).WithName("postGruposFamiliares").WithTags(Tag)
            .Produces<GrupoFamiliarResponse>(201);
        e.MapGet("/grupos-familiares/{grupoId:guid}", ObtenerGrupo)
            .WithName("getGruposFamiliaresByGrupoId").WithTags(Tag).Produces<GrupoFamiliarResponse>();
        e.MapPatch("/grupos-familiares/{grupoId:guid}", ActualizarGrupo)
            .WithName("patchGruposFamiliaresByGrupoId").WithTags(Tag).Produces<GrupoFamiliarResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/eliminaciones", SolicitarEliminacion)
            .WithName("postGruposFamiliaresByGrupoIdEliminaciones").WithTags(Tag)
            .Produces<ProcesoGrupoResponse>(202);
        e.MapGet("/grupos-familiares/{grupoId:guid}/eliminaciones/{eliminacionId:guid}", ObtenerEliminacion)
            .WithName("getGruposFamiliaresByGrupoIdEliminacionesByEliminacionId").WithTags(Tag)
            .Produces<ProcesoGrupoResponse>();

        e.MapGet("/grupos-familiares/{grupoId:guid}/integrantes", ListarIntegrantes)
            .WithName("getGruposFamiliaresByGrupoIdIntegrantes").WithTags(Tag)
            .Produces<PaginaIntegranteFamiliarResponse>();
        e.MapPatch("/grupos-familiares/{grupoId:guid}/integrantes/{integranteId:guid}", ActualizarIntegrante)
            .WithName("patchGruposFamiliaresByGrupoIdIntegrantesByIntegranteId").WithTags(Tag)
            .Produces<IntegranteFamiliarResponse>();
        e.MapDelete("/grupos-familiares/{grupoId:guid}/integrantes/{integranteId:guid}", EliminarIntegrante)
            .WithName("deleteGruposFamiliaresByGrupoIdIntegrantesByIntegranteId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/invitaciones", ListarInvitaciones)
            .WithName("getGruposFamiliaresByGrupoIdInvitaciones").WithTags(Tag)
            .Produces<PaginaInvitacionFamiliarResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/invitaciones", CrearInvitacion)
            .WithName("postGruposFamiliaresByGrupoIdInvitaciones").WithTags(Tag)
            .Produces<CrearInvitacionFamiliarResponse>(201);
        e.MapGet("/invitaciones-familiares/{token}", ObtenerInvitacionPublica)
            .WithName("getInvitacionesFamiliaresByToken").WithTags(Tag).AllowAnonymous()
            .Produces<InvitacionPublicaResponse>();
        e.MapPost("/invitaciones-familiares/{token}/aceptaciones", AceptarInvitacion)
            .WithName("postInvitacionesFamiliaresByTokenAceptaciones").WithTags(Tag)
            .Produces<IntegranteFamiliarResponse>(201);
        e.MapDelete("/grupos-familiares/{grupoId:guid}/invitaciones/{invitacionId:guid}", CancelarInvitacion)
            .WithName("deleteGruposFamiliaresByGrupoIdInvitacionesByInvitacionId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/cuentas-compartidas", ListarCuentas)
            .WithName("getGruposFamiliaresByGrupoIdCuentasCompartidas").WithTags(Tag)
            .Produces<PaginaCuentaCompartidaResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/cuentas-compartidas", CompartirCuenta)
            .WithName("postGruposFamiliaresByGrupoIdCuentasCompartidas").WithTags(Tag)
            .Produces<CuentaCompartidaResponse>(201);
        e.MapDelete("/grupos-familiares/{grupoId:guid}/cuentas-compartidas/{cuentaId:guid}", DesvincularCuenta)
            .WithName("deleteGruposFamiliaresByGrupoIdCuentasCompartidasByCuentaId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/categorias", ListarCategorias)
            .WithName("getGruposFamiliaresByGrupoIdCategorias").WithTags(Tag)
            .Produces<PaginaCategoriaFamiliarResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/categorias", CrearCategoria)
            .WithName("postGruposFamiliaresByGrupoIdCategorias").WithTags(Tag)
            .Produces<CategoriaFamiliarResponse>(201);
        e.MapPatch("/grupos-familiares/{grupoId:guid}/categorias/{categoriaId:guid}", ActualizarCategoria)
            .WithName("patchGruposFamiliaresByGrupoIdCategoriasByCategoriaId").WithTags(Tag)
            .Produces<CategoriaFamiliarResponse>();
        e.MapDelete("/grupos-familiares/{grupoId:guid}/categorias/{categoriaId:guid}", EliminarCategoria)
            .WithName("deleteGruposFamiliaresByGrupoIdCategoriasByCategoriaId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/movimientos", ListarMovimientos)
            .WithName("getGruposFamiliaresByGrupoIdMovimientos").WithTags(Tag)
            .Produces<PaginaMovimientoFamiliarResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/movimientos", CrearMovimiento)
            .WithName("postGruposFamiliaresByGrupoIdMovimientos").WithTags(Tag)
            .Produces<MovimientoFamiliarResponse>(201);
        e.MapGet("/grupos-familiares/{grupoId:guid}/movimientos/{movimientoId:guid}", ObtenerMovimiento)
            .WithName("getGruposFamiliaresByGrupoIdMovimientosByMovimientoId").WithTags(Tag)
            .Produces<MovimientoFamiliarResponse>();
        e.MapPatch("/grupos-familiares/{grupoId:guid}/movimientos/{movimientoId:guid}", ActualizarMovimiento)
            .WithName("patchGruposFamiliaresByGrupoIdMovimientosByMovimientoId").WithTags(Tag)
            .Produces<MovimientoFamiliarResponse>();
        e.MapDelete("/grupos-familiares/{grupoId:guid}/movimientos/{movimientoId:guid}", EliminarMovimiento)
            .WithName("deleteGruposFamiliaresByGrupoIdMovimientosByMovimientoId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/caja-compartida", ObtenerCaja)
            .WithName("getGruposFamiliaresByGrupoIdCajaCompartida").WithTags(Tag)
            .Produces<CajaCompartidaResponse>();
        e.MapGet("/grupos-familiares/{grupoId:guid}/operaciones-caja", ListarOperacionesCaja)
            .WithName("getGruposFamiliaresByGrupoIdOperacionesCaja").WithTags(Tag)
            .Produces<PaginaOperacionCajaResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/operaciones-caja", CrearOperacionCaja)
            .WithName("postGruposFamiliaresByGrupoIdOperacionesCaja").WithTags(Tag)
            .Produces<OperacionCajaResponse>(201);
        e.MapGet("/grupos-familiares/{grupoId:guid}/operaciones-caja/{operacionId:guid}", ObtenerOperacionCaja)
            .WithName("getGruposFamiliaresByGrupoIdOperacionesCajaByOperacionId").WithTags(Tag)
            .Produces<OperacionCajaResponse>();

        e.MapGet("/grupos-familiares/{grupoId:guid}/presupuestos", ListarPresupuestos)
            .WithName("getGruposFamiliaresByGrupoIdPresupuestos").WithTags(Tag)
            .Produces<PaginaPresupuestoFamiliarResponse>();
        e.MapPost("/grupos-familiares/{grupoId:guid}/presupuestos", CrearPresupuesto)
            .WithName("postGruposFamiliaresByGrupoIdPresupuestos").WithTags(Tag)
            .Produces<PresupuestoFamiliarResponse>(201);
        e.MapGet("/grupos-familiares/{grupoId:guid}/presupuestos/{presupuestoId:guid}", ObtenerPresupuesto)
            .WithName("getGruposFamiliaresByGrupoIdPresupuestosByPresupuestoId").WithTags(Tag)
            .Produces<PresupuestoFamiliarResponse>();
        e.MapPatch("/grupos-familiares/{grupoId:guid}/presupuestos/{presupuestoId:guid}", ActualizarPresupuesto)
            .WithName("patchGruposFamiliaresByGrupoIdPresupuestosByPresupuestoId").WithTags(Tag)
            .Produces<PresupuestoFamiliarResponse>();
        e.MapDelete("/grupos-familiares/{grupoId:guid}/presupuestos/{presupuestoId:guid}", EliminarPresupuesto)
            .WithName("deleteGruposFamiliaresByGrupoIdPresupuestosByPresupuestoId").WithTags(Tag)
            .Produces(204);

        e.MapGet("/grupos-familiares/{grupoId:guid}/tableros-financieros", ObtenerDashboard)
            .WithName("getGruposFamiliaresByGrupoIdTablerosFinancieros").WithTags(Tag)
            .Produces<DashboardFamiliarResponse>();
        e.MapGet("/grupos-familiares/{grupoId:guid}/reportes-financieros", ObtenerReporte)
            .WithName("getGruposFamiliaresByGrupoIdReportesFinancieros").WithTags(Tag)
            .Produces<ReporteFamiliarResponse>();
        e.MapGet("/grupos-familiares/{grupoId:guid}/proyecciones-gastos", ObtenerProyeccion)
            .WithName("getGruposFamiliaresByGrupoIdProyeccionesGastos")
            .WithTags("Predicciones, alertas y score")
            .Produces<ProyeccionFamiliarResponse>();
        return e;
    }

    private static async Task<IResult> ListarGrupos(
        string? cursor, long? limite, FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarGrupos(c.UsuarioId(), cursor, limite ?? 20, ct));

    private static async Task<IResult> CrearGrupo(
        GrupoFamiliarRequest r, FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.CrearGrupo(c.UsuarioId(), r.Nombre, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created($"/api/v1/grupos-familiares/{x.Id}", x);
    }

    private static async Task<IResult> ObtenerGrupo(
        Guid grupoId, FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.ObtenerGrupo(c.UsuarioId(), grupoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> ActualizarGrupo(
        Guid grupoId, PatchGruposFamiliaresByGrupoIdRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.ActualizarGrupo(c.UsuarioId(), grupoId, v, r.Nombre, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> SolicitarEliminacion(
        Guid grupoId, PostGruposFamiliaresByGrupoIdEliminacionesRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.SolicitarEliminacion(
            c.UsuarioId(), grupoId, v, r.VerificacionOtpId, c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Accepted(x.UrlEstado, x);
    }

    private static async Task<IResult> ObtenerEliminacion(
        Guid grupoId, Guid eliminacionId, FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.ObtenerEliminacion(c.UsuarioId(), grupoId, eliminacionId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> ListarIntegrantes(
        Guid grupoId, string? cursor, long? limite, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarIntegrantes(c.UsuarioId(), grupoId, cursor, limite ?? 20, ct));

    private static async Task<IResult> ActualizarIntegrante(
        Guid grupoId, Guid integranteId,
        PatchGruposFamiliaresByGrupoIdIntegrantesByIntegranteIdRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.ActualizarIntegrante(c.UsuarioId(), grupoId, integranteId, v, r.Rol, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> EliminarIntegrante(
        Guid grupoId, Guid integranteId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.EliminarIntegrante(c.UsuarioId(), grupoId, integranteId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarInvitaciones(
        Guid grupoId, string? cursor, long? limite, string? estado,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarInvitaciones(
            c.UsuarioId(), grupoId, cursor, limite ?? 20, estado, ct));

    private static async Task<IResult> CrearInvitacion(
        Guid grupoId, InvitacionFamiliarRequest r,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var result = await h.CrearInvitacion(
            c.UsuarioId(), grupoId, r.Correo, r.IdentificadorUsuario,
            r.Alias, r.Rol, c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(result.Response.Version);
        return Results.Created(
            $"/api/v1/invitaciones-familiares/{result.Token}", result.Response);
    }

    private static async Task<IResult> ObtenerInvitacionPublica(
        string token, FinanzasFamiliaresHandler h, CancellationToken ct) =>
        Results.Ok(await h.ObtenerInvitacionPublica(token, ct));

    private static async Task<IResult> AceptarInvitacion(
        string token, AceptacionInvitacionRequest r, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct)
    {
        var x = await h.AceptarInvitacion(c.UsuarioId(), token, r.Codigo, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created($"/api/v1/grupos-familiares/{x.Id}/integrantes/{x.Id}", x);
    }

    private static async Task<IResult> CancelarInvitacion(
        Guid grupoId, Guid invitacionId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.CancelarInvitacion(c.UsuarioId(), grupoId, invitacionId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarCuentas(
        Guid grupoId, string? cursor, long? limite, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarCuentas(c.UsuarioId(), grupoId, cursor, limite ?? 20, ct));

    private static async Task<IResult> CompartirCuenta(
        Guid grupoId, PostGruposFamiliaresByGrupoIdCuentasCompartidasRequest r,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.CompartirCuenta(c.UsuarioId(), grupoId, r.CuentaId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created(
            $"/api/v1/grupos-familiares/{grupoId}/cuentas-compartidas/{r.CuentaId}", x);
    }

    private static async Task<IResult> DesvincularCuenta(
        Guid grupoId, Guid cuentaId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.DesvincularCuenta(c.UsuarioId(), grupoId, cuentaId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarCategorias(
        Guid grupoId, string? cursor, long? limite, string? tipo,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarCategorias(
            c.UsuarioId(), grupoId, cursor, limite ?? 20, tipo, ct));

    private static async Task<IResult> CrearCategoria(
        Guid grupoId, CategoriaFamiliarRequest r, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct)
    {
        var x = await h.CrearCategoria(
            c.UsuarioId(), grupoId, r.Nombre, r.Tipo, r.Icono, r.Color, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created($"/api/v1/grupos-familiares/{grupoId}/categorias/{x.Id}", x);
    }

    private static async Task<IResult> ActualizarCategoria(
        Guid grupoId, Guid categoriaId, CategoriaFamiliarPatchRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.ActualizarCategoria(c.UsuarioId(), grupoId, categoriaId, v,
            r.Nombre, r.Tipo, r.Icono, r.Color, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> EliminarCategoria(
        Guid grupoId, Guid categoriaId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.EliminarCategoria(c.UsuarioId(), grupoId, categoriaId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListarMovimientos(
        Guid grupoId, string? cursor, long? limite, string? texto, string? tipo,
        Guid? categoriaId, Guid? cuentaId, Guid? integranteId, DateOnly? desde, DateOnly? hasta,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarMovimientos(c.UsuarioId(), grupoId, cursor, limite ?? 20,
            tipo, categoriaId, cuentaId, integranteId, desde, hasta, texto, ct));

    private static async Task<IResult> CrearMovimiento(
        Guid grupoId, MovimientoFamiliarRequest r,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.CrearMovimiento(c.UsuarioId(), grupoId, r.CuentaId, r.Tipo,
            r.Monto, r.Descripcion, r.Fecha, r.CategoriaIds, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created($"/api/v1/grupos-familiares/{grupoId}/movimientos/{x.Id}", x);
    }

    private static async Task<IResult> ObtenerMovimiento(
        Guid grupoId, Guid movimientoId, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct)
    {
        var x = await h.ObtenerMovimiento(c.UsuarioId(), grupoId, movimientoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> ActualizarMovimiento(
        Guid grupoId, Guid movimientoId,
        PatchGruposFamiliaresByGrupoIdMovimientosByMovimientoIdRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.ActualizarMovimiento(
            c.UsuarioId(), grupoId, movimientoId, v, r.Descripcion, r.CategoriaIds, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> EliminarMovimiento(
        Guid grupoId, Guid movimientoId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.EliminarMovimiento(c.UsuarioId(), grupoId, movimientoId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ObtenerCaja(
        Guid grupoId, FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.ObtenerCaja(c.UsuarioId(), grupoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> ListarOperacionesCaja(
        Guid grupoId, string? cursor, long? limite, string? tipo, Guid? integranteId,
        DateOnly? desde, DateOnly? hasta, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarOperacionesCaja(c.UsuarioId(), grupoId, cursor,
            limite ?? 20, tipo, integranteId, desde, hasta, ct));

    private static async Task<IResult> CrearOperacionCaja(
        Guid grupoId, OperacionCajaRequest r,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        var x = await h.CrearOperacionCaja(c.UsuarioId(), grupoId, r.Tipo, r.Monto,
            r.Descripcion, r.CuentaPrivadaId, r.VerificacionOtpId, ct);
        return Results.Created($"/api/v1/grupos-familiares/{grupoId}/operaciones-caja/{x.Id}", x);
    }

    private static async Task<IResult> ObtenerOperacionCaja(
        Guid grupoId, Guid operacionId, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ObtenerOperacionCaja(c.UsuarioId(), grupoId, operacionId, ct));

    private static async Task<IResult> ListarPresupuestos(
        Guid grupoId, string? cursor, long? limite, string? periodo, Guid? categoriaId,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ListarPresupuestos(
            c.UsuarioId(), grupoId, cursor, limite ?? 20, periodo, categoriaId, ct));

    private static async Task<IResult> CrearPresupuesto(
        Guid grupoId, PresupuestoFamiliarRequest r, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct)
    {
        var x = await h.CrearPresupuesto(
            c.UsuarioId(), grupoId, r.Nombre, r.Monto, r.Periodo, r.CategoriaIds, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Created($"/api/v1/grupos-familiares/{grupoId}/presupuestos/{x.Id}", x);
    }

    private static async Task<IResult> ObtenerPresupuesto(
        Guid grupoId, Guid presupuestoId, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct)
    {
        var x = await h.ObtenerPresupuesto(c.UsuarioId(), grupoId, presupuestoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> ActualizarPresupuesto(
        Guid grupoId, Guid presupuestoId, PresupuestoFamiliarPatchRequest r,
        [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        var x = await h.ActualizarPresupuesto(c.UsuarioId(), grupoId, presupuestoId, v,
            r.Nombre, r.Monto, r.Periodo, r.CategoriaIds, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(x.Version);
        return Results.Ok(x);
    }

    private static async Task<IResult> EliminarPresupuesto(
        Guid grupoId, Guid presupuestoId, [FromHeader(Name = "If-Match")] string ifMatch,
        FinanzasFamiliaresHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var v)) return ETagExtensions.IfMatchInvalido(c);
        await h.EliminarPresupuesto(c.UsuarioId(), grupoId, presupuestoId, v, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ObtenerDashboard(
        Guid grupoId, DateOnly? desde, DateOnly? hasta, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ObtenerDashboard(c.UsuarioId(), grupoId, desde, hasta, ct));

    private static async Task<IResult> ObtenerReporte(
        Guid grupoId, string? rango, DateOnly? desde, DateOnly? hasta, string? tipo,
        Guid? categoriaId, Guid? integranteId, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ObtenerReporte(c.UsuarioId(), grupoId, rango, desde, hasta,
            tipo, categoriaId, integranteId, ct));

    private static async Task<IResult> ObtenerProyeccion(
        Guid grupoId, string? periodo, FinanzasFamiliaresHandler h,
        HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.ObtenerProyeccion(c.UsuarioId(), grupoId, periodo, ct));
}
