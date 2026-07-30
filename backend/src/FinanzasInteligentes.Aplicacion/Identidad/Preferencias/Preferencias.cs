using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;

namespace FinanzasInteligentes.Aplicacion.Identidad.Preferencias;

public sealed record ObtenerPreferenciasQuery(Guid UsuarioId);
public sealed record ActualizarPreferenciasCommand(
    Guid UsuarioId, long VersionEsperada, string Tema, string Idioma,
    bool NotificacionesPush, bool ResumenSemanal);

public sealed class ObtenerPreferenciasHandler(IIdentidadRepository identidad)
{
    public async Task<PreferenciasResponse> Handle(ObtenerPreferenciasQuery query, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(query.UsuarioId, true, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        return Map(usuario);
    }

    internal static PreferenciasResponse Map(FinanzasInteligentes.Dominio.Identidad.Usuario usuario) =>
        new(usuario.Preferencias.Tema, usuario.Idioma, usuario.Moneda, usuario.ZonaHoraria,
            usuario.Preferencias.NotificacionesPush, usuario.Preferencias.ResumenSemanal,
            usuario.Preferencias.Version);
}

public sealed class ActualizarPreferenciasHandler(
    IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PreferenciasResponse> Handle(
        ActualizarPreferenciasCommand command, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(command.UsuarioId, false, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        if (usuario.Preferencias.Version != command.VersionEsperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión de las preferencias está desactualizada.");
        usuario.ActualizarPerfil(null, command.Idioma, null, null);
        usuario.Preferencias.Actualizar(command.Tema, command.NotificacionesPush, command.ResumenSemanal);
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return ObtenerPreferenciasHandler.Map(usuario);
    }
}