using FinanzasInteligentes.Api.Contratos.Piloto;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Piloto;

namespace FinanzasInteligentes.Api.Endpoints.Piloto;

public static class InstrumentosPilotoEndpoints
{
    public static IEndpointRouteBuilder MapInstrumentosPiloto(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/instrumentos-piloto").WithTags("Piloto");
        grupo.MapGet("/{codigo}", Obtener);
        grupo.MapPost("/{codigo}/respuestas", Responder);
        return endpoints;
    }

    private static Task<InstrumentoPilotoResponse> Obtener(
        string codigo, InstrumentosPilotoHandler handler, HttpContext context, CancellationToken ct) =>
        handler.Obtener(context.UsuarioId(), codigo, ct);

    private static async Task<IResult> Responder(
        string codigo, EnviarRespuestasInstrumentoPilotoRequest request,
        InstrumentosPilotoHandler handler, HttpContext context, CancellationToken ct)
    {
        var response = await handler.Responder(context.UsuarioId(), codigo,
            request.Respuestas.Select(x => new RespuestaInstrumentoRequest(
                x.PreguntaId, x.ValorEscala, x.ValorTexto)).ToArray(), ct);
        return Results.Ok(response);
    }
}
