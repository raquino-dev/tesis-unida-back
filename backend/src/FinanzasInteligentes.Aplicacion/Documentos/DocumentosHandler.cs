using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Documentos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinanzasInteligentes.Aplicacion.Documentos;

public sealed record PaginacionDocumentosResponse(string? SiguienteCursor, bool HayMas, long Limite);
public sealed record DocumentoFinancieroResponse(
    Guid Id, string NombreOriginal, string Tipo, string MimeType, long TamanoBytes,
    string Sha256, string EstadoArchivo, string EstadoProcesamiento,
    Guid ProcesamientoId, DateTimeOffset CreadoEn, long Version);
public sealed record PaginaDocumentoFinancieroResponse(
    IReadOnlyCollection<DocumentoFinancieroResponse> Datos, PaginacionDocumentosResponse Paginacion);
public sealed record DatosDetectadosResponse(
    long? Monto, DateOnly? Fecha, string? Comercio, Guid? CategoriaSugeridaId, string? CdcSifen);
public sealed record ProcesamientoDocumentalResponse(
    Guid Id, Guid DocumentoId, string Tipo, string Estado, double? Confianza,
    JsonElement DatosDetectados, IReadOnlyCollection<string> Advertencias,
    DateTimeOffset? IniciadoEn, DateTimeOffset? FinalizadoEn, long Version);
public sealed record DescargaResponse(string Url, DateTimeOffset ExpiraEn);

