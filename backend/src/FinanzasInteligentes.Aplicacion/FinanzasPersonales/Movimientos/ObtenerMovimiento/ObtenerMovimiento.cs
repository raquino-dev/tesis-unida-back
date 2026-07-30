using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ObtenerMovimiento;

public sealed record ObtenerMovimientoQuery(Guid UsuarioId, Guid MovimientoId);

public sealed class ObtenerMovimientoHandler(IFinanzasRepository finanzas)
{
    public async Task<MovimientoResponse> Handle(ObtenerMovimientoQuery query, CancellationToken cancellationToken)
    {
        var movimiento = await finanzas.ObtenerMovimiento(
            query.UsuarioId, query.MovimientoId, true, cancellationToken)
            ?? throw new NotFoundException("movimiento_no_encontrado", "El movimiento no existe.");

        return movimiento.ToResponse();
    }
}
