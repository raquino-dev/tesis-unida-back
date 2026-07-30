using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Categorias.ListarCategorias;

public sealed record ListarCategoriasQuery(Guid UsuarioId);

public sealed class ListarCategoriasHandler(IFinanzasRepository finanzas)
{
    public async Task<Pagina<CategoriaResponse>> Handle(ListarCategoriasQuery query, CancellationToken cancellationToken)
    {
        var categorias = await finanzas.ListarCategorias(query.UsuarioId, cancellationToken);
        return new Pagina<CategoriaResponse>(categorias.Select(x => x.ToResponse()).ToList(), null);
    }
}
