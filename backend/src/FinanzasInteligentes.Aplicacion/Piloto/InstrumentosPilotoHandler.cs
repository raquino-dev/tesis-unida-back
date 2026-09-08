using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Piloto;

namespace FinanzasInteligentes.Aplicacion.Piloto;

public sealed record PreguntaInstrumentoResponse(
    Guid Id, int Orden, string Tipo, string Texto, bool Requerida, int? Minimo, int? Maximo);
public sealed record InstrumentoPilotoResponse(
    string Codigo, string Version, string Titulo, string Descripcion,
    bool Respondido, bool Habilitado, DateOnly? HabilitadoDesde,
    IReadOnlyCollection<PreguntaInstrumentoResponse> Preguntas);
public sealed record RespuestaInstrumentoRequest(Guid PreguntaId, int? ValorEscala, string? ValorTexto);

public sealed class InstrumentosPilotoHandler(IPilotoRepository piloto, IUnidadDeTrabajo unidadDeTrabajo)
{
    private static readonly TimeZoneInfo ZonaHorariaPiloto =
        TimeZoneInfo.FindSystemTimeZoneById("America/Asuncion");

    private static readonly HashSet<string> CodigosPermitidos = new(StringComparer.Ordinal)
    {
        "preuso",
        "preuso-complementario",
        "postuso"
    };

    public async Task<InstrumentoPilotoResponse> Obtener(Guid usuarioId, string codigo, CancellationToken ct)
    {
        var instrumento = await ObtenerActivo(codigo, ct);
        var respondido = await piloto.UsuarioRespondio(usuarioId, instrumento.Id, ct);
        var elegibilidad = await ObtenerElegibilidad(usuarioId, codigo, ct);
        return Map(instrumento, respondido, elegibilidad.Habilitado, elegibilidad.HabilitadoDesde);
    }

    public async Task<InstrumentoPilotoResponse> Responder(
        Guid usuarioId, string codigo, IReadOnlyCollection<RespuestaInstrumentoRequest> respuestas, CancellationToken ct)
    {
        var instrumento = await ObtenerActivo(codigo, ct);
        if (await piloto.UsuarioRespondio(usuarioId, instrumento.Id, ct))
            throw new ConflictException("instrumento_ya_respondido", "Este instrumento ya fue respondido.");

        var elegibilidad = await ObtenerElegibilidad(usuarioId, codigo, ct);
        if (!elegibilidad.Habilitado)
        {
            if (elegibilidad.HabilitadoDesde is null)
                throw new ConflictException(
                    "participante_piloto_no_configurado",
                    "La ventana de observación del participante no está configurada.");
            throw new ConflictException(
                "postuso_no_habilitado",
                $"El cuestionario final estará disponible desde el {elegibilidad.HabilitadoDesde:dd/MM/yyyy}.");
        }

        if (respuestas.Select(x => x.PreguntaId).Distinct().Count() != respuestas.Count)
            throw new DomainException("respuesta_duplicada", "No puede responder una misma pregunta más de una vez.");

        var porPregunta = respuestas.ToDictionary(x => x.PreguntaId);
        var datos = instrumento.Preguntas.Select(p =>
        {
            porPregunta.TryGetValue(p.Id, out var respuesta);
            return (Pregunta: p, Escala: respuesta?.ValorEscala, Texto: respuesta?.ValorTexto);
        }).Where(x => porPregunta.ContainsKey(x.Pregunta.Id)).ToArray();

        var entidad = RespuestaInstrumentoPiloto.Crear(usuarioId, instrumento, datos);
        piloto.Agregar(entidad);
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(instrumento, true, true, elegibilidad.HabilitadoDesde);
    }

    private async Task<(bool Habilitado, DateOnly? HabilitadoDesde)> ObtenerElegibilidad(
        Guid usuarioId, string codigo, CancellationToken ct)
    {
        if (!string.Equals(codigo, "postuso", StringComparison.Ordinal))
            return (true, null);

        var fechaFin = await piloto.ObtenerFechaFinPiloto(usuarioId, ct);
        if (fechaFin is null) return (false, null);
        var ahoraAsuncion = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaHorariaPiloto);
        var hoyAsuncion = DateOnly.FromDateTime(ahoraAsuncion.DateTime);
        return (hoyAsuncion >= fechaFin.Value, fechaFin.Value);
    }

    private async Task<InstrumentoPiloto> ObtenerActivo(string codigo, CancellationToken ct)
    {
        if (!CodigosPermitidos.Contains(codigo))
            throw new NotFoundException("instrumento_no_encontrado", "El instrumento solicitado no existe.");
        return await piloto.ObtenerInstrumentoActivo(codigo, ct)
            ?? throw new NotFoundException("instrumento_no_encontrado", "No hay una versión activa del instrumento.");
    }

    private static InstrumentoPilotoResponse Map(
        InstrumentoPiloto x, bool respondido, bool habilitado, DateOnly? habilitadoDesde) => new(
        x.Codigo, x.VersionInstrumento, x.Titulo, x.Descripcion, respondido, habilitado, habilitadoDesde,
        x.Preguntas.OrderBy(p => p.Orden).Select(p => new PreguntaInstrumentoResponse(
            p.Id, p.Orden, p.Tipo, p.Texto, p.Requerida, p.Minimo, p.Maximo)).ToArray());
}
