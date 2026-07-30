using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.EliminarCategoria;

public sealed record EliminarCategoriaCommand(
    Guid UsuarioId,
    Guid CategoriaId,
    long VersionEsperada);

public sealed class EliminarCategoriaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(EliminarCategoriaCommand command, CancellationToken cancellationToken)
    {
        var categoria = await finanzas.ObtenerCategoria(
            command.UsuarioId, command.CategoriaId, false, cancellationToken)
            ?? throw new NotFoundException("categoria_no_encontrada", "La categoría no existe.");

        if (categoria.UsuarioId != command.UsuarioId)
            throw new ForbiddenException(
                "categoria_predefinida", "Una categoría predefinida no puede eliminarse.");
        if (categoria.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la categoría está desactualizada.");

        categoria.Eliminar();
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
    }
}