using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.CrearCategoria;

public sealed record CrearCategoriaCommand(
    Guid UsuarioId,
    string Nombre,
    string Tipo,
    string? Icono = null,
    string? Color = null);

public sealed class CrearCategoriaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CategoriaResponse> Handle(CrearCategoriaCommand command, CancellationToken cancellationToken)
    {
        var categoria = Categoria.Crear(
            command.UsuarioId, command.Nombre, command.Tipo, command.Icono, command.Color);

        finanzas.Agregar(categoria);

        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return categoria.ToResponse();
    }
}
