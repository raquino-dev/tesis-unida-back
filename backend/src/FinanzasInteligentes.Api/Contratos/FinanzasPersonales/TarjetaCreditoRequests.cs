namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record TarjetaCreditoRequest(
    string Nombre,
    string Emisor,
    string UltimosCuatro,
    Guid CuentaPagoId,
    long DiaCierre,
    long DiaVencimiento,
    long LimiteCredito,
    string Moneda,
    string Color);

public sealed record TarjetaCreditoPatchRequest(
    string? Nombre = null,
    string? Emisor = null,
    string? UltimosCuatro = null,
    Guid? CuentaPagoId = null,
    long? DiaCierre = null,
    long? DiaVencimiento = null,
    long? LimiteCredito = null,
    string? Moneda = null,
    string? Color = null);