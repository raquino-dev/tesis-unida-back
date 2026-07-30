namespace FinanzasInteligentes.Api.Contratos.Suscripciones;

public sealed record SuscripcionRequest(
    string PlanCodigo,
    string Proveedor,
    string Comprobante,
    Guid VerificacionOtpId);

public sealed record PostSuscripcionCancelacionesRequest(string? Motivo = null);

public sealed record PostRestauracionesSuscripcionRequest(
    string Proveedor,
    string Comprobante);

public sealed record CambiarPlanSuscripcionRequest(
    string PlanCodigo,
    string Proveedor,
    string Comprobante,
    Guid VerificacionOtpId);

public sealed record GooglePubSubPushRequest(GooglePubSubMessage Message);
public sealed record GooglePubSubMessage(string Data, string? MessageId = null);
