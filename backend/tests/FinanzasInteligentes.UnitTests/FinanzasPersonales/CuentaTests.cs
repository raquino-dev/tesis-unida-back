using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class CuentaTests
{
    [Fact]
    public void AplicarIngresoYGastoActualizaSaldoYVersion()
    {
        var cuenta = Cuenta.Crear(Guid.CreateVersion7(), "Efectivo", "efectivo", 100_000);

        cuenta.AplicarMovimiento("ingreso", 50_000);
        cuenta.AplicarMovimiento("gasto", 25_000);

        Assert.Equal(125_000, cuenta.SaldoActual);
        Assert.Equal(3, cuenta.Version);
    }

    [Fact]
    public void RechazaMontoNoPositivo()
    {
        var cuenta = Cuenta.Crear(Guid.CreateVersion7(), "Efectivo", "efectivo", 0);

        var exception = Assert.Throws<DomainException>(() => cuenta.AplicarMovimiento("gasto", 0));

        Assert.Equal("monto_invalido", exception.Code);
    }

    [Fact]
    public void ActualizarSaldoInicialConservaLosMovimientosAplicados()
    {
        var cuenta = Cuenta.Crear(Guid.CreateVersion7(), "Efectivo", "efectivo", 100_000);
        cuenta.AplicarMovimiento("ingreso", 25_000);

        cuenta.Actualizar("Caja", null, 150_000, null, null, false);

        Assert.Equal("Caja", cuenta.Nombre);
        Assert.Equal(150_000, cuenta.SaldoInicial);
        Assert.Equal(175_000, cuenta.SaldoActual);
        Assert.False(cuenta.IncluidaEnTotal);
        Assert.Equal(3, cuenta.Version);
    }

    [Fact]
    public void RechazaActualizacionVacia()
    {
        var cuenta = Cuenta.Crear(Guid.CreateVersion7(), "Efectivo", "efectivo", 0);

        var exception = Assert.Throws<DomainException>(
            () => cuenta.Actualizar(null, null, null, null, null, null));

        Assert.Equal("actualizacion_vacia", exception.Code);
    }
}