public sealed class DocumentosHandler(
    IDocumentosRepository documentos, IFamiliasRepository familias,
    IArchivoStorage storage, IValidadorDocumento validadorDocumento,
    IFinanzasRepository finanzas, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaDocumentoFinancieroResponse> Listar(
        Guid usuarioId, string? cursor, long limite, string? tipo, string? estado,
        DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        ValidarPagina(limite, cursor, out var cursorId);
        var items = (await documentos.ListarDocumentos(usuarioId, ct))
            .Where(x => tipo is null || x.Tipo == tipo)
            .Where(x => estado is null || x.EstadoArchivo == estado)
            .Where(x => desde is null || DateOnly.FromDateTime(x.CreadoEn.UtcDateTime) >= desde)
            .Where(x => hasta is null || DateOnly.FromDateTime(x.CreadoEn.UtcDateTime) <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        var mapped = new List<DocumentoFinancieroResponse>();
        foreach (var item in pagina) mapped.Add(await Map(item, ct));
        return new(mapped, new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    public async Task<DocumentoFinancieroResponse> Crear(
        Guid usuarioId, Stream archivo, string nombre, string mime, long tamano,
        string tipo, string ambito, Guid? grupoId, string shaHeader,
        string idempotencyKey, string correlationId, CancellationToken ct)
    {
        ValidarClave(idempotencyKey);
        await AutorizarAmbito(usuarioId, ambito, grupoId, ct);
        if (tamano <= 0 || tamano > validadorDocumento.TamanoMaximoBytes)
            throw new DomainException(
                "tamano_invalido",
                $"El archivo debe tener entre 1 byte y {validadorDocumento.TamanoMaximoBytes} bytes.");
        using var buffer = new MemoryStream();
        await archivo.CopyToAsync(buffer, ct);
        var contenido = buffer.ToArray();
        if (contenido.LongLength != tamano)
            throw new DomainException("tamano_no_coincide", "El tamaño declarado no coincide con el contenido.");
        validadorDocumento.Validar(tipo, mime, nombre, contenido);
        var sha = Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();
        if (!string.Equals(sha, shaHeader, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("hash_no_coincide", "X-Content-SHA256 no coincide con el archivo.");
        var hashIdem = Hash(idempotencyKey);
        var repetido = await documentos.ObtenerDocumentoPorIdempotencia(usuarioId, hashIdem, ct);
        if (repetido is not null)
        {
            if (repetido.Sha256 != sha || repetido.Tipo != tipo || repetido.Ambito != ambito)
                throw new ConflictException("idempotencia_en_conflicto", "La clave ya se utilizó con otro archivo.");
            return await Map(repetido, ct);
        }
        if (await documentos.ExisteDocumentoConHash(usuarioId, sha, ct))
            throw new ConflictException("archivo_duplicado", "El archivo ya fue cargado.");
        if (await documentos.ContarDocumentos(usuarioId, ct) >=
            validadorDocumento.CantidadMaximaPorUsuario)
            throw new ConflictException(
                "limite_documentos_alcanzado", "Se alcanzó el límite de documentos del usuario.");
        var id = Guid.CreateVersion7();
        var clave = $"documentos/{usuarioId:N}/{id:N}";
        buffer.Position = 0;
        await storage.Guardar(clave, buffer, ct);
        try
        {
            var documento = DocumentoFinanciero.Crear(
                id, usuarioId, ambito, grupoId, clave, nombre, tipo, mime, tamano, sha, hashIdem);
            var proceso = ProcesamientoDocumental.Crear(
                documento.Id, tipo == "xml-sifen" ? "sifen" : "ocr", Hash($"inicial:{id}"));
            documentos.Agregar(documento);
            documentos.Agregar(proceso);
            finanzas.Agregar(EventoOutbox.Crear(
                "documento.procesamiento-solicitado", "procesamiento-documental",
                proceso.Id, new { proceso.Id }, correlationId));
            await unidadDeTrabajo.GuardarCambios(ct);
            return Map(documento, proceso);
        }
        catch
        {
            await storage.Eliminar(clave, ct);
            throw;
        }
    }

    public async Task<DocumentoFinancieroResponse> Obtener(
        Guid usuarioId, Guid id, CancellationToken ct) =>
        await Map(await Documento(usuarioId, id, true, ct), ct);

    public async Task Eliminar(Guid usuarioId, Guid id, long version, CancellationToken ct)
    {
        var documento = await Documento(usuarioId, id, false, ct);
        VerificarVersion(documento.Version, version);
        var proceso = await documentos.ObtenerUltimoProcesamiento(id, ct);
        if (proceso?.Estado is "pendiente" or "procesando")
            throw new ConflictException("procesamiento_activo", "El documento está siendo procesado.");
        documento.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
        await storage.Eliminar(documento.ClaveObjeto, ct);
    }

    public async Task<DescargaResponse> DescargarDocumento(
        Guid usuarioId, Guid id, CancellationToken ct)
    {
        var documento = await Documento(usuarioId, id, true, ct);
        var expira = DateTimeOffset.UtcNow.AddMinutes(10);
        return new(storage.CrearUrlTemporal(documento.ClaveObjeto, expira), expira);
    }

    public async Task<ProcesamientoDocumentalResponse> ObtenerProceso(
        Guid usuarioId, Guid id, CancellationToken ct) =>
        Map(await Proceso(usuarioId, id, true, ct));

    public async Task<ProcesamientoDocumentalResponse> CrearProceso(
        Guid usuarioId, Guid documentoId, string tipo, string idempotencyKey,
        string correlationId, CancellationToken ct)
    {
        ValidarClave(idempotencyKey);
        await Documento(usuarioId, documentoId, true, ct);
        var hash = Hash(idempotencyKey);
        var repetido = await documentos.ObtenerProcesamientoPorHash(documentoId, hash, ct);
        if (repetido is not null)
        {
            if (repetido.Tipo != tipo)
                throw new ConflictException("idempotencia_en_conflicto", "La clave ya se utilizó con otro tipo.");
            return Map(repetido);
        }
        var actual = await documentos.ObtenerUltimoProcesamiento(documentoId, ct);
        if (actual?.Estado is "pendiente" or "procesando")
            throw new ConflictException("procesamiento_activo", "Ya existe un procesamiento activo.");
        var proceso = ProcesamientoDocumental.Crear(documentoId, tipo, hash);
        documentos.Agregar(proceso);
        finanzas.Agregar(EventoOutbox.Crear(
            "documento.procesamiento-solicitado", "procesamiento-documental",
            proceso.Id, new { proceso.Id }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(proceso);
    }

    public async Task<ProcesamientoDocumentalResponse> Corregir(
        Guid usuarioId, Guid id, long version, JsonDocument datos, CancellationToken ct)
    {
        var proceso = await Proceso(usuarioId, id, false, ct);
        VerificarVersion(proceso.Version, version);
        proceso.Corregir(datos);
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(proceso);
    }

    private async Task AutorizarAmbito(Guid usuarioId, string ambito, Guid? grupoId, CancellationToken ct)
    {
        if (ambito == "familiar" && grupoId is not null &&
            !await familias.UsuarioEsIntegrante(grupoId.Value, usuarioId, ct))
            throw new ForbiddenException("grupo_no_autorizado", "No pertenece al grupo familiar.");
    }

    private async Task<DocumentoFinanciero> Documento(Guid u, Guid id, bool read, CancellationToken ct) =>
        await documentos.ObtenerDocumento(u, id, read, ct)
        ?? throw new NotFoundException("documento_no_encontrado", "El documento no existe.");

    private async Task<ProcesamientoDocumental> Proceso(Guid u, Guid id, bool read, CancellationToken ct) =>
        await documentos.ObtenerProcesamiento(u, id, read, ct)
        ?? throw new NotFoundException("procesamiento_no_encontrado", "El procesamiento no existe.");

    private async Task<DocumentoFinancieroResponse> Map(DocumentoFinanciero d, CancellationToken ct) =>
        Map(d, await documentos.ObtenerUltimoProcesamiento(d.Id, ct)
            ?? throw new NotFoundException("procesamiento_no_encontrado", "El procesamiento no existe."));

    private static DocumentoFinancieroResponse Map(DocumentoFinanciero d, ProcesamientoDocumental p) =>
        new(d.Id, d.NombreOriginal, d.Tipo, d.MimeType, d.TamanoBytes, d.Sha256,
            d.EstadoArchivo, p.Estado, p.Id, d.CreadoEn, d.Version);

    private static ProcesamientoDocumentalResponse Map(ProcesamientoDocumental p) =>
        new(p.Id, p.DocumentoId, p.Tipo, p.Estado, p.Confianza,
            p.DatosDetectados.RootElement.Clone(), p.Advertencias,
            p.IniciadoEn, p.FinalizadoEn, p.Version);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void ValidarClave(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200)
            throw new DomainException("idempotencia_invalida", "Idempotency-Key es requerido.");
    }

    private static void VerificarVersion(long actual, long esperada)
    {
        if (actual != esperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión está desactualizada.");
    }

    private static void ValidarPagina(long limite, string? cursor, out Guid? id)
    {
        if (limite is < 1 or > 100) throw new DomainException("limite_invalido", "El límite no es válido.");
        id = null;
        if (cursor is not null)
        {
            if (!Guid.TryParse(cursor, out var parsed)) throw new DomainException("cursor_invalido", "El cursor no es válido.");
            id = parsed;
        }
    }

}
