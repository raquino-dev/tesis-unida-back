using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;
using Microsoft.Extensions.Options;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class DocumentoOptions
{
    public const string SectionName = "Documentos";
    public long TamanoMaximoBytes { get; init; } = 10_485_760;
    public int CantidadMaximaPorUsuario { get; init; } = 500;
}

public sealed class ValidadorDocumento(IOptions<DocumentoOptions> options) : IValidadorDocumento
{
    private readonly DocumentoOptions _options = options.Value;
    public long TamanoMaximoBytes => _options.TamanoMaximoBytes;
    public int CantidadMaximaPorUsuario => _options.CantidadMaximaPorUsuario;

    public void Validar(string tipo, string mime, string nombre, ReadOnlyMemory<byte> contenido)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 180 ||
            Path.GetFileName(nombre) != nombre || nombre.Any(char.IsControl))
            Fallar("nombre_archivo_invalido", "El nombre del archivo no es válido.");
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        var bytes = contenido.Span;
        var valido = tipo switch
        {
            "imagen" when mime == "image/jpeg" && extension is ".jpg" or ".jpeg" =>
                Empieza(bytes, [0xFF, 0xD8, 0xFF]),
            "imagen" when mime == "image/png" && extension == ".png" =>
                Empieza(bytes, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "pdf" when mime == "application/pdf" && extension == ".pdf" =>
                Empieza(bytes, Encoding.ASCII.GetBytes("%PDF-")),
            "xml-sifen" when mime is "application/xml" or "text/xml" && extension == ".xml" =>
                XmlSeguro(bytes),
            _ => false
        };
        if (!valido)
            Fallar("contenido_archivo_invalido", "El contenido, MIME y extensión del archivo no coinciden.");
    }

    private static bool Empieza(ReadOnlySpan<byte> data, ReadOnlySpan<byte> firma) =>
        data.Length >= firma.Length && data[..firma.Length].SequenceEqual(firma);

    private static bool XmlSeguro(ReadOnlySpan<byte> data)
    {
        if (data.Length is 0 or > 10_485_760) return false;
        var muestra = Encoding.UTF8.GetString(data[..Math.Min(data.Length, 4096)]);
        var normalizada = muestra.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return normalizada.StartsWith('<') &&
               !muestra.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
               !muestra.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase);
    }

    private static void Fallar(string codigo, string mensaje) =>
        throw new DomainException(codigo, mensaje);
}
