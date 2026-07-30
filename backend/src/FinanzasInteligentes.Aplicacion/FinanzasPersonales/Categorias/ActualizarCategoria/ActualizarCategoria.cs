using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ActualizarCategoria;

public sealed record ActualizarCategoriaCommand(
    Guid UsuarioId,
    Guid CategoriaId,
    long VersionEsperada,
    string? Nombre,
    string? Tipo,
    string? Icono,
    string? Color);

public sealed class ActualizarCategoriaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CategoriaResponse> Handle(
        ActualizarCategoriaCommand command,
        CancellationToken cancellationToken)
    {
        var categoria = await finanzas.ObtenerCategoria(
            command.UsuarioId, command.CategoriaId, false, cancellationToken)
            ?? throw new NotFoundException("categoria_no_encontrada", "La categoría no existe.");

        if (categoria.UsuarioId != command.UsuarioId)
            throw new ForbiddenException(
                "categoria_predefinida", "Una categoría predefinida no puede modificarse.");
        if (categoria.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la categoría está desactualizada.");

        categoria.Actualizar(command.Nombre, command.Tipo, command.Icono, command.Color);

        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return categoria.ToResponse();
    }
}