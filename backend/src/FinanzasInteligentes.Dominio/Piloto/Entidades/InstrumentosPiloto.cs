using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Piloto;

public sealed class InstrumentoPiloto : Entity
{
    private InstrumentoPiloto() { }

    public string Codigo { get; private set; } = string.Empty;
    public string VersionInstrumento { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public bool Activo { get; private set; }
    public ICollection<PreguntaInstrumentoPiloto> Preguntas { get; } = [];
}

public sealed class PreguntaInstrumentoPiloto : Entity
{
    private PreguntaInstrumentoPiloto() { }

    public Guid InstrumentoId { get; private set; }
    public int Orden { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Texto { get; private set; } = string.Empty;
    public bool Requerida { get; private set; }
    public int? Minimo { get; private set; }
    public int? Maximo { get; private set; }
}

public sealed class RespuestaInstrumentoPiloto : Entity
{
    private RespuestaInstrumentoPiloto() { }

    public Guid UsuarioId { get; private set; }
    public Guid InstrumentoId { get; private set; }
    public string VersionInstrumento { get; private set; } = string.Empty;
    public DateTimeOffset RespondidoEn { get; private set; }
    public ICollection<DetalleRespuestaInstrumentoPiloto> Detalles { get; } = [];

    public static RespuestaInstrumentoPiloto Crear(
        Guid usuarioId,
        InstrumentoPiloto instrumento,
        IReadOnlyCollection<(PreguntaInstrumentoPiloto Pregunta, int? Escala, string? Texto)> respuestas)
    {
        var preguntas = instrumento.Preguntas.OrderBy(x => x.Orden).ToArray();
        if (preguntas.Length == 0)
            throw new DomainException("instrumento_sin_preguntas", "El instrumento no tiene preguntas configuradas.");
        if (respuestas.Select(x => x.Pregunta.Id).Distinct().Count() != respuestas.Count ||
            respuestas.Any(x => !preguntas.Any(p => p.Id == x.Pregunta.Id)))
            throw new DomainException("respuesta_invalida", "Las respuestas no corresponden al instrumento.");

        foreach (var pregunta in preguntas)
        {
            var respuesta = respuestas.SingleOrDefault(x => x.Pregunta.Id == pregunta.Id);
            if (pregunta.Requerida && respuesta.Pregunta is null)
                throw new DomainException("respuesta_requerida", "Debe responder todas las preguntas obligatorias.");
            if (respuesta.Pregunta is null) continue;
            if (pregunta.Tipo == "escala" &&
                (respuesta.Escala is null || respuesta.Escala < pregunta.Minimo || respuesta.Escala > pregunta.Maximo))
                throw new DomainException("escala_invalida", "Una respuesta de escala está fuera del rango permitido.");
            if (pregunta.Tipo == "texto" && pregunta.Requerida && string.IsNullOrWhiteSpace(respuesta.Texto))
                throw new DomainException("texto_requerido", "Debe completar las respuestas de texto obligatorias.");
        }

        var entidad = new RespuestaInstrumentoPiloto
        {
            UsuarioId = usuarioId,
            InstrumentoId = instrumento.Id,
            VersionInstrumento = instrumento.VersionInstrumento,
            RespondidoEn = DateTimeOffset.UtcNow
        };
        foreach (var item in respuestas)
            entidad.Detalles.Add(DetalleRespuestaInstrumentoPiloto.Crear(
                item.Pregunta.Id, item.Escala, item.Texto));
        return entidad;
    }
}

public sealed class DetalleRespuestaInstrumentoPiloto : Entity
{
    private DetalleRespuestaInstrumentoPiloto() { }

    public Guid RespuestaId { get; private set; }
    public Guid PreguntaId { get; private set; }
    public int? ValorEscala { get; private set; }
    public string? ValorTexto { get; private set; }

    public static DetalleRespuestaInstrumentoPiloto Crear(
        Guid preguntaId, int? valorEscala, string? valorTexto) => new()
    {
        PreguntaId = preguntaId,
        ValorEscala = valorEscala,
        ValorTexto = string.IsNullOrWhiteSpace(valorTexto) ? null : valorTexto.Trim()
    };
}
