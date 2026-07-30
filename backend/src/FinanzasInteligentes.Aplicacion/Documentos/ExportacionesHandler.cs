using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Documentos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.Documentos;

public sealed record FiltrosExportacionRequest(
    string? Tipo, Guid? CategoriaId, Guid? CuentaId, string Documento);
public sealed record TotalesExportacionResponse(long Ingresos, long Gastos, long Transferido);
public sealed record ExportacionResponse(
    Guid Id, string Formato, string Estado, DateTimeOffset CreadoEn,
    DateTimeOffset FinalizadoEn, long CantidadMovimientos,
    TotalesExportacionResponse Totales, DateTimeOffset ExpiraEn, long Version);
public sealed record PaginaExportacionResponse(
    IReadOnlyCollection<ExportacionResponse> Datos, PaginacionDocumentosResponse Paginacion);

public sealed class ExportacionesHandler(
    IDocumentosRepository documentos, IFamiliasRepository familias,
    IArchivoStorage storage, IFinanzasRepository finanzas, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaExportacionResponse> Listar(
        Guid usuarioId, string? cursor, long limite, string? estado, string? formato,
        DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        Guid? cursorId = null;
        if (cursor is not null)
        {
            if (!Guid.TryParse(cursor, out var parsed))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = parsed;
        }
        var items = (await documentos.ListarExportaciones(usuarioId, ct))
            .Where(x => estado is null || x.Estado == estado)
            .Where(x => formato is null || x.Formato == formato)
            .Where(x => desde is null || x.Desde >= desde)
            .Where(x => hasta is null || x.Hasta <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        return new(pagina.Select(Map).ToArray(),
            new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    public async Task<ExportacionResponse> Crear(
        Guid usuarioId, string formato, string ambito, Guid? grupoId,
        DateOnly desde, DateOnly hasta, FiltrosExportacionRequest filtros,
        string idempotencyKey, string correlationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            throw new DomainException("idempotencia_invalida", "Idempotency-Key es requerido.");
        if (ambito == "familiar" && grupoId is not null &&
            !await familias.UsuarioEsIntegrante(grupoId.Value, usuarioId, ct))
            throw new ForbiddenException("grupo_no_autorizado", "No pertenece al grupo familiar.");
        var hash = Hash(idempotencyKey);
        var repetida = await documentos.ObtenerExportacionPorHash(usuarioId, hash, ct);
        if (repetida is not null)
        {
            if (repetida.Formato != formato || repetida.Ambito != ambito ||
                repetida.GrupoFamiliarId != grupoId || repetida.Desde != desde ||
                repetida.Hasta != hasta || repetida.TipoMovimiento != filtros.Tipo ||
                repetida.CategoriaId != filtros.CategoriaId || repetida.CuentaId != filtros.CuentaId ||
                repetida.Documento != filtros.Documento)
                throw new ConflictException("idempotencia_en_conflicto", "La clave ya se utilizó con otros filtros.");
            return Map(repetida);
        }
        var entity = Exportacion.Crear(
            usuarioId, formato, ambito, grupoId, desde, hasta, filtros.Tipo,
            filtros.CategoriaId, filtros.CuentaId, filtros.Documento, hash);
        documentos.Agregar(entity);
        finanzas.Agregar(EventoOutbox.Crear(
            "exportacion.solicitada", "exportacion", entity.Id,
            new { entity.Id }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity);
    }

    public async Task<ExportacionResponse> Obtener(
        Guid usuarioId, Guid id, CancellationToken ct) =>
        Map(await Existente(usuarioId, id, true, ct));

    public async Task Eliminar(
        Guid usuarioId, Guid id, long version, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, id, false, ct);
        if (entity.Version != version)
            throw new PreconditionFailedException("etag_desactualizado", "La versión está desactualizada.");
        if (entity.Estado is "pendiente" or "procesando")
            throw new ConflictException("procesamiento_activo", "La exportación está siendo generada.");
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
        if (entity.ClaveObjeto is not null) await storage.Eliminar(entity.ClaveObjeto, ct);
    }

    public async Task<DescargaResponse> Descargar(
        Guid usuarioId, Guid id, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, id, true, ct);
        if (entity.Estado != "completado" || entity.ClaveObjeto is null ||
            entity.ExpiraEn <= DateTimeOffset.UtcNow)
            throw new ConflictException("exportacion_no_completada", "La exportación no está disponible.");
        var expira = DateTimeOffset.UtcNow.AddMinutes(10);
        return new(storage.CrearUrlTemporal(entity.ClaveObjeto, expira), expira);
    }

    private async Task<Exportacion> Existente(Guid u, Guid id, bool read, CancellationToken ct) =>
        await documentos.ObtenerExportacion(u, id, read, ct)
        ?? throw new NotFoundException("exportacion_no_encontrada", "La exportación no existe.");

    private static ExportacionResponse Map(Exportacion x) =>
        new(x.Id, x.Formato, x.Estado, x.CreadoEn, x.FinalizadoEn ?? x.CreadoEn,
            x.CantidadMovimientos,
            new(x.TotalIngresos, x.TotalGastos, x.TotalTransferido),
            x.ExpiraEn ?? x.CreadoEn.AddDays(7), x.Version);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}