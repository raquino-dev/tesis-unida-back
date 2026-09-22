using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Globalization;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ListarMovimientos;

public sealed record ListarMovimientosQuery(
    Guid UsuarioId, Guid? CuentaId, DateOnly? Desde, DateOnly? Hasta,
    string? Cursor, int Limite, bool SinPaginacion = false);

public sealed class ListarMovimientosHandler(IFinanzasRepository finanzas)
{
    public async Task<Pagina<MovimientoResponse>> Handle(ListarMovimientosQuery query, CancellationToken cancellationToken)
    {
        if (query.SinPaginacion)
        {
            var todos = await finanzas.ListarMovimientos(query.UsuarioId, cancellationToken);
            return new Pagina<MovimientoResponse>(
                todos.Select(x => x.ToResponse()).ToArray(), null);
        }
        if (query.Limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        if (query.Desde > query.Hasta)
            throw new DomainException("rango_fechas_invalido", "El rango de fechas no es válido.");
        DateOnly? cursorFecha = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var parts = query.Cursor.Split('|');
            if (parts.Length != 2 ||
                !DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate) ||
                !Guid.TryParse(parts[1], out var parsedId))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorFecha = parsedDate;
            cursorId = parsedId;
        }
        var movimientos = await finanzas.ListarMovimientosPagina(
            query.UsuarioId, query.CuentaId, query.Desde, query.Hasta,
            cursorFecha, cursorId, query.Limite + 1, cancellationToken);
        var seleccionados = movimientos.Take(query.Limite).ToArray();
        var siguiente = movimientos.Count > query.Limite
            ? $"{seleccionados[^1].Fecha:yyyy-MM-dd}|{seleccionados[^1].Id}"
            : null;
        return new Pagina<MovimientoResponse>(
            seleccionados.Select(x => x.ToResponse()).ToArray(), siguiente);
    }
}
