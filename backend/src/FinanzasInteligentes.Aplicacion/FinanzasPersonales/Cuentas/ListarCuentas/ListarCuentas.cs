using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ListarCuentas;

public sealed record ListarCuentasQuery(Guid UsuarioId);

public sealed class ListarCuentasHandler(IFinanzasRepository finanzas)
{
    public async Task<Pagina<CuentaResponse>> Handle(ListarCuentasQuery query, CancellationToken cancellationToken)
    {
        var cuentas = await finanzas.ListarCuentas(query.UsuarioId, cancellationToken);
        return new Pagina<CuentaResponse>(cuentas.Select(x => x.ToResponse()).ToList(), null);
    }
}
