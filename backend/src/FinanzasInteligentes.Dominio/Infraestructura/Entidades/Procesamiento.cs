using FinanzasInteligentes.BuildingBlocks;
using System.Text.Json;

namespace FinanzasInteligentes.Dominio.Infraestructura.Entidades;

public sealed class EventoOutbox : Entity
{
    private EventoOutbox()
    { }

    public string Tipo { get; private set; } = string.Empty;
    public string AgregadoTipo { get; private set; } = string.Empty;
    public Guid AgregadoId { get; private set; }
    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");
    public string? CorrelationId { get; private set; }
    public DateTimeOffset DisponibleEn { get; private set; }
    public DateTimeOffset? ProcesadoEn { get; private set; }
    public int Intentos { get; private set; }
    public string? UltimoError { get; private set; }
    public string Estado { get; private set; } = "pendiente";

    public static EventoOutbox Crear(string tipo, string agregadoTipo, Guid agregadoId, object payload, string? correlationId) =>
        new()
        {
            Tipo = tipo,
            AgregadoTipo = agregadoTipo,
            AgregadoId = agregadoId,
            Payload = JsonSerializer.SerializeToDocument(payload),
            CorrelationId = correlationId,
            DisponibleEn = DateTimeOffset.UtcNow
        };

    public void MarcarProcesado(bool eliminarPayloadSensible = false)
    {
        if (eliminarPayloadSensible)
        {
            Payload.Dispose();
            Payload = JsonDocument.Parse("""{"redactado":true}""");
        }
        Estado = "procesado";
        ProcesadoEn = DateTimeOffset.UtcNow;
    }

    public void ReprogramarError(Exception exception, int maximoIntentos)
    {
        Intentos = checked(Intentos + 1);
        UltimoError = exception.Message.Length > 1_000
            ? exception.Message[..1_000]
            : exception.Message;

        if (Intentos >= maximoIntentos)
        {
            Estado = "fallido";
            ProcesadoEn = DateTimeOffset.UtcNow;
            return;
        }

        var demora = Intentos switch
        {
            1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            4 => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(6)
        };
        DisponibleEn = DateTimeOffset.UtcNow.Add(demora);
    }

    public void MarcarFallido(string motivo)
    {
        Estado = "fallido";
        UltimoError = motivo.Length > 1_000 ? motivo[..1_000] : motivo;
        ProcesadoEn = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Reserva persistente de un efecto externo. Para el correo se crea y confirma antes
/// de llamar al proveedor, de modo que un reintento del outbox nunca vuelva a enviar
/// el mismo mensaje si el fallo ocurrió en otra integración posterior.
/// </summary>
public sealed class EntregaOutbox : Entity
{
    private EntregaOutbox()
    { }

    public Guid EventoOutboxId { get; private set; }
    public string Canal { get; private set; } = string.Empty;
    public string DestinatarioHash { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "reservada";
    public DateTimeOffset ReservadaEn { get; private set; }
    public DateTimeOffset? EnviadaEn { get; private set; }

    public static EntregaOutbox Reservar(
        Guid eventoOutboxId, string canal, string destinatarioHash) =>
        new()
        {
            EventoOutboxId = eventoOutboxId,
            Canal = canal,
            DestinatarioHash = destinatarioHash,
            ReservadaEn = DateTimeOffset.UtcNow
        };

    public void MarcarEnviada()
    {
        Estado = "enviada";
        EnviadaEn = DateTimeOffset.UtcNow;
    }

    public void MarcarOmitida() => Estado = "omitida";
}

public sealed class Idempotencia : Entity
{
    private Idempotencia()
    { }

    public Guid? UsuarioId { get; private set; }
    public string Clave { get; private set; } = string.Empty;
    public string Metodo { get; private set; } = string.Empty;
    public string Ruta { get; private set; } = string.Empty;
    public string HashSolicitud { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "procesando";
    public short? CodigoRespuesta { get; private set; }
    public JsonDocument? CuerpoRespuesta { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
}
