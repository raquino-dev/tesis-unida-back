using FinanzasInteligentes.Dominio.Seguridad;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface ISeguridadRepository
{
    Task<Dispositivo?> ObtenerDispositivo(Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct);

    Task<Dispositivo?> ObtenerDispositivoPorInstalacion(Guid usuarioId, string identificador, CancellationToken ct);

    Task<IReadOnlyCollection<EventoSeguridad>> ListarEventosSeguridad(Guid usuarioId, CancellationToken ct);

    Task<IReadOnlyCollection<EventoAuditoria>> ListarEventosAuditoria(Guid usuarioId, CancellationToken ct);

    Task RevocarSesionesDeDispositivo(Guid usuarioId, string identificador, CancellationToken ct);

    void Agregar(Dispositivo entity);

    void Agregar(EventoSeguridad entity);

    void Agregar(EventoAuditoria entity);
}

public interface IProtectorTokenPush
{
    string Proteger(string token);
    string Desproteger(string tokenProtegido);
}
