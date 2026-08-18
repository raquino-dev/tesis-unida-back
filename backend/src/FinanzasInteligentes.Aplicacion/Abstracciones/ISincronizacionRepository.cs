namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public sealed record CambioSincronizacionLectura(
    long Secuencia,
    string TipoEntidad,
    Guid EntidadId,
    string Operacion,
    long Version,
    DateTimeOffset OcurridoEn);

public sealed record PaginaCambiosSincronizacion(
    IReadOnlyCollection<CambioSincronizacionLectura> Cambios,
    long CursorGlobal,
    bool HayMas);

public interface ISincronizacionRepository
{
    Task<PaginaCambiosSincronizacion> Listar(
        Guid usuarioId,
        long desde,
        int limite,
        CancellationToken cancellationToken);
}
