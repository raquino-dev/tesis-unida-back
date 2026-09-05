namespace FinanzasInteligentes.Api.Contratos.Identidad;

public sealed record ActualizarPerfilRequest(
    string? Nombre = null,
    string? Alias = null,
    string? Idioma = null,
    string? Ubicacion = null,
    string? ZonaHoraria = null);

public sealed record ActualizarPreferenciasRequest(
    string Tema,
    string Idioma,
    bool NotificacionesPush,
    bool ResumenSemanal);

public sealed record RenovarSesionRequest(
    string RefreshToken,
    string IdentificadorDispositivo);

public sealed record DesafioOtpRequest(string Motivo, string Canal);
public sealed record VerificacionOtpRequest(Guid DesafioId, string Codigo);
public sealed record RecuperacionContrasenaRequest(string Correo);
public sealed record RestablecimientoContrasenaRequest(
    Guid RecuperacionId, string Codigo, string NuevaContrasena);
public sealed record CambiarContrasenaRequest(
    string ContrasenaActual, string NuevaContrasena, Guid VerificacionOtpId);

public sealed record EliminarPerfilRequest(string Contrasena, Guid VerificacionOtpId);
public sealed record CambiarEstadoUsuarioRequest(string Estado, string? Motivo = null);
public sealed record CambiarRolUsuarioRequest(string Rol, string? Motivo = null);
public sealed record AceptarConsentimientoRequest(string VersionPolitica, string Finalidad);
