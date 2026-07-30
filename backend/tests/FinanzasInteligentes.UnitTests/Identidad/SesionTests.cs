using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.UnitTests.Identidad;

public sealed class SesionTests
{
    [Fact]
    public void MarcarUsadaImpideReutilizarRefreshToken()
    {
        var sesion = CrearSesion();

        sesion.MarcarUsada();

        var exception = Assert.Throws<DomainException>(sesion.MarcarUsada);
        Assert.Equal("refresh_token_invalido", exception.Code);
    }

    [Fact]
    public void RevocarEsIdempotente()
    {
        var sesion = CrearSesion();

        sesion.Revocar();
        var revocadaEn = sesion.RevocadoEn;
        sesion.Revocar();

        Assert.NotNull(revocadaEn);
        Assert.Equal(revocadaEn, sesion.RevocadoEn);
    }

    private static Sesion CrearSesion() =>
        Sesion.Crear(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "hash",
            DateTimeOffset.UtcNow.AddDays(30),
            "android-installation-id",
            "Pixel 8",
            "android");
}