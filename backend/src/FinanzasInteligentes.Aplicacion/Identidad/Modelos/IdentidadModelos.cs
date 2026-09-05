namespace FinanzasInteligentes.Aplicacion.Identidad.Modelos;

public sealed record UsuarioResponse(
    Guid Id,
    string Correo,
    string Nombre,
    string Alias,
    string Moneda,
    string Idioma,
    string Ubicacion,
    string ZonaHoraria,
    string Estado,
    string Rol,
    bool CorreoVerificado,
    DateTimeOffset CreadoEn,
    long Version);

public sealed record SesionResponse(
    Guid Id,
    string AccessToken,
    string RefreshToken,
    string TipoToken,
    DateTimeOffset AccessTokenExpiraEn,
    DateTimeOffset RefreshTokenExpiraEn,
    bool RequiereOtp,
    UsuarioResumenResponse Usuario);

public sealed record UsuarioResumenResponse(
    Guid Id, string Nombre, string Alias, string Correo, string Rol = "usuario");

public sealed record PreferenciasResponse(
    string Tema,
    string Idioma,
    string Moneda,
    string ZonaHoraria,
    bool NotificacionesPush,
    bool ResumenSemanal,
    long Version);

public sealed record DispositivoSesionResponse(Guid Id, string Nombre, string Plataforma);

public sealed record SesionActivaResponse(
    Guid Id,
    DispositivoSesionResponse Dispositivo,
    DateTimeOffset EmitidaEn,
    DateTimeOffset ExpiraEn,
    bool Actual,
    DateTimeOffset? RevocadaEn);

public sealed record PaginacionResponse(string? CursorSiguiente);

public sealed record PaginaSesionActivaResponse(
    IReadOnlyCollection<SesionActivaResponse> Datos,
    PaginacionResponse Paginacion);

public sealed record DesafioOtpResponse(
    Guid Id, string DestinoEnmascarado, DateTimeOffset ExpiraEn, long IntentosRestantes);

public sealed record VerificacionSeguridadResponse(
    Guid Id, bool Valida, DateTimeOffset ExpiraEn);

public sealed record SolicitudRecuperacionResponse(Guid Id, DateTimeOffset ExpiraEn);

public sealed record ProcesoAsyncResponse(
    Guid Id,
    string Estado,
    DateTimeOffset CreadoEn,
    DateTimeOffset? CompletadoEn,
    string? ErrorCodigo,
    string UrlEstado);

public sealed record ProcesoAsyncResultado(ProcesoAsyncResponse Response, long Version);
