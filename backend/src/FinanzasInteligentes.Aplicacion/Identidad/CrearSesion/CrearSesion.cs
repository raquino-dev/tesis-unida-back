using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Seguridad;

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
    ISeguridadRepository seguridad,
    IPasswordService passwords,
    ITokenService tokens,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<SesionResponse> Handle(CrearSesionCommand command, CancellationToken cancellationToken)
    {
        var correo = command.Correo.Trim().ToLowerInvariant();

        var usuario = await identidad.BuscarUsuarioPorCorreo(correo, cancellationToken);
        if (usuario is null || usuario.Estado != "activo")
            throw new AuthenticationException("credenciales_invalidas", "Las credenciales no son válidas.");
        if (!passwords.Verificar(usuario.HashContrasena, command.Contrasena))
        {
            seguridad.Agregar(EventoSeguridad.Crear(
                usuario.Id, "inicio-sesion-fallido",
                "Se rechazó un inicio de sesión por credenciales inválidas.", false,
                dispositivo: command.Dispositivo?.Nombre));
            await unidadDeTrabajo.GuardarCambios(cancellationToken);
            throw new AuthenticationException(
                "credenciales_invalidas", "Las credenciales no son válidas.");
        }

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
        seguridad.Agregar(EventoSeguridad.Crear(
            usuario.Id, "inicio-sesion",
            "Se inició una sesión correctamente.", true,
            dispositivo: sesion.NombreDispositivo));

        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return new SesionResponse(
            sesion.Id,
            token.AccessToken,
            token.RefreshToken,
            "Bearer",
            ahora.AddSeconds(token.ExpiraEnSegundos),
            refreshExpira,
            false,
            new UsuarioResumenResponse(
                usuario.Id, usuario.Nombre, usuario.Alias, usuario.Correo, usuario.Rol));
    }
}
