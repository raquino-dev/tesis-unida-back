namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record CrearMovimientoRequest(
    string Ambito,
    Guid CuentaId,
    string Tipo,
    long Monto,
    string Descripcion,
    DateOnly Fecha,
    TimeOnly? Hora = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null,
    Guid? DocumentoId = null,
    Guid? MovimientoRecurrenteId = null,
    Guid? GrupoFamiliarId = null);