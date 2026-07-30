namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Planificacion;

public sealed record PaginacionPlanificacionResponse(
    string? SiguienteCursor, bool HayMas, long Limite);

public sealed record CategoriaPresupuestoResponse(Guid Id, string Nombre);

public sealed record PresupuestoResponse(
    Guid Id,
    string Ambito,
    string Nombre,
    long Monto,
    long Gastado,
    long Disponible,
    double Progreso,
    string Estado,
    string Salud,
    string Periodo,
    IReadOnlyCollection<CategoriaPresupuestoResponse> Categorias,
    long Version);

public sealed record PaginaPresupuestoResponse(
    IReadOnlyCollection<PresupuestoResponse> Datos,
    PaginacionPlanificacionResponse Paginacion);

public sealed record ResumenPresupuestarioResponse(
    long Total,
    long Gastado,
    long Disponible,
    double Progreso,
    IReadOnlyCollection<PresupuestoResponse> Presupuestos);

public sealed record CuentaMetaResponse(Guid Id, string Nombre);

public sealed record MetaAhorroResponse(
    Guid Id,
    string Ambito,
    Guid? GrupoFamiliarId,
    string Nombre,
    long MontoObjetivo,
    long MontoAhorrado,
    long MontoRestante,
    double Progreso,
    DateOnly FechaObjetivo,
    CuentaMetaResponse Cuenta,
    long Version);

public sealed record PaginaMetaAhorroResponse(
    IReadOnlyCollection<MetaAhorroResponse> Datos,
    PaginacionPlanificacionResponse Paginacion);

public sealed record UsuarioAporteResponse(Guid Id, string Nombre);

public sealed record AporteMetaResponse(
    Guid Id,
    Guid MetaAhorroId,
    long Monto,
    UsuarioAporteResponse AportadoPor,
    DateOnly Fecha,
    DateTimeOffset CreadoEn,
    long SaldoMeta);

public sealed record PaginaAporteMetaResponse(
    IReadOnlyCollection<AporteMetaResponse> Datos,
    PaginacionPlanificacionResponse Paginacion);