using System.Text;
using FinanzasInteligentes.Infraestructura.Procesamiento.Outbox;

namespace FinanzasInteligentes.IntegrationTests.Procesamiento;

public sealed class ExtractoPdfGeneratorTests
{
    [Fact]
    public void Crear_GeneraPdfPaginadoConResumenYDetalle()
    {
        var movimientos = Enumerable.Range(1, 50)
            .Select(index => new ExtractoPdfMovimiento(
                new DateOnly(2026, 9, Math.Min(index, 28)),
                $"Movimiento {index}",
                index % 2 == 0 ? "Alimentación" : "Transporte",
                index % 3 == 0 ? "Crédito Itaú" : "Efectivo",
                index % 5 == 0 ? "ingreso" : "gasto",
                10_000 + index,
                index % 5 == 0 ? 10_000 + index : 0,
                index % 5 == 0 ? 0 : 10_000 + index))
            .ToArray();
        var ingresos = movimientos.Sum(x => x.IngresoAnalitico);
        var gastos = movimientos.Sum(x => x.GastoAnalitico);

        var bytes = ExtractoPdfGenerator.Crear(
            new ExtractoPdfMetadata(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 30),
                "Todas las cuentas",
                new DateOnly(2026, 9, 22),
                ingresos,
                gastos),
            movimientos);
        var contenido = Encoding.Latin1.GetString(bytes);
        Assert.StartsWith("%PDF-1.7", contenido);
        Assert.Contains("/Count 3", contenido);
        Assert.Contains("Extracto financiero", contenido);
        Assert.Contains("Página 3 de 3", contenido);
        Assert.Contains("Alimentación", contenido);
    }
}
