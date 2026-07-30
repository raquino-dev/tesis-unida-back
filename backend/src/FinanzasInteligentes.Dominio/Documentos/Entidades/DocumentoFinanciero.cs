using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Text.Json;

namespace FinanzasInteligentes.Dominio.Documentos;

public sealed class DocumentoFinanciero : MutableEntity
{
    private DocumentoFinanciero()
    { }

    public Guid UsuarioId { get; private set; }
    public string Ambito { get; private set; } = "privado";
    public Guid? GrupoFamiliarId { get; private set; }
    public string ClaveObjeto { get; private set; } = string.Empty;
    public string NombreOriginal { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long TamanoBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string HashIdempotencia { get; private set; } = string.Empty;
    public string EstadoArchivo { get; private set; } = "disponible";
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static DocumentoFinanciero Crear(
        Guid id, Guid usuarioId, string ambito, Guid? grupoId, string claveObjeto,
        string nombre, string tipo, string mime, long tamano, string sha256,
        string hashIdempotencia)
    {
        if (tipo is not ("imagen" or "pdf" or "xml-sifen"))
            throw new DomainException("tipo_documento_invalido", "El tipo de documento no es válido.");
        if ((ambito == "privado" && grupoId is not null) ||
            (ambito == "familiar" && grupoId is null) ||
            ambito is not ("privado" or "familiar"))
            throw new DomainException("ambito_invalido", "El propietario del documento no es válido.");
        if (tamano is <= 0 or > 10_485_760)
            throw new DomainException("tamano_invalido", "El archivo debe tener entre 1 byte y 10 MiB.");
        if (sha256.Length != 64)
            throw new DomainException("hash_invalido", "El SHA-256 no es válido.");
        return new()
        {
            Id = id,
            UsuarioId = usuarioId,
            Ambito = ambito,
            GrupoFamiliarId = grupoId,
            ClaveObjeto = claveObjeto,
            NombreOriginal = Path.GetFileName(nombre),
            Tipo = tipo,
            MimeType = mime,
            TamanoBytes = tamano,
            Sha256 = sha256.ToLowerInvariant(),
            HashIdempotencia = hashIdempotencia
        };
    }

    public void Eliminar()
    {
        if (EliminadoEn is not null) return;
        EstadoArchivo = "eliminado";
        EliminadoEn = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class ProcesamientoDocumental : MutableEntity
{
    private ProcesamientoDocumental()
    { }

    public Guid DocumentoId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "pendiente";
    public string Proveedor { get; private set; } = "interno";
    public string VersionModelo { get; private set; } = "documentos-v1";
    public double? Confianza { get; private set; }
    public JsonDocument DatosDetectados { get; private set; } = JsonDocument.Parse("{}");
    public string[] Advertencias { get; private set; } = [];
    public DateTimeOffset? IniciadoEn { get; private set; }
    public DateTimeOffset? FinalizadoEn { get; private set; }
    public string HashIdempotencia { get; private set; } = string.Empty;

    public static ProcesamientoDocumental Crear(Guid documentoId, string tipo, string hash)
    {
        if (tipo is not ("ocr" or "sifen"))
            throw new DomainException("tipo_procesamiento_invalido", "El procesamiento debe ser ocr o sifen.");
        return new() { DocumentoId = documentoId, Tipo = tipo, HashIdempotencia = hash };
    }

    public void Iniciar()
    { Estado = "procesando"; IniciadoEn = DateTimeOffset.UtcNow; Touch(); }

    public void Completar(object datos, double confianza, params string[] advertencias)
    {
        DatosDetectados = JsonSerializer.SerializeToDocument(datos);
        Confianza = Math.Clamp(confianza, 0, 1);
        Advertencias = advertencias;
        Estado = advertencias.Length == 0 ? "completado" : "incompleto";
        FinalizadoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Corregir(JsonDocument datos)
    {
        if (Estado is "pendiente" or "procesando")
            throw new DomainException("procesamiento_activo", "El procesamiento todavía está activo.");
        if (datos.RootElement.ValueKind != JsonValueKind.Object ||
            !datos.RootElement.EnumerateObject().Any())
            throw new DomainException("datos_invalidos", "Debe proporcionar datos detectados.");
        DatosDetectados = JsonDocument.Parse(datos.RootElement.GetRawText());
        Advertencias = [];
        Estado = "completado";
        Touch();
    }
}