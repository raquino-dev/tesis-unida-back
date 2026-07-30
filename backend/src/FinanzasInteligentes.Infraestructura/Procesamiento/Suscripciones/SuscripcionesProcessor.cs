using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Dominio.Suscripciones;
using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Procesamiento.Suscripciones;

public interface ISuscripcionesProcessor
{
    Task<int> ProcesarAvisos(CancellationToken ct);
}

public sealed class SuscripcionesProcessor(FinanzasDbContext db) : ISuscripcionesProcessor
{
    public async Task<int> ProcesarAvisos(CancellationToken ct)
    {
        var ahora = DateTimeOffset.UtcNow;
        var candidatas = await db.Suscripciones.AsNoTracking()
            .Where(x => x.FinPeriodoEn >= ahora.AddDays(-2) &&
                        x.FinPeriodoEn <= ahora.AddDays(8) &&
                        x.Estado != "reemplazada")
            .ToListAsync(ct);
        var generados = 0;
        foreach (var suscripcion in candidatas)
        {
            var dias = (int)Math.Ceiling((suscripcion.FinPeriodoEn - ahora).TotalDays);
            var tipo = dias switch
            {
                7 => "vence-7-dias",
                3 => "vence-3-dias",
                1 => "vence-1-dia",
                0 => "vence-hoy",
                < 0 => "vencida",
                _ => null
            };
            if (tipo is null ||
                await db.AvisosSuscripcion.AnyAsync(x =>
                    x.SuscripcionId == suscripcion.Id &&
                    x.Tipo == tipo &&
                    x.PeriodoFinEn == suscripcion.FinPeriodoEn, ct))
                continue;

            db.AvisosSuscripcion.Add(AvisoSuscripcion.Crear(
                suscripcion.UsuarioId, suscripcion.Id, tipo, suscripcion.FinPeriodoEn));
            db.EventosOutbox.Add(EventoOutbox.Crear(
                "suscripcion.aviso-vencimiento", "suscripcion", suscripcion.Id,
                new
                {
                    suscripcion.Id,
                    suscripcion.UsuarioId,
                    Tipo = tipo,
                    suscripcion.FinPeriodoEn
                },
                $"worker-suscripciones-{suscripcion.Id:N}-{tipo}"));
            generados++;
        }

        if (generados > 0) await db.SaveChangesAsync(ct);
        return generados;
    }
}
