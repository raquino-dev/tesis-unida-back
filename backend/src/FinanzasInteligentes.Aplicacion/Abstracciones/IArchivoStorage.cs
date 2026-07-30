namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IArchivoStorage
{
    Task Guardar(string clave, Stream contenido, CancellationToken ct);

    Task<Stream> Abrir(string clave, CancellationToken ct);

    Task Eliminar(string clave, CancellationToken ct);

    string CrearUrlTemporal(string clave, DateTimeOffset expiraEn);

    bool ValidarUrl(string clave, long expiraUnix, string firma);
}