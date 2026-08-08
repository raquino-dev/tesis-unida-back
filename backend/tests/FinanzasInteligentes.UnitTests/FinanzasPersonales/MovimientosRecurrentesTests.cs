using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class MovimientosRecurrentesTests
{
    [Fact]
    public void FrecuenciaMensualConservaAnclajeDeInicio()
    {
        var inicio = new DateOnly(2026, 1, 31);

        Assert.Equal(
            new DateOnly(2026, 2, 28),
            MovimientoRecurrente.CalcularEjecucion(inicio, "mensual", 1));
        Assert.Equal(
            new DateOnly(2026, 3, 31),
            MovimientoRecurrente.CalcularEjecucion(inicio, "mensual", 2));
    }

    [Fact]
    public void RecurrenciaFinalizaAlCompletarCantidad()
    {
        var usuarioId = Guid.CreateVersion7();
        var categoria = Categoria.Crear(usuarioId, "Servicios", "gasto");
        var recurrencia = MovimientoRecurrente.Crear(
            usuarioId, Guid.CreateVersion7(), "gasto", 180_000, "Internet",
            new DateOnly(2026, 7, 1), null, "mensual", 1, [categoria]);

        recurrencia.RegistrarEjecucion(DateTimeOffset.UtcNow);

        Assert.Equal(1, recurrencia.OcurrenciasCompletadas);
        Assert.Equal("finalizada", recurrencia.Estado);
        Assert.Equal(2, recurrencia.Version);
    }

    [Fact]
    public void RecurrenciaRechazaCategoriaIncompatible()
    {
        var usuarioId = Guid.CreateVersion7();
        var categoria = Categoria.Crear(usuarioId, "Salario", "ingreso");

        Assert.Throws<DomainException>(() => MovimientoRecurrente.Crear(
            usuarioId, Guid.CreateVersion7(), "gasto", 180_000, "Internet",
            new DateOnly(2026, 7, 1), null, "mensual", null, [categoria]));
    }

    [Fact]
    public void RecurrenciaPuedePausarseYReanudarsePeroNoReabrirseAlFinalizar()
    {
        var usuarioId = Guid.CreateVersion7();
        var categoria = Categoria.Crear(usuarioId, "Servicios", "gasto");
        var recurrencia = MovimientoRecurrente.Crear(
            usuarioId, Guid.CreateVersion7(), "gasto", 180_000, "Internet",
            new DateOnly(2026, 7, 1), null, "mensual", null, [categoria]);

        recurrencia.CambiarEstado("pausada");
        recurrencia.CambiarEstado("activa");
        recurrencia.CambiarEstado("finalizada");

        var exception = Assert.Throws<DomainException>(
            () => recurrencia.CambiarEstado("activa"));
        Assert.Equal("recurrencia_finalizada", exception.Code);
        Assert.Equal("finalizada", recurrencia.Estado);
    }

    [Fact]
    public void MovimientoRecurrenteExigePeriodoYRecurrenciaJuntos()
    {
        Assert.Throws<DomainException>(() => Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 100_000,
            "Servicio", new DateOnly(2026, 7, 1), "recurrencia"));
    }
}
