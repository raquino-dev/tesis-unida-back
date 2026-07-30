namespace FinanzasInteligentes.Aplicacion.FinanzasFamiliares.Modelos;

public sealed record PaginacionFamiliarResponse(string? CursorSiguiente);
public sealed record GrupoFamiliarResponse(
    Guid Id, string Nombre, string MiRol, long CantidadIntegrantes,
    long CantidadCuentasCompartidas, DateTimeOffset CreadoEn, long Version);
public sealed record PaginaGrupoFamiliarResponse(
    IReadOnlyCollection<GrupoFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record UsuarioFamiliarResponse(Guid Id, string Nombre, string Correo);
public sealed record IntegranteFamiliarResponse(
    Guid Id, UsuarioFamiliarResponse Usuario, string Rol,
    DateTimeOffset IncorporadoEn, long Version);
public sealed record PaginaIntegranteFamiliarResponse(
    IReadOnlyCollection<IntegranteFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record CrearInvitacionFamiliarResponse(
    Guid Id, Guid GrupoFamiliarId, string? Correo, Guid? UsuarioDestino,
    string Rol, string Estado, DateTimeOffset ExpiraEn, long Version, string Codigo);
public sealed record InvitacionFamiliarResponse(
    Guid Id, string? Correo, Guid? UsuarioDestino, string Rol,
    string Estado, DateTimeOffset ExpiraEn, long Version);
public sealed record PaginaInvitacionFamiliarResponse(
    IReadOnlyCollection<InvitacionFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);
public sealed record GrupoInvitacionResponse(Guid Id, string Nombre);
public sealed record InvitacionPublicaResponse(
    GrupoInvitacionResponse Grupo, string DestinoEnmascarado,
    string Rol, string Estado, DateTimeOffset ExpiraEn);

public sealed record CuentaFamiliarResumenResponse(Guid Id, string Nombre, string Tipo, string Moneda);
public sealed record UsuarioResumenFamiliarResponse(Guid Id, string Nombre);
public sealed record CuentaCompartidaResponse(
    Guid GrupoFamiliarId, CuentaFamiliarResumenResponse Cuenta,
    UsuarioResumenFamiliarResponse CompartidaPor, DateTimeOffset CompartidaEn, long Version);
public sealed record PaginaCuentaCompartidaResponse(
    IReadOnlyCollection<CuentaCompartidaResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record CategoriaFamiliarResponse(
    Guid Id, Guid GrupoFamiliarId, string Nombre, string Tipo,
    string Icono, string Color, long Version);
public sealed record PaginaCategoriaFamiliarResponse(
    IReadOnlyCollection<CategoriaFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record MovimientoFamiliarResponse(
    Guid Id, string Ambito, Guid CuentaId, string Tipo, long Monto, string Moneda,
    string Descripcion, DateOnly Fecha, IReadOnlyCollection<Guid> CategoriaIds,
    Guid CreadoPorId, string Estado, DateTimeOffset CreadoEn, long Version);
public sealed record PaginaMovimientoFamiliarResponse(
    IReadOnlyCollection<MovimientoFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record CajaCompartidaResponse(
    Guid GrupoFamiliarId, long Saldo, long TotalAportesMes, long TotalRetirosMes, long Version);
public sealed record OperacionCajaResponse(
    Guid Id, string Tipo, long Monto, string Descripcion,
    UsuarioResumenFamiliarResponse RealizadoPor, DateTimeOffset CreadoEn,
    long SaldoAnterior, long SaldoPosterior);
public sealed record PaginaOperacionCajaResponse(
    IReadOnlyCollection<OperacionCajaResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record CategoriaPresupuestoResponse(Guid Id, string Nombre);
public sealed record PresupuestoFamiliarResponse(
    Guid Id, string Ambito, string Nombre, long Monto, long Gastado, long Disponible,
    double Progreso, string Estado, string Salud, string Periodo,
    IReadOnlyCollection<CategoriaPresupuestoResponse> Categorias, long Version);
public sealed record PaginaPresupuestoFamiliarResponse(
    IReadOnlyCollection<PresupuestoFamiliarResponse> Datos, PaginacionFamiliarResponse Paginacion);

public sealed record PeriodoAnaliticoResponse(DateOnly Desde, DateOnly Hasta);
public sealed record CategoriaAnaliticaResponse(Guid CategoriaId, string Nombre, long Monto, double Porcentaje);
public sealed record DashboardFamiliarResponse(
    string Ambito, PeriodoAnaliticoResponse Periodo, long Ingresos, long Gastos,
    long Balance, long PresupuestoTotal, long PresupuestoDisponible, long ScoreFinanciero,
    IReadOnlyCollection<CategoriaAnaliticaResponse> CategoriasPrincipales,
    IReadOnlyCollection<object> ProximosRecurrentes,
    IReadOnlyCollection<string> AlertasDestacadas);
public sealed record TendenciaFamiliarResponse(string Periodo, long Ingresos, long Gastos);
public sealed record ReporteFamiliarResponse(
    string Ambito, string Rango, DateOnly Desde, DateOnly Hasta,
    long Ingresos, long Gastos, long Balance,
    IReadOnlyCollection<CategoriaAnaliticaResponse> Distribucion,
    IReadOnlyCollection<TendenciaFamiliarResponse> Tendencia,
    IReadOnlyCollection<string> Observaciones);
public sealed record CategoriaProyeccionResponse(string Nombre, long MontoProyectado, double Variacion);
public sealed record HistorialProyeccionResponse(string Periodo, long Proyectado, long Real);
public sealed record ProyeccionFamiliarResponse(
    string Ambito, long MesesHistorial, bool Preliminar, long GastoProyectado,
    long BalanceProyectado, string CategoriaMayorCrecimiento, string NivelRiesgo,
    string VersionModelo, PeriodoAnaliticoResponse Periodo,
    IReadOnlyCollection<CategoriaProyeccionResponse> Categorias,
    IReadOnlyCollection<HistorialProyeccionResponse> Historial,
    DateTimeOffset GeneradoEn);

public sealed record ProcesoGrupoResponse(
    Guid Id, string Estado, DateTimeOffset CreadoEn, DateTimeOffset? CompletadoEn,
    string? ErrorCodigo, string UrlEstado, long Version);