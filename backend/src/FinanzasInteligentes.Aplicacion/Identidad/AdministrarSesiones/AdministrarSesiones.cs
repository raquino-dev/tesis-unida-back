using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.Identidad.AdministrarSesiones;

public sealed record ListarSesionesQuery(Guid UsuarioId, Guid SesionActualId);
public sealed record RevocarSesionCommand(Guid UsuarioId, Guid SesionId);
public sealed record RenovarSesionCommand(string RefreshToken, string IdentificadorDispositivo);

public sealed class ListarSesionesHandler(IIdentidadRepository identidad)
{
    public async Task<PaginaSesionActivaResponse> Handle(
        ListarSesionesQuery query, CancellationToken cancellationToken)
    {
        var sesiones = await identidad.ListarSesiones(query.UsuarioId, cancellationToken);
        var datos = sesiones.Select(x => new SesionActivaResponse(
            x.Id,
            new DispositivoSesionResponse(x.Id, x.NombreDispositivo, x.PlataformaDispositivo),
            x.CreadoEn,
            x.ExpiraEn,
            x.Id == query.SesionActualId,
            x.RevocadoEn)).ToList();
        return new(datos, new(null));
    }
}

public sealed class RevocarSesionHandler(
    IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(RevocarSesionCommand command, CancellationToken cancellationToken)
    {
        var sesion = await identidad.ObtenerSesion(
            command.UsuarioId, command.SesionId, cancellationToken)
            ?? throw new NotFoundException("sesion_no_encontrada", "La sesión no existe.");
        sesion.Revocar();
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
    }
}

public sealed class RenovarSesionHandler(
    IIdentidadRepository identidad,
    ITokenService tokens,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<SesionResponse> Handle(
        RenovarSesionCommand command, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(command.RefreshToken)));
        var anterior = await identidad.ConsumirSesionPorRefreshToken(
            hash, command.IdentificadorDispositivo, cancellationToken)
            ?? throw new AuthenticationException("refresh_token_invalido", "El refresh token no es válido.");

        var usuario = await identidad.ObtenerUsuario(anterior.UsuarioId, false, cancellationToken)
            ?? throw new AuthenticationException("usuario_no_encontrado", "El usuario no existe.");
        if (usuario.Estado != "activo")
        {
            await identidad.RevocarSesiones(usuario.Id, cancellationToken);
            throw new AuthenticationException("sesion_invalida", "La sesión no es válida.");
        }

        var nuevaSesionId = Guid.CreateVersion7();
        var token = tokens.Emitir(usuario, nuevaSesionId);
        var ahora = DateTimeOffset.UtcNow;
        var refreshExpira = token.RefreshTokenExpiraEn;
        var nueva = Sesion.Crear(
            nuevaSesionId, usuario.Id, token.HashRefreshToken, refreshExpira,
            anterior.IdentificadorDispositivo, anterior.NombreDispositivo,
            anterior.PlataformaDispositivo, anterior.FamiliaToken);
        identidad.Agregar(nueva);
        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return new(
            nueva.Id, token.AccessToken, token.RefreshToken, "Bearer",
            ahora.AddSeconds(token.ExpiraEnSegundos), refreshExpira, false,
            new(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol));
    }
}
