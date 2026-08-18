using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Sincronizacion;

public sealed record ListarCambiosSincronizacionQuery(
    Guid UsuarioId,
    long Desde,
    int Limite);

public sealed record SincronizacionResponse(
    IReadOnlyCollection<CambioSincronizacionLectura> Cambios,
    long SiguienteCursor,
    bool HayMas);

public sealed class ListarCambiosSincronizacionHandler(
    ISincronizacionRepository repository)
{
    public async Task<SincronizacionResponse> Handle(
        ListarCambiosSincronizacionQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Desde < 0)
            throw new DomainException("cursor_invalido", "El cursor no puede ser negativo.");
        if (query.Limite is < 1 or > 500)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 500.");

        var pagina = await repository.Listar(
            query.UsuarioId, query.Desde, query.Limite, cancellationToken);
        var siguiente = pagina.Cambios.Count > 0
            ? pagina.Cambios.Last().Secuencia
            : Math.Max(query.Desde, pagina.CursorGlobal);
        return new(pagina.Cambios, siguiente, pagina.HayMas);
    }
}
