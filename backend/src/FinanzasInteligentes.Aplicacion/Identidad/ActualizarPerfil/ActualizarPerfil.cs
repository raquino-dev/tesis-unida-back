using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Mapeos;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;

namespace FinanzasInteligentes.Aplicacion.Identidad.ActualizarPerfil;

public sealed record ActualizarPerfilCommand(
    Guid UsuarioId, long VersionEsperada, string? Nombre, string? Idioma,
    string? Ubicacion, string? ZonaHoraria);

public sealed class ActualizarPerfilHandler(IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<UsuarioResponse> Handle(ActualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        if (usuario.Version != command.VersionEsperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión del perfil está desactualizada.");
        usuario.ActualizarPerfil(command.Nombre, command.Idioma, command.Ubicacion, command.ZonaHoraria);
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return usuario.ToResponse();
    }
}