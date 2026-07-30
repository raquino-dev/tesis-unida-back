using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class LocalArchivoStorage : IArchivoStorage
{
    private readonly string root;
    private readonly byte[] signingKey;
    private readonly string publicBaseUrl;

    public LocalArchivoStorage(IOptions<ArchivoStorageOptions> options)
    {
        var configuration = options.Value;
        root = Path.GetFullPath(configuration.Ruta);
        Directory.CreateDirectory(root);
        signingKey = Encoding.UTF8.GetBytes(configuration.SigningKey);
        publicBaseUrl = configuration.PublicBaseUrl.TrimEnd('/');
    }

    public async Task Guardar(string clave, Stream contenido, CancellationToken ct)
    {
        var path = Resolver(clave);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await contenido.CopyToAsync(output, ct);
    }

    public Task<Stream> Abrir(string clave, CancellationToken ct) =>
        Task.FromResult<Stream>(new FileStream(
            Resolver(clave), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));

    public Task Eliminar(string clave, CancellationToken ct)
    {
        var path = Resolver(clave);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string CrearUrlTemporal(string clave, DateTimeOffset expiraEn)
    {
        var unix = expiraEn.ToUnixTimeSeconds();
        var firma = Firmar($"{clave}:{unix}");
        return $"{publicBaseUrl}/api/v1/descargas/{Uri.EscapeDataString(clave)}?expira={unix}&firma={firma}";
    }

    public bool ValidarUrl(string clave, long expiraUnix, string firma) =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expiraUnix &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Firmar($"{clave}:{expiraUnix}")),
            Encoding.ASCII.GetBytes(firma));

    private string Resolver(string clave)
    {
        var normalized = clave.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, normalized));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Clave de archivo inválida.");
        return path;
    }

    private string Firmar(string value) =>
        Convert.ToHexString(HMACSHA256.HashData(
            signingKey, Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
