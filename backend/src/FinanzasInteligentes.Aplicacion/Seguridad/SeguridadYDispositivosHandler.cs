using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Seguridad;
using System.Text.Json;

namespace FinanzasInteligentes.Aplicacion.Seguridad;

public sealed record DispositivoResponse(
    Guid Id, string IdentificadorInstalacion, string Nombre, string Plataforma,
    string VersionAplicacion, bool Confiable, DateTimeOffset UltimoAccesoEn, long Version);
public sealed record EventoSeguridadResponse(
    Guid Id, string Tipo, string Descripcion, bool Exitoso,
    string OrigenAproximado, string Dispositivo, DateTimeOffset OcurridoEn);
public sealed record EventoAuditoriaResponse(
    Guid Id, string Accion, string Recurso, Guid RecursoId, Guid UsuarioId,
    Guid? GrupoFamiliarId, JsonElement? DatosAnteriores,
    JsonElement DatosPosteriores, string CorrelationId, DateTimeOffset OcurridoEn);
public sealed record PaginacionEventosResponse(string? SiguienteCursor, bool HayMas, long Limite);
public sealed record PaginaEventoSeguridadResponse(
    IReadOnlyCollection<EventoSeguridadResponse> Datos, PaginacionEventosResponse Paginacion);
public sealed record PaginaEventoAuditoriaResponse(
    IReadOnlyCollection<EventoAuditoriaResponse> Datos, PaginacionEventosResponse Paginacion);

public sealed class SeguridadYDispositivosHandler(
    ISeguridadRepository seguridad, IProtectorTokenPush protector,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<DispositivoResponse> CrearDispositivo(
        Guid usuarioId, string identificador, string nombre, string plataforma,
        string versionSistema, string versionAplicacion, string tokenPush,
        string zonaHoraria, string correlationId, CancellationToken ct)
    {
        if (await seguridad.ObtenerDispositivoPorInstalacion(
            usuarioId, identificador.Trim(), ct) is not null)
            throw new ConflictException(
                "dispositivo_existente", "La instalación ya está registrada.");
        var entity = Dispositivo.Crear(
            usuarioId, identificador, nombre, plataforma, versionSistema,
            versionAplicacion, protector.Proteger(tokenPush), zonaHoraria);
        seguridad.Agregar(entity);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuarioId, "dispositivo-registrado",
            "Se registró una nueva instalación de la aplicación.", true,
            entity.Id, entity.Nombre));
        seguridad.Agregar(EventoAuditoria.Crear(
            usuarioId, null, "dispositivo.creado", "dispositivo", entity.Id,
            null, new { entity.Nombre, entity.Plataforma, entity.VersionAplicacion },
            correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity);
    }

    public async Task<DispositivoResponse> ActualizarDispositivo(
        Guid usuarioId, Guid id, long version, string? nombre,
        string? versionAplicacion, string? tokenPush, string? zonaHoraria,
        string correlationId, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, id, false, ct);
        VerificarVersion(entity.Version, version);
        var anterior = new { entity.Nombre, entity.VersionAplicacion, entity.ZonaHoraria };
        entity.Actualizar(
            nombre, versionAplicacion,
            tokenPush is null ? null : protector.Proteger(tokenPush), zonaHoraria);
        seguridad.Agregar(EventoAuditoria.Crear(
            usuarioId, null, "dispositivo.actualizado", "dispositivo", entity.Id,
            anterior, new { entity.Nombre, entity.VersionAplicacion, entity.ZonaHoraria },
            correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity);
    }

    public async Task EliminarDispositivo(
        Guid usuarioId, Guid id, long version, string correlationId, CancellationToken ct)
    {
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(ct);
        var entity = await Existente(usuarioId, id, false, ct);
        VerificarVersion(entity.Version, version);
        entity.Revocar();
        await seguridad.RevocarSesionesDeDispositivo(
            usuarioId, entity.IdentificadorInstalacion, ct);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuarioId, "dispositivo-revocado",
            "Se desvinculó una instalación y se revocaron sus sesiones.", true,
            entity.Id, entity.Nombre));
        seguridad.Agregar(EventoAuditoria.Crear(
            usuarioId, null, "dispositivo.eliminado", "dispositivo", entity.Id,
            new { confiable = true }, new { confiable = false }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await transaction.Confirmar(ct);
    }

    public async Task<PaginaEventoSeguridadResponse> ListarSeguridad(
        Guid usuarioId, string? cursor, long limite, string? tipo,
        DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var cursorId = ValidarPagina(cursor, limite);
        var items = (await seguridad.ListarEventosSeguridad(usuarioId, ct))
            .Where(x => tipo is null || x.Tipo == tipo)
            .Where(x => desde is null || DateOnly.FromDateTime(x.OcurridoEn.UtcDateTime) >= desde)
            .Where(x => hasta is null || DateOnly.FromDateTime(x.OcurridoEn.UtcDateTime) <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        return new(pagina.Select(x => new EventoSeguridadResponse(
            x.Id, x.Tipo, x.Descripcion, x.Exitoso, x.OrigenAproximado,
            x.Dispositivo, x.OcurridoEn)).ToArray(),
            new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    public async Task<PaginaEventoAuditoriaResponse> ListarAuditoria(
        Guid usuarioIdActual, string? cursor, long limite, string? recurso,
        Guid? usuarioId, Guid? grupoId, DateOnly? desde, DateOnly? hasta,
        CancellationToken ct)
    {
        var cursorId = ValidarPagina(cursor, limite);
        var items = (await seguridad.ListarEventosAuditoria(usuarioIdActual, ct))
            .Where(x => recurso is null || x.Recurso == recurso)
            .Where(x => usuarioId is null || x.UsuarioId == usuarioId)
            .Where(x => grupoId is null || x.GrupoFamiliarId == grupoId)
            .Where(x => desde is null || DateOnly.FromDateTime(x.OcurridoEn.UtcDateTime) >= desde)
            .Where(x => hasta is null || DateOnly.FromDateTime(x.OcurridoEn.UtcDateTime) <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        return new(pagina.Select(x => new EventoAuditoriaResponse(
            x.Id, x.Accion, x.Recurso, x.RecursoId, x.UsuarioId!.Value,
            x.GrupoFamiliarId, x.DatosAnteriores?.RootElement.Clone(),
            x.DatosPosteriores.RootElement.Clone(), x.CorrelationId, x.OcurridoEn)).ToArray(),
            new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    private async Task<Dispositivo> Existente(
        Guid usuarioId, Guid id, bool read, CancellationToken ct) =>
        await seguridad.ObtenerDispositivo(usuarioId, id, read, ct)
        ?? throw new NotFoundException("dispositivo_no_encontrado", "El dispositivo no existe.");

    private static DispositivoResponse Map(Dispositivo x) =>
        new(x.Id, x.IdentificadorInstalacion, x.Nombre, x.Plataforma,
            x.VersionAplicacion, x.Confiable, x.UltimoAccesoEn, x.Version);

    private static void VerificarVersion(long actual, long esperada)
    {
        if (actual != esperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión está desactualizada.");
    }

    private static Guid? ValidarPagina(string? cursor, long limite)
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        if (cursor is null) return null;
        if (!Guid.TryParse(cursor, out var parsed))
            throw new DomainException("cursor_invalido", "El cursor no es válido.");
        return parsed;
    }
}