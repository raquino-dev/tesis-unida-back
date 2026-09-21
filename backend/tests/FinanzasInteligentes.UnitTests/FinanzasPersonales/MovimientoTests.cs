using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class MovimientoTests
{
    [Fact]
    public void UnaAnulacionEsUnica()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "gasto",
            10_000,
            "Compra",
            new DateOnly(2026, 7, 23));

        movimiento.Anular("Corrección");

        var exception = Assert.Throws<DomainException>(() => movimiento.Anular("Otra"));
        Assert.Equal("movimiento_anulado", exception.Code);
        Assert.Equal("anulado", movimiento.Estado);
    }

    [Fact]
    public void ActualizarDescripcionIncrementaVersion()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 1000,
            "Compra", DateOnly.FromDateTime(DateTime.UtcNow));

        movimiento.Actualizar("Compra actualizada", null);

        Assert.Equal("Compra actualizada", movimiento.Descripcion);
        Assert.Equal(2, movimiento.Version);
    }

    [Fact]
    public void MovimientoAnuladoNoPuedeActualizarse()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 1000,
            "Compra", DateOnly.FromDateTime(DateTime.UtcNow));
        movimiento.Anular("Duplicado");

        Assert.Throws<DomainException>(
            () => movimiento.Actualizar("Cambio", null));
    }

    [Fact]
    public void ActualizarReemplazaCategorias()
    {
        var usuarioId = Guid.CreateVersion7();
        var movimiento = Movimiento.Crear(
            usuarioId, Guid.CreateVersion7(), "gasto", 1000,
            "Compra", DateOnly.FromDateTime(DateTime.UtcNow));
        var categoriaInicial = Categoria.Crear(
            usuarioId, "Alimentos", "gasto", "restaurant", "#6868A6");
        var categoriaNueva = Categoria.Crear(
            usuarioId, "Hogar", "gasto", "home", "#112233");
        movimiento.AsignarCategoriasIniciales([categoriaInicial]);

        movimiento.Actualizar(null, [categoriaNueva]);

        Assert.Collection(
            movimiento.Categorias,
            categoria => Assert.Equal(categoriaNueva.Id, categoria.Id));
        Assert.Equal(2, movimiento.Version);
    }

    [Fact]
    public void ActualizarPuedeVincularUnComprobante()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 1000,
            "Compra", DateOnly.FromDateTime(DateTime.UtcNow));
        var documentoId = Guid.CreateVersion7();

        movimiento.Actualizar(null, null, documentoId);

        Assert.Equal(documentoId, movimiento.DocumentoId);
        Assert.Equal(2, movimiento.Version);
    }

    [Fact]
    public void ActualizarImporteCuentaYFechaIncrementaVersion()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 1000,
            "Compra", new DateOnly(2026, 9, 10));
        var nuevaCuenta = Guid.CreateVersion7();

        movimiento.Actualizar(null, null, cuentaId: nuevaCuenta,
            tipo: "ingreso", monto: 2500, fecha: new DateOnly(2026, 9, 11));

        Assert.Equal(nuevaCuenta, movimiento.CuentaId);
        Assert.Equal("ingreso", movimiento.Tipo);
        Assert.Equal(2500, movimiento.Monto);
        Assert.Equal(new DateOnly(2026, 9, 11), movimiento.Fecha);
        Assert.Equal(2, movimiento.Version);
    }

    [Fact]
    public void ConservaYActualizaLaHoraDelMovimiento()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "gasto", 1000,
            "Compra", new DateOnly(2026, 9, 10),
            hora: new TimeOnly(14, 25));

        Assert.Equal(new TimeOnly(14, 25), movimiento.Hora);

        movimiento.Actualizar(null, null, hora: new TimeOnly(16, 40));

        Assert.Equal(new TimeOnly(16, 40), movimiento.Hora);
        Assert.Equal(2, movimiento.Version);
    }

    [Theory]
    [InlineData("gasto", null, "compra", 0, 1000)]
    [InlineData("ingreso", null, "reintegro", 0, -1000)]
    [InlineData("ingreso", "pago", "pago", 0, 0)]
    public void ClasificaOperacionesDeTarjetaSinInflarIngresos(
        string tipo,
        string? operacionSolicitada,
        string operacionEsperada,
        long ingresoEsperado,
        long gastoEsperado)
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), tipo, 1000,
            "Operación de tarjeta", DateOnly.FromDateTime(DateTime.UtcNow),
            tarjetaCreditoId: Guid.CreateVersion7(),
            operacionTarjeta: operacionSolicitada);

        Assert.Equal(operacionEsperada, movimiento.OperacionTarjeta);
        Assert.Equal(ingresoEsperado, movimiento.MontoIngresoAnalitico());
        Assert.Equal(gastoEsperado, movimiento.MontoGastoAnalitico());
    }

    [Fact]
    public void TransferenciaInternaNoEsIngresoNiGastoAnalitico()
    {
        var movimiento = Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "ingreso", 1000,
            "Transferencia", DateOnly.FromDateTime(DateTime.UtcNow),
            origen: "transferencia", transferenciaId: Guid.CreateVersion7());

        Assert.Equal(0, movimiento.MontoIngresoAnalitico());
        Assert.Equal(0, movimiento.MontoGastoAnalitico());
    }

    [Fact]
    public void RechazaPagoSinTarjeta()
    {
        var exception = Assert.Throws<DomainException>(() => Movimiento.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "ingreso", 1000,
            "Pago", DateOnly.FromDateTime(DateTime.UtcNow),
            operacionTarjeta: "pago"));

        Assert.Equal("operacion_tarjeta_invalida", exception.Code);
    }
}
