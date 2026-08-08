namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class ArchivoStorageOptions
{
    public const string SectionName = "Archivos";

    public string Ruta { get; init; } = "private-storage";
    public string PublicBaseUrl { get; init; } = "http://localhost:5000";
    public string SigningKey { get; init; } = string.Empty;
}

public sealed class S3StorageOptions
{
    public const string SectionName = "S3";

    public bool Habilitado { get; init; }
    public string Region { get; init; } = "us-east-1";
    public string Bucket { get; init; } = string.Empty;
    public string Prefijo { get; init; } = "piloto";
}

public sealed class TextractOptions
{
    public const string SectionName = "Textract";

    public bool Habilitado { get; init; }
    public string Region { get; init; } = "us-east-1";
}
