using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Suscripciones;

public sealed class GooglePlayOptions
{
    public const string SectionName = "GooglePlay";
    public bool Habilitado { get; init; }
    public bool PermitirProveedorInterno { get; init; } = true;
    public string PackageName { get; init; } = string.Empty;
    public Dictionary<string, string> Productos { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string RtdnAudience { get; init; } = string.Empty;
    public string RtdnServiceAccountEmail { get; init; } = string.Empty;
}

public sealed class ValidadorComprobanteSuscripcion(
    IOptions<GooglePlayOptions> options) : IValidadorComprobanteSuscripcion, IDisposable
{
    private readonly GooglePlayOptions _options = options.Value;
    private AndroidPublisherService? _service;

    public async Task<ComprobanteSuscripcionValidado> Validar(
        string proveedor, string comprobante, string? planCodigo,
        CancellationToken ct, bool requerirVigente = true)
    {
        var normalizado = proveedor.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(comprobante) || comprobante.Trim().Length is < 8 or > 4096)
            throw new DomainException("comprobante_invalido", "El comprobante no es válido.");
        if (normalizado == "interno")
        {
            if (!_options.PermitirProveedorInterno)
                throw new DomainException("proveedor_invalido", "El proveedor interno está deshabilitado.");
            return new(normalizado, Hash(comprobante), planCodigo);
        }
        if (normalizado != "google-play")
            throw new DomainException("proveedor_invalido", "Inicialmente sólo se admite Google Play Billing.");
        if (!_options.Habilitado)
            throw new DomainException(
                "google_play_no_configurado", "La validación de Google Play todavía no está configurada.");

        _service ??= await CrearServicio(ct);
        var compra = await _service.Purchases.Subscriptionsv2
            .Get(_options.PackageName, comprobante.Trim()).ExecuteAsync(ct);
        if (requerirVigente && compra.SubscriptionState is not (
            "SUBSCRIPTION_STATE_ACTIVE" or
            "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or
            "SUBSCRIPTION_STATE_CANCELED"))
            throw new DomainException("suscripcion_google_no_vigente", "La compra de Google Play no está vigente.");

        var lineas = compra.LineItems ?? [];
        var productoEsperado = planCodigo is null
            ? null
            : _options.Productos.GetValueOrDefault(planCodigo);
        var linea = productoEsperado is null
            ? lineas.OrderByDescending(x => x.ExpiryTimeDateTimeOffset).FirstOrDefault()
            : lineas.FirstOrDefault(x => x.ProductId == productoEsperado);
        if (linea is null)
            throw new DomainException("producto_google_invalido", "La compra no corresponde al plan solicitado.");
        var fin = linea.ExpiryTimeDateTimeOffset;
        if (fin is null || requerirVigente && fin <= DateTimeOffset.UtcNow)
            throw new DomainException("suscripcion_google_expirada", "La compra de Google Play está expirada.");

        var estado = compra.SubscriptionState switch
        {
            "SUBSCRIPTION_STATE_ACTIVE" => "activa",
            "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or "SUBSCRIPTION_STATE_ON_HOLD" => "en_gracia",
            "SUBSCRIPTION_STATE_CANCELED" => "cancelada",
            "SUBSCRIPTION_STATE_PAUSED" => "pausada",
            "SUBSCRIPTION_STATE_EXPIRED" => "expirada",
            _ => "pendiente"
        };
        return new(normalizado, Hash(comprobante), planCodigo, fin.Value, estado);
    }

    private async Task<AndroidPublisherService> CrearServicio(CancellationToken ct)
    {
        var credential = await GoogleCredential.GetApplicationDefaultAsync(ct);
        return new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher),
            ApplicationName = "FinanzasInteligentes"
        });
    }

    private static string Hash(string comprobante) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(comprobante.Trim())));

    public void Dispose() => _service?.Dispose();
}
