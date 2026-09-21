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

        Assert.Equal(140, OperationIdRegex().Matches(yaml).Count);
        Assert.Equal(91, PathRegex().Matches(yaml).Count);
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

    [Fact]
    public void MovimientosPublicanElContratoFinancieroActual()
    {
        var yaml = File.ReadAllLines(OpenApiPath);

        Assert.Equal(
            new HashSet<string>
            {
                "ambito", "cuentaId", "tipo", "monto", "descripcion", "fecha", "hora",
                "categoriaIds", "documentoId", "movimientoRecurrenteId", "grupoFamiliarId",
                "id", "tarjetaCreditoId", "operacionTarjeta"
            },
            ObtenerPropiedades(yaml, "MovimientoRequest"));
        Assert.Equal(
            new HashSet<string>
            {
                "id", "ambito", "cuentaId", "tipo", "monto", "moneda", "descripcion",
                "fecha", "hora", "estado", "creadoEn", "version", "categoriaIds",
                "documentoId", "movimientoRecurrenteId", "transferenciaId",
                "tarjetaCreditoId", "operacionTarjeta"
            },
            ObtenerPropiedades(yaml, "MovimientoResponse"));
        Assert.Equal(
            new HashSet<string>
            {
                "cuentaId", "tipo", "monto", "descripcion", "fecha", "hora",
                "categoriaIds", "documentoId"
            },
            ObtenerPropiedades(yaml, "MovimientoPatchRequest"));
    }

    private static HashSet<string> ObtenerPropiedades(string[] lineas, string esquema)
    {
        var inicio = Array.FindIndex(lineas, linea => linea == $"    {esquema}:");
        Assert.True(inicio >= 0, $"No se encontró el esquema {esquema}.");

        var propiedades = new HashSet<string>(StringComparer.Ordinal);
        var dentroDePropiedades = false;
        for (var indice = inicio + 1; indice < lineas.Length; indice++)
        {
            var linea = lineas[indice];
            if (linea.StartsWith("    ", StringComparison.Ordinal) &&
                !linea.StartsWith("      ", StringComparison.Ordinal))
                break;
            if (linea == "      properties:")
            {
                dentroDePropiedades = true;
                continue;
            }
            if (!dentroDePropiedades)
                continue;

            var coincidencia = PropertyRegex().Match(linea);
            if (coincidencia.Success)
                propiedades.Add(coincidencia.Groups[1].Value);
        }

        Assert.NotEmpty(propiedades);
        return propiedades;
    }

    [GeneratedRegex(@"(?m)^\s{6}operationId: ")]
    private static partial Regex OperationIdRegex();

    [GeneratedRegex(@"(?m)^  ""/[^""]+"":$")]
    private static partial Regex PathRegex();

    [GeneratedRegex(@"^        ([A-Za-z][A-Za-z0-9]*):$")]
    private static partial Regex PropertyRegex();
}
