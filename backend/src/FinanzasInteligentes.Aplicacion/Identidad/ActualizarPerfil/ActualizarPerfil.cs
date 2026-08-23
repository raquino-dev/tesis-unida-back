using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Mapeos;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.Aplicacion.Identidad.ActualizarPerfil;

public sealed record ActualizarPerfilCommand(
    Guid UsuarioId, long VersionEsperada, string? Nombre, string? Alias, string? Idioma,
    string? Ubicacion, string? ZonaHoraria);

public sealed class ActualizarPerfilHandler(IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<UsuarioResponse> Handle(ActualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        if (usuario.Version != command.VersionEsperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión del perfil está desactualizada.");
        var alias = command.Alias is null ? null : Usuario.NormalizarAlias(command.Alias);
        if (alias is not null &&
            await identidad.ExisteAlias(alias, command.UsuarioId, cancellationToken))
            throw new ConflictException("alias_duplicado", "El alias ya está en uso.");
        usuario.ActualizarPerfil(
            command.Nombre, command.Idioma, command.Ubicacion, command.ZonaHoraria, alias);
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return usuario.ToResponse();
    }
}
