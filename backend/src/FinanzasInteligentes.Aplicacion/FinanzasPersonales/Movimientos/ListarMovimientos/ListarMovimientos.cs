using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ListarMovimientos;

public sealed record ListarMovimientosQuery(Guid UsuarioId);

public sealed class ListarMovimientosHandler(IFinanzasRepository finanzas)
{
    public async Task<Pagina<MovimientoResponse>> Handle(ListarMovimientosQuery query, CancellationToken cancellationToken)
    {
        var movimientos = await finanzas.ListarMovimientos(query.UsuarioId, cancellationToken);
        return new Pagina<MovimientoResponse>(movimientos.Select(x => x.ToResponse()).ToList(), null);
    }
}
