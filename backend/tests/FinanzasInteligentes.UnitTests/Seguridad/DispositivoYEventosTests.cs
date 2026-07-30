using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Seguridad;

namespace FinanzasInteligentes.UnitTests.Seguridad;

public sealed class DispositivoYEventosTests
{
    [Fact]
    public void DispositivoNoExponeTokenYPuedeRevocarse()
    {
        var dispositivo = Crear();

        dispositivo.Revocar();

        Assert.False(dispositivo.Confiable);
        Assert.Empty(dispositivo.TokenPushProtegido);
        Assert.NotNull(dispositivo.RevocadoEn);
        Assert.Equal(2, dispositivo.Version);
    }

    [Fact]
    public void ActualizacionVaciaEsInvalida()
    {
        Assert.Throws<DomainException>(() =>
            Crear().Actualizar(null, null, null, null));
    }

    [Fact]
    public void EventoDeSeguridadEsInmutableYSanitizado()
    {
        var evento = EventoSeguridad.Crear(
            Guid.CreateVersion7(), "inicio-sesion", "Inicio exitoso.", true);

        Assert.True(evento.Exitoso);
        Assert.Equal("No disponible", evento.OrigenAproximado);
        Assert.Equal("Desconocido", evento.Dispositivo);
    }

    private static Dispositivo Crear() => Dispositivo.Crear(
        Guid.CreateVersion7(), "instalacion-1", "Pixel", "android",
        "15", "1.0.0", "token-protegido", "America/Asuncion");
}