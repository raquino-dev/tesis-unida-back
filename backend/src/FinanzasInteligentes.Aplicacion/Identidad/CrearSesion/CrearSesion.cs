using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.Aplicacion.Identidad.CrearSesion;

public sealed record CrearSesionCommand(
    string Correo,
    string Contrasena,
    DispositivoSesion? Dispositivo = null,
    bool RecordarDispositivo = false);

public sealed record DispositivoSesion(
    string Identificador,
    string Nombre,
    string Plataforma,
    string VersionSistema,
    string VersionAplicacion);

public sealed class CrearSesionHandler(
    IIdentidadRepository identidad,
    IPasswordService passwords,
    ITokenService tokens,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<SesionResponse> Handle(CrearSesionCommand command, CancellationToken cancellationToken)
    {
        var correo = command.Correo.Trim().ToLowerInvariant();

        var usuario = await identidad.BuscarUsuarioPorCorreo(correo, cancellationToken);
        if (usuario is null || usuario.Estado != "activo" ||
            !passwords.Verificar(usuario.HashContrasena, command.Contrasena))
            throw new AuthenticationException("credenciales_invalidas", "Las credenciales no son válidas.");

        var sesionId = Guid.CreateVersion7();
        var token = tokens.Emitir(usuario, sesionId);
        var ahora = DateTimeOffset.UtcNow;
        var refreshExpira = token.RefreshTokenExpiraEn;
        var sesion = Sesion.Crear(
            sesionId,
            usuario.Id,
            token.HashRefreshToken,
            refreshExpira,
            command.Dispositivo?.Identificador ?? "desconocido",
            command.Dispositivo?.Nombre ?? "Dispositivo desconocido",
            command.Dispositivo?.Plataforma ?? "desconocida");
        identidad.Agregar(sesion);

        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return new SesionResponse(
            sesion.Id,
            token.AccessToken,
            token.RefreshToken,
            "Bearer",
            ahora.AddSeconds(token.ExpiraEnSegundos),
            refreshExpira,
            false,
            new UsuarioResumenResponse(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol));
    }
}
