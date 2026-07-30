using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.Identidad.Administracion;

public sealed record ListarUsuariosAdministracionQuery(string? Estado, string? Busqueda, int Limite = 50);
public sealed record CambiarEstadoUsuarioCommand(
    Guid AdministradorId, Guid UsuarioId, string Estado, string? Motivo, string CorrelationId);
public sealed record CambiarRolUsuarioCommand(
    Guid AdministradorId, Guid UsuarioId, string Rol, string? Motivo, string CorrelationId);

public sealed record UsuarioAdministracionResponse(
    Guid Id, string Correo, string Nombre, string Estado, string Rol,
    DateTimeOffset CreadoEn, DateTimeOffset ActualizadoEn, long Version);

public sealed class ListarUsuariosAdministracionHandler(IIdentidadRepository identidad)
{
    public async Task<IReadOnlyCollection<UsuarioAdministracionResponse>> Handle(
        ListarUsuariosAdministracionQuery query, CancellationToken cancellationToken) =>
        (await identidad.ListarUsuarios(
            query.Estado?.Trim().ToLowerInvariant(), query.Busqueda, query.Limite, cancellationToken))
        .Select(Mapear).ToList();

    internal static UsuarioAdministracionResponse Mapear(Dominio.Identidad.Usuario usuario) =>
        new(usuario.Id, usuario.Correo, usuario.Nombre, usuario.Estado, usuario.Rol,
            usuario.CreadoEn, usuario.ActualizadoEn, usuario.Version);
}

public sealed class CambiarEstadoUsuarioHandler(
    IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<UsuarioAdministracionResponse> Handle(
        CambiarEstadoUsuarioCommand command, CancellationToken cancellationToken)
    {
        if (command.AdministradorId == command.UsuarioId && command.Estado != "activo")
            throw new ForbiddenException(
                "autodesactivacion_no_permitida", "Un administrador no puede desactivar su propia cuenta.");

        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        usuario.CambiarEstado(command.Estado.Trim().ToLowerInvariant());
        if (usuario.Estado != "activo")
            await identidad.RevocarSesiones(usuario.Id, cancellationToken);
        identidad.Agregar(EventoOutbox.Crear(
            "usuario.estado-cambiado", "usuario", usuario.Id,
            new { usuario.Id, usuario.Estado, command.Motivo, AdministradorId = command.AdministradorId },
            command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return ListarUsuariosAdministracionHandler.Mapear(usuario);
    }
}

public sealed class CambiarRolUsuarioHandler(
    IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<UsuarioAdministracionResponse> Handle(
        CambiarRolUsuarioCommand command, CancellationToken cancellationToken)
    {
        if (command.AdministradorId == command.UsuarioId && command.Rol != "administrador")
            throw new ForbiddenException(
                "auto_degradacion_no_permitida", "Un administrador no puede retirar su propio rol.");

        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        usuario.CambiarRol(command.Rol.Trim().ToLowerInvariant());
        await identidad.RevocarSesiones(usuario.Id, cancellationToken);
        identidad.Agregar(EventoOutbox.Crear(
            "usuario.rol-cambiado", "usuario", usuario.Id,
            new { usuario.Id, usuario.Rol, command.Motivo, AdministradorId = command.AdministradorId },
            command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return ListarUsuariosAdministracionHandler.Mapear(usuario);
    }
}
