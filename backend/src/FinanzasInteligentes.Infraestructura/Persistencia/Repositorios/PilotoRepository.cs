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

    public void Agregar(RespuestaInstrumentoPiloto respuesta) => db.RespuestasInstrumentoPiloto.Add(respuesta);
}
