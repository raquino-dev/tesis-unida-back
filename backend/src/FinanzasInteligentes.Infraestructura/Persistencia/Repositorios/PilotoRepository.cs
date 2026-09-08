using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Piloto;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class PilotoRepository(FinanzasDbContext db) : IPilotoRepository
{
    public Task<InstrumentoPiloto?> ObtenerInstrumentoActivo(string codigo, CancellationToken ct) =>
        db.InstrumentosPiloto.AsNoTracking()
            .Include(x => x.Preguntas)
            .SingleOrDefaultAsync(x => x.Codigo == codigo && x.Activo, ct);

    public Task<bool> UsuarioRespondio(Guid usuarioId, Guid instrumentoId, CancellationToken ct) =>
        db.RespuestasInstrumentoPiloto.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == usuarioId && x.InstrumentoId == instrumentoId, ct);

    public async Task<DateOnly?> ObtenerFechaFinPiloto(Guid usuarioId, CancellationToken ct)
    {
        var fechas = await db.Database
            .SqlQuery<DateOnly>($"""
                SELECT fecha_fin_planificada AS "Value"
                  FROM piloto.participantes
                 WHERE usuario_id = {usuarioId}
                   AND activo = true
                """)
            .ToListAsync(ct);
        return fechas.Count == 0 ? null : fechas[0];
    }

    public void Agregar(RespuestaInstrumentoPiloto respuesta) => db.RespuestasInstrumentoPiloto.Add(respuesta);
}
