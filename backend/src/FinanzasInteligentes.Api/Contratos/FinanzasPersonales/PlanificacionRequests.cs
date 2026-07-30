namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record PresupuestoRequest(
    string Ambito,
    Guid? GrupoFamiliarId,
    string Nombre,
    long Monto,
    string Periodo,
    IReadOnlyCollection<Guid> CategoriaIds);

public sealed record PresupuestoPatchRequest(
    string? Ambito = null,
    Guid? GrupoFamiliarId = null,
    string? Nombre = null,
    long? Monto = null,
    string? Periodo = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null);

public sealed record MetaAhorroRequest(
    string Ambito,
    Guid? GrupoFamiliarId,
    string Nombre,
    long MontoObjetivo,
    DateOnly FechaObjetivo,
    Guid CuentaId);

public sealed record MetaAhorroPatchRequest(
    string? Nombre = null,
    long? MontoObjetivo = null,
    DateOnly? FechaObjetivo = null);

public sealed record AporteMetaRequest(
    long Monto,
    Guid CuentaOrigenId,
    string Descripcion);