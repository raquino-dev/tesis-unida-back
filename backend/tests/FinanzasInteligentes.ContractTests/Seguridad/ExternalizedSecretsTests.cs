using FinanzasInteligentes.Infraestructura;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class ExternalizedSecretsTests
{
    private static readonly string SourceRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../../backend/src"));

    private static readonly HashSet<string> ForbiddenPropertyNames = new(
        [
            "ConnectionStrings",
            "SigningKey",
            "TokenPushKey",
            "CertificatePassword",
            "Password",
            "Secret",
            "ApiKey",
            "AccessKey",
            "PrivateKey"
        ],
        StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void AppSettingsVersionadosNoContienenPropiedadesSecretas()
    {
        var appSettings = Directory.GetFiles(
            SourceRoot,
            "appsettings*.json",
            SearchOption.AllDirectories)
            .Where(path =>
                !path.Split(Path.DirectorySeparatorChar)
                    .Any(segment =>
                        segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                        segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.NotEmpty(appSettings);

        foreach (var path in appSettings)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var forbiddenPaths = new List<string>();
            FindForbiddenProperties(document.RootElement, "$", forbiddenPaths);

            Assert.True(
                forbiddenPaths.Count == 0,
                $"{Path.GetRelativePath(SourceRoot, path)} contiene configuración sensible: " +
                string.Join(", ", forbiddenPaths));
        }
    }

    [Fact]
    public void ClavesDeDistintoPropositoNoPuedenCompartirValor()
    {
        const string repeatedSecret = "secreto-repetido-con-mas-de-32-bytes-para-prueba";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] =
                    "Host=localhost;Database=finanzas;Username=prueba;Password=prueba",
                ["Jwt:SigningKey"] = repeatedSecret,
                ["Archivos:SigningKey"] = repeatedSecret,
                ["Seguridad:TokenPushKey"] = "otro-secreto-independiente-con-mas-de-32-bytes"
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfraestructura(configuration));

        Assert.Contains("deben usar secretos diferentes", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(repeatedSecret, exception.Message, StringComparison.Ordinal);
    }

    private static void FindForbiddenProperties(
        JsonElement element,
        string path,
        ICollection<string> forbiddenPaths)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = $"{path}.{property.Name}";
                if (ForbiddenPropertyNames.Contains(property.Name))
                {
                    forbiddenPaths.Add(propertyPath);
                }

                FindForbiddenProperties(property.Value, propertyPath, forbiddenPaths);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                FindForbiddenProperties(item, $"{path}[{index}]", forbiddenPaths);
                index++;
            }
        }
    }
}
