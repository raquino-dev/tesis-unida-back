using FinanzasInteligentes.Api.Configuracion;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace FinanzasInteligentes.Api.Dependencies;

public static class ProductionSecurityDependencyInjection
{
    public static IServiceCollection AddProductionSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        AddReverseProxy(services, configuration);
        AddPersistentDataProtection(services, configuration, environment);

        return services;
    }

    private static void AddReverseProxy(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ReverseProxyOptions.SectionName)
            .Get<ReverseProxyOptions>() ?? new ReverseProxyOptions();

        var knownProxies = options.KnownProxies
            .SelectMany(ParseProxyAddresses)
            .Distinct()
            .ToArray();

        if (options.Enabled && knownProxies.Length == 0)
        {
            throw new InvalidOperationException(
                "ReverseProxy:Enabled está activo, pero ReverseProxy:KnownProxies no contiene ninguna IP confiable.");
        }

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;
            forwarded.ForwardLimit = 1;

            forwarded.KnownIPNetworks.Clear();
            forwarded.KnownProxies.Clear();

            foreach (var proxy in knownProxies)
            {
                forwarded.KnownProxies.Add(proxy);
            }
        });
    }

    private static void AddPersistentDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var options = configuration
            .GetSection(DataProtectionStorageOptions.SectionName)
            .Get<DataProtectionStorageOptions>() ?? new DataProtectionStorageOptions();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            throw new InvalidOperationException("DataProtection:ApplicationName es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(options.KeysPath))
        {
            throw new InvalidOperationException("DataProtection:KeysPath es obligatorio.");
        }

        var keysPath = Path.GetFullPath(options.KeysPath, environment.ContentRootPath);
        Directory.CreateDirectory(keysPath);

        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            if (string.IsNullOrWhiteSpace(options.CertificatePassword))
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificatePassword es obligatorio cuando se configura DataProtection:CertificatePath.");
            }

            var certificatePath = Path.GetFullPath(options.CertificatePath, environment.ContentRootPath);
            if (!File.Exists(certificatePath))
            {
                throw new InvalidOperationException(
                    $"No se encontró el certificado de Data Protection en '{certificatePath}'.");
            }

            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                options.CertificatePassword,
                OperatingSystem.IsMacOS()
                    ? X509KeyStorageFlags.DefaultKeySet
                    : X509KeyStorageFlags.EphemeralKeySet);

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    "El certificado de Data Protection debe contener su clave privada.");
            }

            dataProtection.ProtectKeysWithCertificate(certificate);
            return;
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "En Production debe configurar DataProtection:CertificatePath y DataProtection:CertificatePassword " +
                "para cifrar las claves persistidas.");
        }
    }

    private static IEnumerable<IPAddress> ParseProxyAddresses(string value)
    {
        if (!IPAddress.TryParse(value, out var address))
        {
            throw new InvalidOperationException(
                $"ReverseProxy:KnownProxies contiene una IP inválida: '{value}'.");
        }

        yield return address;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            yield return address.MapToIPv6();
        }
    }
}
