using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class CategoriaTests
{
    [Fact]
    public void ActualizarModificaCamposYVersion()
    {
        var categoria = Categoria.Crear(Guid.CreateVersion7(), "Comida", "gasto");

        categoria.Actualizar("Alimentación", "ambos", "restaurant", "#6868A6");

        Assert.Equal("Alimentación", categoria.Nombre);
        Assert.Equal("ambos", categoria.Tipo);
        Assert.Equal("restaurant", categoria.Icono);
        Assert.Equal("#6868A6", categoria.Color);
        Assert.Equal(2, categoria.Version);
    }

    [Fact]
    public void EliminarRealizaBorradoLogico()
    {
        var categoria = Categoria.Crear(Guid.CreateVersion7(), "Comida", "gasto");

        categoria.Eliminar();

        Assert.NotNull(categoria.EliminadoEn);
        Assert.Equal(2, categoria.Version);
    }

    [Fact]
    public void RechazaActualizacionVacia()
    {
        var categoria = Categoria.Crear(Guid.CreateVersion7(), "Comida", "gasto");

        var exception = Assert.Throws<DomainException>(
            () => categoria.Actualizar(null, null, null, null));

        Assert.Equal("actualizacion_vacia", exception.Code);
    }
}