using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.Identidad.EliminarPerfil;

public sealed record SolicitarEliminacionPerfilCommand(
    Guid UsuarioId, string Contrasena, Guid VerificacionOtpId, string CorrelationId);
public sealed record ObtenerEliminacionPerfilQuery(Guid UsuarioId, Guid EliminacionId);

public sealed class SolicitarEliminacionPerfilHandler(
    IIdentidadRepository identidad,
    IPasswordService passwords,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<ProcesoAsyncResultado> Handle(
        SolicitarEliminacionPerfilCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        if (!passwords.Verificar(usuario.HashContrasena, command.Contrasena))
            throw new ForbiddenException("contrasena_invalida", "La contraseña no es válida.");
        if (await identidad.ExisteEliminacionPerfilPendiente(usuario.Id, cancellationToken))
            throw new ConflictException("eliminacion_pendiente", "Ya existe una eliminación pendiente.");
        if (!await identidad.ConsumirVerificacionOtp(
            usuario.Id, command.VerificacionOtpId, "eliminacion-perfil", cancellationToken))
            throw new ForbiddenException("verificacion_otp_invalida", "La verificación OTP no es válida.");

        var eliminacion = EliminacionPerfil.Crear(usuario.Id);
        identidad.Agregar(eliminacion);
        identidad.Agregar(EventoOutbox.Crear(
            "perfil.eliminacion-solicitada", "eliminacion-perfil", eliminacion.Id,
            new { eliminacion.Id, eliminacion.UsuarioId }, command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);
        return Map(eliminacion);
    }

    internal static ProcesoAsyncResultado Map(EliminacionPerfil eliminacion) =>
        new(new(
            eliminacion.Id, eliminacion.Estado, eliminacion.CreadoEn,
            eliminacion.CompletadoEn, eliminacion.ErrorCodigo,
            $"/api/v1/eliminaciones-perfil/{eliminacion.Id}"), eliminacion.Version);
}

public sealed class ObtenerEliminacionPerfilHandler(IIdentidadRepository identidad)
{
    public async Task<ProcesoAsyncResultado> Handle(
        ObtenerEliminacionPerfilQuery query, CancellationToken cancellationToken)
    {
        var eliminacion = await identidad.ObtenerEliminacionPerfil(
            query.UsuarioId, query.EliminacionId, true, cancellationToken)
            ?? throw new NotFoundException("eliminacion_no_encontrada", "La eliminación no existe.");
        return SolicitarEliminacionPerfilHandler.Map(eliminacion);
    }
}