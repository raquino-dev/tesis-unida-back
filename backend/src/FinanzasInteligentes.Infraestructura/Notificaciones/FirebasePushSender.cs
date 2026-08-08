using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Infraestructura.Persistencia;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Notificaciones;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";
    public bool Habilitado { get; init; }
    public string ProjectId { get; init; } = string.Empty;
}

public interface IPushNotificationSender
{
    Task Enviar(
        Guid usuarioId,
        string titulo,
        string mensaje,
        IReadOnlyDictionary<string, string> datos,
        CancellationToken cancellationToken);
}

public sealed class FirebasePushSender(
    FinanzasDbContext db,
    IProtectorTokenPush protector,
    IOptions<FirebaseOptions> options,
    ILogger<FirebasePushSender> logger) : IPushNotificationSender, IDisposable
{
    private static readonly string[] Scopes =
        ["https://www.googleapis.com/auth/firebase.messaging"];
    private readonly FirebaseOptions _options = options.Value;
    private readonly HttpClient _http = new();
    private GoogleCredential? _credential;

    public async Task Enviar(
        Guid usuarioId,
        string titulo,
        string mensaje,
        IReadOnlyDictionary<string, string> datos,
        CancellationToken cancellationToken)
    {
        if (!_options.Habilitado)
        {
            logger.LogDebug(
                "Firebase deshabilitado: no se envió push a {UsuarioId}",
                usuarioId);
            return;
        }

        var permitePush = await db.Usuarios.AsNoTracking()
            .Where(x => x.Id == usuarioId)
            .Select(x => x.Preferencias.NotificacionesPush)
            .SingleOrDefaultAsync(cancellationToken);
        if (!permitePush) return;

        var tokens = await db.Dispositivos.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId &&
                x.RevocadoEn == null &&
                x.TokenPushProtegido != string.Empty)
            .Select(x => x.TokenPushProtegido)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0) return;

        _credential ??= (await GoogleCredential.GetApplicationDefaultAsync())
            .CreateScoped(Scopes);
        var accessToken = await _credential.UnderlyingCredential
            .GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
        var endpoint =
            $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(_options.ProjectId)}/messages:send";

        foreach (var tokenProtegido in tokens)
        {
            var token = protector.Desproteger(tokenProtegido);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new
                {
                    message = new
                    {
                        token,
                        notification = new { title = titulo, body = mensaje },
                        data = datos,
                        android = new { priority = "high" }
                    }
                })
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await _http.SendAsync(
                request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                logger.LogWarning(
                    "FCM rechazó un push para {UsuarioId}: {StatusCode} {Detail}",
                    usuarioId, (int)response.StatusCode,
                    detail.Length > 500 ? detail[..500] : detail);
            }
        }
    }

    public void Dispose() => _http.Dispose();
}
