using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.UnitTests.Analitica;

public sealed class AlertaFinancieraTests
{
    [Fact]
    public void MarcarComoLeidaYArchivarIncrementaVersion()
    {
        var alerta = Crear();

        alerta.Actualizar(true, true);

        Assert.True(alerta.Leida);
        Assert.True(alerta.Archivada);
        Assert.NotNull(alerta.LeidaEn);
        Assert.NotNull(alerta.ArchivadaEn);
        Assert.Equal(2, alerta.Version);
    }

    [Fact]
    public void RechazaActualizacionVacia()
    {
        Assert.Throws<DomainException>(() => Crear().Actualizar(null, null));
    }

    private static AlertaFinanciera Crear() => AlertaFinanciera.Crear(
        Guid.CreateVersion7(), "saldo-negativo", "critica", "Saldo negativo",
        "La cuenta quedó con saldo negativo.", "El saldo cruzó cero.",
        "Saldo actual.", "Riesgo de sobregiro.", "Regularice la cuenta.",
        "saldo-negativo:prueba");
}