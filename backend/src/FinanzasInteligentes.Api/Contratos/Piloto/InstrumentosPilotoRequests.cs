namespace FinanzasInteligentes.Api.Contratos.Piloto;

public sealed record RespuestaInstrumentoPilotoItemRequest(
    Guid PreguntaId, int? ValorEscala, string? ValorTexto);
public sealed record EnviarRespuestasInstrumentoPilotoRequest(
    IReadOnlyCollection<RespuestaInstrumentoPilotoItemRequest> Respuestas);
