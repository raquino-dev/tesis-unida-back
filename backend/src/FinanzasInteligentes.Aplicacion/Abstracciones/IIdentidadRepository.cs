using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IIdentidadRepository
{
    Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken);

    Task<Usuario?> BuscarUsuarioPorCorreo(string correo, CancellationToken cancellationToken);

    Task<Usuario?> ObtenerUsuario(Guid usuarioId, bool soloLectura, CancellationToken cancellationToken);
    Task<bool> UsuarioEstaActivo(Guid usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Usuario>> ListarUsuarios(
        string? estado, string? busqueda, int limite, CancellationToken cancellationToken);

    Task<PoliticaPrivacidad?> ObtenerPoliticaVigente(CancellationToken cancellationToken);
    Task<PoliticaPrivacidad?> ObtenerPoliticaPorVersion(
        string version, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsentimientoPrivacidad>> ListarConsentimientos(
        Guid usuarioId, CancellationToken cancellationToken);
    Task<ConsentimientoPrivacidad?> ObtenerConsentimiento(
        Guid usuarioId, Guid consentimientoId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Sesion>> ListarSesiones(Guid usuarioId, CancellationToken cancellationToken);

    Task<Sesion?> ObtenerSesion(Guid usuarioId, Guid sesionId, CancellationToken cancellationToken);

    Task<Sesion?> ConsumirSesionPorRefreshToken(
        string hashRefreshToken,
        string identificadorDispositivo,
        CancellationToken cancellationToken);

    Task<DesafioOtp?> ObtenerDesafioOtp(Guid usuarioId, Guid desafioId, CancellationToken cancellationToken);

    Task<bool> ConsumirVerificacionOtp(
        Guid usuarioId, Guid verificacionId, string motivo, CancellationToken cancellationToken);

    Task<RecuperacionContrasena?> ConsumirRecuperacion(
        string hashToken, CancellationToken cancellationToken);

    Task RevocarSesiones(Guid usuarioId, CancellationToken cancellationToken);

    Task<EliminacionPerfil?> ObtenerEliminacionPerfil(
        Guid usuarioId, Guid eliminacionId, bool soloLectura, CancellationToken cancellationToken);

    Task<bool> ExisteEliminacionPerfilPendiente(Guid usuarioId, CancellationToken cancellationToken);

    void Agregar(Usuario usuario);

    void Agregar(Sesion sesion);

    void Agregar(DesafioOtp desafio);

    void Agregar(VerificacionOtp verificacion);

    void Agregar(RecuperacionContrasena recuperacion);
    void Agregar(ConsentimientoPrivacidad consentimiento);

    void Agregar(EliminacionPerfil eliminacion);

    void Agregar(EventoOutbox evento);
}
