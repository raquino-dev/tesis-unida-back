using FinanzasInteligentes.Infraestructura.Correo;
using System.Net;

namespace FinanzasInteligentes.ContractTests.Correo;

public sealed class PlantillasCorreoSuscripcionTests
{
    [Fact]
    public void PagoProcesadoIncluyeDatosYEscapaContenidoDelUsuario()
    {
        var contenido = PlantillasCorreoSuscripcion.PagoProcesado(
            "Rodrigo <script>",
            "Premium mensual",
            new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero),
            "Google Play",
            "https://finanza.example/app");

        Assert.Equal("¡Gracias por tu suscripción!", contenido.Asunto);
        Assert.Contains("PAGO PROCESADO", contenido.Html);
        Assert.Contains("20 de octubre de 2026", contenido.Html);
        Assert.Contains("Google Play", contenido.Html);
        Assert.Contains("Rodrigo &lt;script&gt;", contenido.Html);
        Assert.DoesNotContain("Rodrigo <script>", contenido.Html);
    }

    [Fact]
    public void CancelacionInformaHastaCuandoContinuaElAcceso()
    {
        var contenido = PlantillasCorreoSuscripcion.Cancelacion(
            "Ana",
            "Premium anual",
            new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 8, 20, 12, 0, 0, TimeSpan.Zero),
            "https://finanza.example/app");

        Assert.Equal("Tu suscripción ha sido cancelada", contenido.Asunto);
        Assert.Contains("SUSCRIPCIÓN CANCELADA", WebUtility.HtmlDecode(contenido.Html));
        Assert.Contains("Acceso disponible hasta", contenido.Html);
        Assert.Contains("20 de agosto de 2027", contenido.Html);
        Assert.Contains("Podés volver a suscribirte", WebUtility.HtmlDecode(contenido.Html));
    }
}
