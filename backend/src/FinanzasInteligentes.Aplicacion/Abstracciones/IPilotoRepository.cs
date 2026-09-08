using FinanzasInteligentes.Dominio.Piloto;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IPilotoRepository
{
    Task<InstrumentoPiloto?> ObtenerInstrumentoActivo(string codigo, CancellationToken ct);
    Task<bool> UsuarioRespondio(Guid usuarioId, Guid instrumentoId, CancellationToken ct);
    Task<DateOnly?> ObtenerFechaFinPiloto(Guid usuarioId, CancellationToken ct);
    void Agregar(RespuestaInstrumentoPiloto respuesta);
}
