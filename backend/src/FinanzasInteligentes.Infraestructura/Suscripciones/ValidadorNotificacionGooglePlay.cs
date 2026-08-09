using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinanzasInteligentes.Infraestructura.Suscripciones;

public sealed class ValidadorNotificacionGooglePlay(
    IOptions<GooglePlayOptions> options) : IValidadorNotificacionGooglePlay
{
    private readonly GooglePlayOptions _options = options.Value;

    public async Task<NotificacionGooglePlayValidada?> Validar(
        string tokenAutorizacion, string dataBase64, CancellationToken ct)
    {
        if (!_options.Habilitado ||
            string.IsNullOrWhiteSpace(_options.RtdnAudience) ||
            string.IsNullOrWhiteSpace(_options.RtdnServiceAccountEmail))
            throw new DomainException("rtdn_no_configurado", "Google Play RTDN no está configurado.");
        var jwt = tokenAutorizacion.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? tokenAutorizacion[7..].Trim()
            : string.Empty;
        if (jwt.Length == 0)
            throw new DomainException("rtdn_no_autorizado", "La notificación no está autorizada.");

        var payload = await GoogleJsonWebSignature.ValidateAsync(jwt,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.RtdnAudience]
            });
        if (!payload.EmailVerified ||
            !string.Equals(payload.Email, _options.RtdnServiceAccountEmail, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("rtdn_no_autorizado", "La identidad de Google Pub/Sub no es válida.");

        GooglePlayRtdn? notification;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(dataBase64));
            notification = JsonSerializer.Deserialize<GooglePlayRtdn>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new DomainException("rtdn_invalido", "El mensaje RTDN no es válido.");
        }

        if (notification?.TestNotification is not null)
            return null;

        var subscription = notification?.SubscriptionNotification;
        if (notification?.PackageName != _options.PackageName ||
            subscription is null ||
            string.IsNullOrWhiteSpace(subscription.PurchaseToken) ||
            string.IsNullOrWhiteSpace(subscription.SubscriptionId))
            throw new DomainException("rtdn_invalido", "El mensaje RTDN no corresponde a la aplicación.");
        var ocurrido = long.TryParse(notification.EventTimeMillis, out var millis)
            ? DateTimeOffset.FromUnixTimeMilliseconds(millis)
            : DateTimeOffset.UtcNow;
        var referencia = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{subscription.PurchaseToken}:{subscription.NotificationType}:{notification.EventTimeMillis}")));
        return new(
            subscription.PurchaseToken, subscription.SubscriptionId,
            subscription.NotificationType, ocurrido, referencia);
    }

    private sealed record GooglePlayRtdn(
        string PackageName,
        string EventTimeMillis,
        SubscriptionNotification? SubscriptionNotification,
        TestNotification? TestNotification);
    private sealed record SubscriptionNotification(
        int NotificationType,
        string PurchaseToken,
        string SubscriptionId);
    private sealed record TestNotification(string Version);
}
