using System.Text.RegularExpressions;

namespace FinanzasInteligentes.ContractTests.OpenApi;

public sealed partial class OpenApiContractTests
{
    private static string OpenApiPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../docs/api/openapi.yaml"));

    [Fact]
    public void OpenApiContieneTodoElCatalogoRelevado()
    {
        var yaml = File.ReadAllText(OpenApiPath);

        Assert.Equal(132, OperationIdRegex().Matches(yaml).Count);
        Assert.Equal(83, PathRegex().Matches(yaml).Count);
        Assert.DoesNotContain("CT-SIN-ASIGNAR", yaml);
        Assert.DoesNotContain("x-etapa: 0", yaml);
    }

    [Fact]
    public void TarjetasUsanSoloAliasYSinDatosDelPlastico()
    {
        var yaml = File.ReadAllText(OpenApiPath);

        Assert.Contains("TarjetaCreditoRequest:", yaml);
        Assert.Contains("alias:", yaml);
        Assert.DoesNotContain("ultimosCuatro:", yaml);
        Assert.DoesNotContain("emisor:", yaml);
    }

    [GeneratedRegex(@"(?m)^\s{6}operationId: ")]
    private static partial Regex OperationIdRegex();

    [GeneratedRegex(@"(?m)^  ""/[^""]+"":$")]
    private static partial Regex PathRegex();
}
