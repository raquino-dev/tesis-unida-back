using FinanzasInteligentes.Dominio.Suscripciones;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface ISuscripcionesRepository
{
    Task<IReadOnlyCollection<PlanSuscripcion>> ListarPlanes(CancellationToken ct);

    Task<PlanSuscripcion?> ObtenerPlanPorCodigo(string codigo, CancellationToken ct);

    Task<PlanSuscripcion?> ObtenerPlan(Guid planId, CancellationToken ct);

    Task<Suscripcion?> ObtenerVigente(Guid usuarioId, bool soloLectura, CancellationToken ct);

    Task<Suscripcion?> ObtenerPorComprobante(
        string proveedor, string hashComprobante, bool soloLectura, CancellationToken ct);
    Task<IReadOnlyCollection<TransaccionSuscripcion>> ListarTransacciones(
        Guid usuarioId, CancellationToken ct);
    Task<bool> ExisteTransaccion(
        string proveedor, string referenciaHash, string tipo, CancellationToken ct);

    void Agregar(Suscripcion suscripcion);
    void Agregar(TransaccionSuscripcion transaccion);
}

public interface IValidadorComprobanteSuscripcion
{
    Task<ComprobanteSuscripcionValidado> Validar(
        string proveedor, string comprobante, string? planCodigo,
        CancellationToken ct, bool requerirVigente = true);
}

public sealed record ComprobanteSuscripcionValidado(
    string Proveedor, string HashComprobante, string? PlanCodigo,
    DateTimeOffset? FinPeriodoEn = null, string? Estado = null);

public interface IValidadorNotificacionGooglePlay
{
    Task<NotificacionGooglePlayValidada> Validar(
        string tokenAutorizacion, string dataBase64, CancellationToken ct);
}

public sealed record NotificacionGooglePlayValidada(
    string PurchaseToken, string SubscriptionId, int Tipo,
    DateTimeOffset OcurridoEn, string ReferenciaHash);
