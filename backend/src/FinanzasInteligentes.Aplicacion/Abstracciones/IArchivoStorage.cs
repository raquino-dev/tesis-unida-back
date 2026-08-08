namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IArchivoStorage
{
    Task Guardar(string clave, Stream contenido, CancellationToken ct);

    Task<Stream> Abrir(string clave, CancellationToken ct);

    Task Eliminar(string clave, CancellationToken ct);

    string CrearUrlTemporal(string clave, DateTimeOffset expiraEn);

    bool ValidarUrl(string clave, long expiraUnix, string firma);
}

public sealed record ResultadoOcrDocumento(
    long? Monto,
    DateOnly? Fecha,
    string? Comercio,
    double Confianza,
    IReadOnlyCollection<string> Advertencias);

public interface IProcesadorOcrDocumento
{
    bool Habilitado { get; }

    Task<ResultadoOcrDocumento> Procesar(
        Stream contenido,
        CancellationToken ct);
}
