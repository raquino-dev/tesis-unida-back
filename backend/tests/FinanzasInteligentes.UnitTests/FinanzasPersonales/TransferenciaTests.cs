using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class TransferenciaTests
{
    [Fact]
    public void NoPermiteTransferirALaMismaCuenta()
    {
        var cuentaId = Guid.CreateVersion7();
        Assert.Throws<DomainException>(() => Transferencia.Iniciar(
            Guid.CreateVersion7(), cuentaId, cuentaId, 100_000,
            new DateOnly(2026, 7, 25), "Traspaso", new string('a', 64)));
    }

    [Fact]
    public void ConfirmarYAnularConservaTrazabilidad()
    {
        var transferencia = Transferencia.Iniciar(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 100_000,
            new DateOnly(2026, 7, 25), "Traspaso", new string('a', 64));
        var egresoId = Guid.CreateVersion7();
        var ingresoId = Guid.CreateVersion7();

        transferencia.Confirmar(egresoId, ingresoId);
        transferencia.Anular();

        Assert.Equal("anulada", transferencia.Estado);
        Assert.Equal(egresoId, transferencia.MovimientoEgresoId);
        Assert.Equal(ingresoId, transferencia.MovimientoIngresoId);
        Assert.Equal(2, transferencia.Version);
    }

    [Fact]
    public void MovimientoDeTransferenciaExigeVinculo()
    {
        Assert.Throws<DomainException>(() => Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 100_000,
            "Traspaso", new DateOnly(2026, 7, 25), "transferencia"));
    }
}