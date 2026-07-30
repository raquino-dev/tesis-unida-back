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

    public void MarcarProcesado()
    {
        Estado = "procesado";
        ProcesadoEn = DateTimeOffset.UtcNow;
    }
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