using System.Text.Json.Serialization;

namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record MovimientoRecurrenteRequest(
    string Tipo,
    long Monto,
    IReadOnlyCollection<Guid> CategoriaIds,
    Guid CuentaId,
    string Descripcion,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    string Frecuencia,
    long? CantidadOcurrencias);

public sealed class MovimientoRecurrentePatchRequest
{
    private DateOnly? fechaFin;
    private long? cantidadOcurrencias;

    public string? Tipo { get; init; }
    public long? Monto { get; init; }
    public IReadOnlyCollection<Guid>? CategoriaIds { get; init; }
    public Guid? CuentaId { get; init; }
    public string? Descripcion { get; init; }
    public DateOnly? FechaInicio { get; init; }

    public DateOnly? FechaFin
    {
        get => fechaFin;
        init
        {
            fechaFin = value;
            FechaFinEspecificada = true;
        }
    }

    public string? Frecuencia { get; init; }

    public long? CantidadOcurrencias
    {
        get => cantidadOcurrencias;
        init
        {
            cantidadOcurrencias = value;
            CantidadOcurrenciasEspecificada = true;
        }
    }

    [JsonIgnore]
    public bool FechaFinEspecificada { get; private set; }

    [JsonIgnore]
    public bool CantidadOcurrenciasEspecificada { get; private set; }
}