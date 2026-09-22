namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record PatchMovimientosByMovimientoIdRequest(
    string? Descripcion = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null,
    Guid? DocumentoId = null,
    Guid? CuentaId = null,
    string? Tipo = null,
    long? Monto = null,
    DateOnly? Fecha = null,
    TimeOnly? Hora = null);
