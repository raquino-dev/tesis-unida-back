namespace FinanzasInteligentes.ArchitectureTests.Capas;

public sealed class DependenciasTests
{
    private static string BackendRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact]
    public void DominioNoReferenciaAplicacionNiInfraestructura()
    {
        var project = File.ReadAllText(Path.Combine(
            BackendRoot,
            "src/FinanzasInteligentes.Dominio/FinanzasInteligentes.Dominio.csproj"));

        Assert.DoesNotContain("FinanzasInteligentes.Aplicacion", project);
        Assert.DoesNotContain("FinanzasInteligentes.Infraestructura", project);
    }

    [Fact]
    public void EndpointsNoAccedenDirectamenteAEfCore()
    {
        var endpointRoot = Path.Combine(BackendRoot, "src/FinanzasInteligentes.Api/Endpoints");
        var source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(endpointRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("FinanzasDbContext", source);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source);
    }

    [Fact]
    public void ProgramSoloComponeLaAplicacion()
    {
        var lines = File.ReadAllLines(Path.Combine(
            BackendRoot,
            "src/FinanzasInteligentes.Api/Program.cs"));

        Assert.True(lines.Length < 50, $"Program.cs contiene {lines.Length} líneas.");
    }
}