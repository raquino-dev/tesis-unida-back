namespace FinanzasInteligentes.Aplicacion.Suscripciones.Modelos;

public sealed record PlanSuscripcionResponse(
    Guid Id,
    string Codigo,
    string Nombre,
    long Precio,
    string Moneda,
    string Periodo,
    IReadOnlyCollection<string> Capacidades,
    bool Destacado);

public sealed record PlanSuscripcionResumenResponse(Guid Id, string Codigo, string Nombre);

public sealed record SuscripcionResponse(
    Guid Id,
    string Estado,
    PlanSuscripcionResumenResponse Plan,
    DateTimeOffset IniciadaEn,
    DateTimeOffset? CanceladaEn,
    DateTimeOffset FinPeriodoEn,
    IReadOnlyCollection<string> Capacidades,
    long Version);

public sealed record TransaccionSuscripcionResponse(
    Guid Id, Guid SuscripcionId, string Tipo, string Estado,
    string Proveedor, long Monto, string Moneda, DateTimeOffset OcurridoEn);
