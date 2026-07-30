using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ObtenerCategoria;

public sealed record ObtenerCategoriaQuery(Guid UsuarioId, Guid CategoriaId);

public sealed class ObtenerCategoriaHandler(IFinanzasRepository finanzas)
{
    public async Task<CategoriaResponse> Handle(ObtenerCategoriaQuery query, CancellationToken cancellationToken)
    {
        var categoria = await finanzas.ObtenerCategoria(
            query.UsuarioId, query.CategoriaId, true, cancellationToken)
            ?? throw new NotFoundException("categoria_no_encontrada", "La categoría no existe.");

        return categoria.ToResponse();
    }
}
