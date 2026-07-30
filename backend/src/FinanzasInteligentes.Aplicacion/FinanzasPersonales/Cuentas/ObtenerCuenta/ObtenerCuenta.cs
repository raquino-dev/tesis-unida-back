using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ObtenerCuenta;

public sealed record ObtenerCuentaQuery(Guid UsuarioId, Guid CuentaId);

public sealed class ObtenerCuentaHandler(IFinanzasRepository finanzas)
{
    public async Task<CuentaResponse> Handle(ObtenerCuentaQuery query, CancellationToken cancellationToken)
    {
        var cuenta = await finanzas.ObtenerCuenta(query.UsuarioId, query.CuentaId, true, cancellationToken)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");

        return cuenta.ToResponse();
    }
}
