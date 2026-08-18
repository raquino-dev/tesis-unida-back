using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class SincronizacionRepository(FinanzasDbContext db)
    : ISincronizacionRepository
{
    public async Task<PaginaCambiosSincronizacion> Listar(
        Guid usuarioId,
        long desde,
        int limite,
        CancellationToken cancellationToken)
    {
        var filas = await db.CambiosSincronizacion
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Secuencia > desde)
            .OrderBy(x => x.Secuencia)
            .Take(limite + 1)
            .ToListAsync(cancellationToken);
        var hayMas = filas.Count > limite;
        if (hayMas) filas.RemoveAt(filas.Count - 1);
        // El cursor solamente puede avanzar hasta la última fila entregada.
        // Usar el máximo global omitiría páginas pendientes y cambios de otros
        // usuarios producen huecos válidos en la secuencia.
        var siguienteCursor = filas.Count == 0 ? desde : filas[^1].Secuencia;
        return new(
            filas.Select(x => new CambioSincronizacionLectura(
                x.Secuencia, x.TipoEntidad, x.EntidadId, x.Operacion,
                x.Version, x.OcurridoEn)).ToArray(),
            siguienteCursor,
            hayMas);
    }
}
