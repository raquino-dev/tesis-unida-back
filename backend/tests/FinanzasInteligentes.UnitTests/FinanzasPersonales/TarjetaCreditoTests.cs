using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class TarjetaCreditoTests
{
    [Fact]
    public void CrearCalculaCreditoDisponible()
    {
        var tarjeta = Crear();

        Assert.Equal(15_000_000, tarjeta.CreditoDisponible);
        Assert.Equal("Compras del hogar", tarjeta.Alias);
        Assert.Equal(1, tarjeta.Version);
    }

    [Fact]
    public void CrearRechazaAliasVacio()
    {
        var exception = Assert.Throws<DomainException>(() => TarjetaCredito.Crear(
            Guid.CreateVersion7(), " ", Guid.CreateVersion7(),
            20, 5, 1_000_000, "PYG", "#6868A6"));

        Assert.Equal("alias_invalido", exception.Code);
    }

    [Fact]
    public void ActualizarYEliminarIncrementanVersion()
    {
        var tarjeta = Crear();

        tarjeta.Actualizar(
            "Tarjeta nueva", null, 21, null, 20_000_000, null, null);
        tarjeta.Eliminar();

        Assert.Equal("Tarjeta nueva", tarjeta.Alias);
        Assert.Equal(20_000_000, tarjeta.LimiteCredito);
        Assert.NotNull(tarjeta.EliminadoEn);
        Assert.Equal(3, tarjeta.Version);
    }

    private static TarjetaCredito Crear() =>
        TarjetaCredito.Crear(
            Guid.CreateVersion7(), "Compras del hogar",
            Guid.CreateVersion7(), 20, 5, 15_000_000, "PYG", "#6868A6");
}
