using FinanzasInteligentes.Api.Dependencies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class ProductionSecurityConfigurationTests
{
    [Fact]
    public void ProxyHabilitadoRequiereAlMenosUnaIpConfiable()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReverseProxy:Enabled"] = "true"
        });
        var environment = CreateEnvironment(Environments.Development, CreateTemporaryDirectory());
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddProductionSecurity(configuration, environment));

        Assert.Contains("KnownProxies", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProduccionRequiereCertificadoParaCifrarLasClaves()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["DataProtection:KeysPath"] = Path.Combine(directory, "keys")
            });
            var environment = CreateEnvironment(Environments.Production, directory);
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddProductionSecurity(configuration, environment));

            Assert.Contains("CertificatePath", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ProduccionPersisteClavesCifradasConElCertificado()
    {
        var directory = CreateTemporaryDirectory();
        const string password = "contraseña-prueba-data-protection";

        try
        {
            var keysPath = Path.Combine(directory, "keys");
            var certificatePath = Path.Combine(directory, "data-protection.pfx");
            CreateCertificate(certificatePath, password);

            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["DataProtection:KeysPath"] = keysPath,
                ["DataProtection:CertificatePath"] = certificatePath,
                ["DataProtection:CertificatePassword"] = password
            });
            var environment = CreateEnvironment(Environments.Production, directory);
            var services = new ServiceCollection();

            services.AddProductionSecurity(configuration, environment);

            using var provider = services.BuildServiceProvider();
            var protector = provider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("prueba-configuracion");

            _ = protector.Protect("dato-protegido");

            var keyFile = Assert.Single(Directory.GetFiles(keysPath, "key-*.xml"));
            var keyXml = File.ReadAllText(keyFile);
            Assert.Contains("encryptedSecret", keyXml, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static TestWebHostEnvironment CreateEnvironment(string name, string contentRoot) =>
        new()
        {
            EnvironmentName = name,
            ApplicationName = "FinanzasInteligentes.Api.Tests",
            ContentRootPath = contentRoot,
            ContentRootFileProvider = new NullFileProvider(),
            WebRootPath = contentRoot,
            WebRootFileProvider = new NullFileProvider()
        };

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finanzas-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateCertificate(string path, string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Finanzas Inteligentes Tests",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
