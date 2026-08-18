namespace FinanzasInteligentes.Infraestructura.Persistencia;

public sealed class CambioSincronizacion
{
    private CambioSincronizacion()
    { }

    public long Secuencia { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string TipoEntidad { get; private set; } = string.Empty;
    public Guid EntidadId { get; private set; }
    public string Operacion { get; private set; } = "actualizado";
    public long Version { get; private set; }
    public DateTimeOffset OcurridoEn { get; private set; }

    public static CambioSincronizacion Crear(
        Guid usuarioId,
        string tipoEntidad,
        Guid entidadId,
        string operacion,
        long version) => new()
        {
            UsuarioId = usuarioId,
            TipoEntidad = tipoEntidad,
            EntidadId = entidadId,
            Operacion = operacion,
            Version = version,
            OcurridoEn = DateTimeOffset.UtcNow
        };
}
