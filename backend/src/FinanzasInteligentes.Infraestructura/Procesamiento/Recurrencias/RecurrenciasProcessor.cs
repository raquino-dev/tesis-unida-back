using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Procesamiento.Recurrencias;

public interface IRecurrenciasProcessor
{
    Task<int> ProcesarVencidas(CancellationToken cancellationToken);
}

public sealed class RecurrenciasProcessor(FinanzasDbContext db) : IRecurrenciasProcessor
{
    public async Task<int> ProcesarVencidas(CancellationToken cancellationToken)
    {
        var limiteConsulta = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var recurrencias = await db.MovimientosRecurrentes
            .Include(x => x.Categorias)
            .Where(x => x.Estado == "activa" && x.EliminadoEn == null &&
                x.ProximaEjecucion <= limiteConsulta)
            .OrderBy(x => x.ProximaEjecucion)
            .Take(25)
            .ToListAsync(cancellationToken);
        var procesadas = 0;

        foreach (var recurrencia in recurrencias)
        {
            var usuario = await db.Usuarios.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == recurrencia.UsuarioId, cancellationToken);
            if (usuario is null)
                continue;
            var hoyLocal = ObtenerFechaLocal(usuario.ZonaHoraria);
            if (recurrencia.ProximaEjecucion > hoyLocal)
                continue;

            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);
            var periodo = recurrencia.ProximaEjecucion;
            var yaGenerado = await db.Movimientos.AnyAsync(
                x => x.RecurrenciaId == recurrencia.Id &&
                    x.PeriodoRecurrencia == periodo,
                cancellationToken);
            if (!yaGenerado)
            {
                var cuenta = await db.Cuentas.SingleOrDefaultAsync(
                    x => x.Id == recurrencia.CuentaId &&
                        x.UsuarioId == recurrencia.UsuarioId &&
                        x.EliminadoEn == null,
                    cancellationToken);
                if (cuenta is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var movimiento = Movimiento.Crear(
                    recurrencia.UsuarioId, recurrencia.CuentaId,
                    recurrencia.Tipo, recurrencia.Monto, recurrencia.Descripcion,
                    periodo, "recurrencia", recurrencia.Id, periodo);
                movimiento.AsignarCategoriasIniciales(recurrencia.Categorias.ToArray());
                cuenta.AplicarMovimiento(recurrencia.Tipo, recurrencia.Monto);
                db.Movimientos.Add(movimiento);
                db.EventosOutbox.Add(EventoOutbox.Crear(
                    "movimiento-recurrente.ejecutado", "movimiento-recurrente",
                    recurrencia.Id,
                    new { RecurrenciaId = recurrencia.Id, MovimientoId = movimiento.Id, Periodo = periodo },
                    null));
            }

            recurrencia.RegistrarEjecucion(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            procesadas++;
        }

        return procesadas;
    }

    private static DateOnly ObtenerFechaLocal(string zonaHoraria)
    {
        try
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zona).DateTime);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
        catch (InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}