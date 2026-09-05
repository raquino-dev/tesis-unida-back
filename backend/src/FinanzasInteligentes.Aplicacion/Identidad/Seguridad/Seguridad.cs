using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Dominio.Seguridad;
using System.Security.Cryptography;

namespace FinanzasInteligentes.Aplicacion.Identidad.Seguridad;

public sealed record CrearDesafioOtpCommand(Guid UsuarioId, string Motivo, string Canal, string CorrelationId);
public sealed record VerificarOtpCommand(Guid UsuarioId, Guid DesafioId, string Codigo);
public sealed record SolicitarRecuperacionCommand(string Correo, string CorrelationId);
public sealed record RestablecerContrasenaCommand(string Token, string NuevaContrasena);
public sealed record CambiarContrasenaCommand(
    Guid UsuarioId, string ContrasenaActual, string NuevaContrasena, Guid VerificacionOtpId);

public sealed class CrearDesafioOtpHandler(
    IIdentidadRepository identidad, ISeguridadFlujosConfiguracion configuracion,
    ISeguridadRepository seguridad, IHasherTokenUnSoloUso tokenHasher,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<DesafioOtpResponse> Handle(
        CrearDesafioOtpCommand command, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, true, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(configuracion.OtpMinutos);
        var desafio = DesafioOtp.Crear(
            usuario.Id, command.Motivo, command.Canal,
            tokenHasher.Hash(codigo), usuario.Correo, expiraEn);
        identidad.Agregar(desafio);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuario.Id, "otp-solicitado",
            $"Se solicitó una verificación OTP para {desafio.Motivo}.", true));
        identidad.Agregar(EventoOutbox.Crear(
            "otp.solicitado", "desafio-otp", desafio.Id,
            new { desafio.Id, usuario.Correo, Codigo = codigo, desafio.Motivo, expiraEn },
            command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return new(desafio.Id, Enmascarar(usuario.Correo), expiraEn, desafio.IntentosRestantes);
    }

    private static string Enmascarar(string correo)
    {
        var partes = correo.Split('@', 2);
        var visible = partes[0][..Math.Min(2, partes[0].Length)];
        return $"{visible}***@{partes[1]}";
    }
}

public sealed class VerificarOtpHandler(
    IIdentidadRepository identidad, ISeguridadFlujosConfiguracion configuracion,
    ISeguridadRepository seguridad, IHasherTokenUnSoloUso tokenHasher,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<VerificacionSeguridadResponse> Handle(
        VerificarOtpCommand command, CancellationToken cancellationToken)
    {
        var desafio = await identidad.ObtenerDesafioOtp(
            command.UsuarioId, command.DesafioId, cancellationToken)
            ?? throw new NotFoundException("desafio_otp_no_encontrado", "El desafío OTP no existe.");
        try
        {
            desafio.Verificar(tokenHasher.Hash(command.Codigo));
        }
        catch (DomainException)
        {
            seguridad.Agregar(EventoSeguridad.Crear(
                command.UsuarioId, "otp-fallido",
                "Se rechazó un código OTP.", false));
            await unidadDeTrabajo.GuardarCambios(cancellationToken);
            throw;
        }
        var verificacion = VerificacionOtp.Crear(
            desafio, DateTimeOffset.UtcNow.AddMinutes(configuracion.VerificacionOtpMinutos));
        identidad.Agregar(verificacion);
        seguridad.Agregar(EventoSeguridad.Crear(
            command.UsuarioId, "otp-verificado",
            $"Se verificó un OTP para {desafio.Motivo}.", true));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return new(verificacion.Id, true, verificacion.ExpiraEn);
    }
}

public sealed class SolicitarRecuperacionHandler(
    IIdentidadRepository identidad, ISeguridadFlujosConfiguracion configuracion,
    ISeguridadRepository seguridad, IHasherTokenUnSoloUso tokenHasher,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(SolicitarRecuperacionCommand command, CancellationToken cancellationToken)
    {
        var usuario = await identidad.BuscarUsuarioPorCorreo(
            command.Correo.Trim().ToLowerInvariant(), cancellationToken);
        if (usuario is null) return;

        // Un código hexadecimal conserva 128 bits de entropía, evita caracteres
        // ambiguos de Base64 y también puede copiarse manualmente desde el correo.
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(
            configuracion.RecuperacionContrasenaMinutos);
        var recuperacion = RecuperacionContrasena.Crear(
            usuario.Id, tokenHasher.Hash(token), expiraEn);
        identidad.Agregar(recuperacion);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuario.Id, "recuperacion-contrasena-solicitada",
            "Se solicitó restablecer la contraseña.", true));
        identidad.Agregar(EventoOutbox.Crear(
            "contrasena.recuperacion-solicitada", "usuario", usuario.Id,
            new { usuario.Correo, Token = token, expiraEn }, command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
    }
}

public sealed class RestablecerContrasenaHandler(
    IIdentidadRepository identidad, IPasswordService passwords,
    IPoliticaContrasena politicaContrasena, ISeguridadRepository seguridad,
    IHasherTokenUnSoloUso tokenHasher,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(RestablecerContrasenaCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);
        var recuperacion = await identidad.ConsumirRecuperacion(
            tokenHasher.Hash(command.Token), cancellationToken)
            ?? throw new DomainException("token_recuperacion_invalido", "El token no es válido o expiró.");
        var usuario = await identidad.ObtenerUsuario(recuperacion.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        politicaContrasena.Validar(command.NuevaContrasena, usuario.Correo, usuario.Nombre);
        usuario.CambiarContrasena(passwords.Hash(command.NuevaContrasena));
        await identidad.RevocarSesiones(usuario.Id, cancellationToken);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuario.Id, "contrasena-restablecida",
            "Se restableció la contraseña y se revocaron las sesiones.", true));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);
    }
}

public sealed class CambiarContrasenaHandler(
    IIdentidadRepository identidad, IPasswordService passwords,
    IPoliticaContrasena politicaContrasena, ISeguridadRepository seguridad,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(CambiarContrasenaCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        politicaContrasena.Validar(command.NuevaContrasena, usuario.Correo, usuario.Nombre);
        if (!passwords.Verificar(usuario.HashContrasena, command.ContrasenaActual))
            throw new ForbiddenException("contrasena_actual_invalida", "La contraseña actual no es válida.");
        if (!await identidad.ConsumirVerificacionOtp(
            usuario.Id, command.VerificacionOtpId, "cambio-contrasena", cancellationToken))
            throw new ForbiddenException("verificacion_otp_invalida", "La verificación OTP no es válida.");
        usuario.CambiarContrasena(passwords.Hash(command.NuevaContrasena));
        await identidad.RevocarSesiones(usuario.Id, cancellationToken);
        seguridad.Agregar(EventoSeguridad.Crear(
            usuario.Id, "contrasena-cambiada",
            "Se cambió la contraseña y se revocaron las sesiones.", true));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);
    }
}
