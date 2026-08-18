namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record TarjetaCreditoRequest(
    string Alias,
    Guid CuentaPagoId,
    long DiaCierre,
    long DiaVencimiento,
    long LimiteCredito,
    string Moneda,
    string Color,
    Guid? Id = null);

public sealed record TarjetaCreditoPatchRequest(
    string? Alias = null,
    Guid? CuentaPagoId = null,
    long? DiaCierre = null,
    long? DiaVencimiento = null,
    long? LimiteCredito = null,
    string? Moneda = null,
    string? Color = null);
