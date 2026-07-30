namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class ArchivoStorageOptions
{
    public const string SectionName = "Archivos";

    public string Ruta { get; init; } = "private-storage";
    public string PublicBaseUrl { get; init; } = "http://localhost:5000";
    public string SigningKey { get; init; } = string.Empty;
}
