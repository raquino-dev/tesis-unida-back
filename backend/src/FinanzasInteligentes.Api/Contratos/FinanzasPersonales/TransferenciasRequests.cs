namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record TransferenciaRequest(
    Guid CuentaOrigenId,
    Guid CuentaDestinoId,
    long Monto,
    DateOnly Fecha,
    string Descripcion);