using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Mapeos;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;

namespace FinanzasInteligentes.Aplicacion.Identidad.ObtenerPerfil;

public sealed record ObtenerPerfilQuery(Guid UsuarioId);

public sealed class ObtenerPerfilHandler(IIdentidadRepository identidad)
{
    public async Task<UsuarioResponse> Handle(ObtenerPerfilQuery query, CancellationToken cancellationToken)
    {
        var usuario = await identidad.ObtenerUsuario(query.UsuarioId, true, cancellationToken)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");

        return usuario.ToResponse();
    }
}