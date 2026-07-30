using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.UnitTests.FinanzasPersonales;

public sealed class PlanificacionFinancieraTests
{
    [Fact]
    public void PresupuestoRequiereCategorias()
    {
        Assert.Throws<DomainException>(() => Presupuesto.Crear(
            Guid.CreateVersion7(), "Alimentación", 1_000_000, "mensual", []));
    }

    [Fact]
    public void PresupuestoActualizaMontoYVersion()
    {
        var usuarioId = Guid.CreateVersion7();
        var categoria = Categoria.Crear(usuarioId, "Alimentación", "gasto");
        var presupuesto = Presupuesto.Crear(
            usuarioId, "Alimentación", 1_000_000, "mensual", [categoria]);

        presupuesto.Actualizar(null, 1_500_000, null, null);

        Assert.Equal(1_500_000, presupuesto.Monto);
        Assert.Equal(2, presupuesto.Version);
    }

    [Fact]
    public void MetaPrivadaNoAceptaGrupoFamiliar()
    {
        Assert.Throws<DomainException>(() => MetaAhorro.Crear(
            Guid.CreateVersion7(), "privado", Guid.CreateVersion7(), "Viaje",
            5_000_000, DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6),
            Guid.CreateVersion7()));
    }

    [Fact]
    public void MetaActualizaObjetivoYVersion()
    {
        var meta = MetaAhorro.Crear(
            Guid.CreateVersion7(), "privado", null, "Viaje",
            5_000_000, DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6),
            Guid.CreateVersion7());

        meta.Actualizar("Vacaciones", 6_000_000, null);

        Assert.Equal("Vacaciones", meta.Nombre);
        Assert.Equal(6_000_000, meta.MontoObjetivo);
        Assert.Equal(2, meta.Version);
    }

    [Fact]
    public void AporteRechazaMontoNoPositivo()
    {
        Assert.Throws<DomainException>(() => AporteMeta.Crear(
            Guid.CreateVersion7(), 0, Guid.CreateVersion7(), "Aporte",
            Guid.CreateVersion7(), new string('a', 64)));
    }
}