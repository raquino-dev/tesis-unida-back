using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Suscripciones;

namespace FinanzasInteligentes.UnitTests.Suscripciones;

public sealed class SuscripcionTests
{
    [Fact]
    public void CancelarConservaFinDePeriodo()
    {
        var fin = DateTimeOffset.UtcNow.AddMonths(1);
        var suscripcion = Crear(fin);

        suscripcion.Cancelar("No la utilizaré este mes");

        Assert.Equal("cancelada", suscripcion.Estado);
        Assert.NotNull(suscripcion.CanceladaEn);
        Assert.Equal(fin, suscripcion.FinPeriodoEn);
        Assert.Equal(2, suscripcion.Version);
    }

    [Fact]
    public void RestaurarReactivaSuscripcionCancelada()
    {
        var suscripcion = Crear(DateTimeOffset.UtcNow.AddMonths(1));
        suscripcion.Cancelar(null);

        suscripcion.Restaurar(suscripcion.FinPeriodoEn);

        Assert.Equal("activa", suscripcion.Estado);
        Assert.Null(suscripcion.CanceladaEn);
        Assert.Equal(3, suscripcion.Version);
    }

    [Fact]
    public void SuscripcionActivaNoPuedeCancelarseDosVeces()
    {
        var suscripcion = Crear(DateTimeOffset.UtcNow.AddMonths(1));
        suscripcion.Cancelar(null);

        Assert.Throws<DomainException>(() => suscripcion.Cancelar(null));
    }

    [Fact]
    public void CambioDePlanRelacionaAmbasSuscripciones()
    {
        var anterior = Crear(DateTimeOffset.UtcNow.AddMonths(1));
        var nueva = Crear(DateTimeOffset.UtcNow.AddYears(1));

        nueva.VincularAnterior(anterior.Id);
        anterior.ReemplazarPor(nueva.Id);

        Assert.Equal("reemplazada", anterior.Estado);
        Assert.Equal(nueva.Id, anterior.ReemplazadaPorId);
        Assert.Equal(anterior.Id, nueva.SuscripcionAnteriorId);
    }

    [Theory]
    [InlineData("activa")]
    [InlineData("en_gracia")]
    [InlineData("cancelada")]
    [InlineData("expirada")]
    [InlineData("pausada")]
    public void SincronizaEstadosValidosDelProveedor(string estado)
    {
        var suscripcion = Crear(DateTimeOffset.UtcNow.AddMonths(1));

        suscripcion.SincronizarProveedor(estado, DateTimeOffset.UtcNow.AddMonths(2));

        Assert.Equal(estado, suscripcion.Estado);
    }

    private static Suscripcion Crear(DateTimeOffset fin) =>
        Suscripcion.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "interno",
            new string('a', 64), fin);
}
