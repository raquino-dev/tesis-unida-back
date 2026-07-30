using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Correo;

public sealed class CorreoOptions
{
    public const string SectionName = "Correo";
    public bool Habilitado { get; init; }
    public string Region { get; init; } = "us-east-1";
    public string Remitente { get; init; } = string.Empty;
    public string NombreRemitente { get; init; } = "Finanzas Inteligentes";
    public string UrlAplicacion { get; init; } = "https://app.example.invalid";
}

public interface ICorreoSender
{
    Task Enviar(
        string destinatario, string asunto, string texto, string html,
        CancellationToken cancellationToken);
}

public sealed class SesCorreoSender(
    IOptions<CorreoOptions> options,
    ILogger<SesCorreoSender> logger) : ICorreoSender, IDisposable
{
    private readonly CorreoOptions _options = options.Value;
    private AmazonSimpleEmailServiceV2Client? _client;

    public async Task Enviar(
        string destinatario, string asunto, string texto, string html,
        CancellationToken cancellationToken)
    {
        if (!_options.Habilitado)
        {
            logger.LogWarning(
                "Correo deshabilitado: no se envió el mensaje {Asunto} a {Destinatario}",
                asunto, Enmascarar(destinatario));
            return;
        }

        _client ??= new AmazonSimpleEmailServiceV2Client(
            RegionEndpoint.GetBySystemName(_options.Region));
        await _client.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = $"{_options.NombreRemitente} <{_options.Remitente}>",
            Destination = new Destination { ToAddresses = [destinatario] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = asunto, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Text = new Content { Data = texto, Charset = "UTF-8" },
                        Html = new Content { Data = html, Charset = "UTF-8" }
                    }
                }
            }
        }, cancellationToken);
    }

    public void Dispose() => _client?.Dispose();

    private static string Enmascarar(string correo)
    {
        var partes = correo.Split('@', 2);
        return partes.Length == 2 ? $"{partes[0][..Math.Min(2, partes[0].Length)]}***@{partes[1]}" : "***";
    }
}
