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
}