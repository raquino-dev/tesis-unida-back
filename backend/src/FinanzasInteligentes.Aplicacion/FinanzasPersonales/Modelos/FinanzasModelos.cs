namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;

public sealed record CuentaResponse(
    Guid Id,
    string Nombre,
    string Tipo,
    string Moneda,
    long SaldoActual,
    long SaldoInicial,
    string? Color,
    string? Icono,
    bool IncluidaEnTotal,
    bool Eliminada,
    long Version);

public sealed record CategoriaResponse(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Icono,
    string? Color,
    bool Predefinida,
    bool EnUso,
    long Version);

public sealed record MovimientoResponse(
    Guid Id,
    string Ambito,
    Guid CuentaId,
    string Tipo,
    long Monto,
    string Moneda,
    string Descripcion,
    DateOnly Fecha,
    string Estado,
    DateTimeOffset CreadoEn,
    long Version,
    IReadOnlyCollection<Guid> CategoriaIds,
    Guid? DocumentoId,
    Guid? MovimientoRecurrenteId);

public sealed record CuentaPagoTarjetaResponse(Guid Id, string Nombre, string Tipo);

public sealed record TarjetaCreditoResponse(
    Guid Id,
    string Alias,
    CuentaPagoTarjetaResponse CuentaPago,
    long DiaCierre,
    long DiaVencimiento,
    long LimiteCredito,
    long SaldoUtilizado,
    long CreditoDisponible,
    string Moneda,
    string Color,
    long Version);

public sealed record PaginacionFinanzasResponse(string? CursorSiguiente);

public sealed record PaginaTarjetaCreditoResponse(
    IReadOnlyCollection<TarjetaCreditoResponse> Datos,
    PaginacionFinanzasResponse Paginacion);

public sealed record Pagina<T>(IReadOnlyCollection<T> Elementos, string? CursorSiguiente);
