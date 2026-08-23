using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.Aplicacion.Identidad.Mapeos;

public static class IdentidadMappings
{
    public static UsuarioResponse ToResponse(this Usuario usuario) =>
        new(
            usuario.Id,
            usuario.Correo,
            usuario.Nombre,
            usuario.Alias,
            usuario.Moneda,
            usuario.Idioma,
            usuario.Ubicacion,
            usuario.ZonaHoraria,
            usuario.Estado,
            usuario.Rol,
            false,
            usuario.CreadoEn,
            usuario.Version);
}
