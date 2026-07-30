using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Infraestructura.Archivos;
using Microsoft.Extensions.Options;
using System.Text;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class ValidacionDocumentosTests
{
    private readonly ValidadorDocumento _validator =
        new(Options.Create(new DocumentoOptions()));

    [Fact]
    public void AceptaPdfConFirmaMimeYExtensionCoherentes() =>
        _validator.Validar(
            "pdf", "application/pdf", "comprobante.pdf",
            Encoding.ASCII.GetBytes("%PDF-1.7\ncontenido"));

    [Fact]
    public void RechazaPdfFalsoAunqueDeclareMimeCorrecto() =>
        Assert.Throws<DomainException>(() =>
            _validator.Validar(
                "pdf", "application/pdf", "comprobante.pdf",
                Encoding.UTF8.GetBytes("<script>contenido malicioso</script>")));

    [Fact]
    public void RechazaExtensionQueNoCorrespondeAlContenido() =>
        Assert.Throws<DomainException>(() =>
            _validator.Validar(
                "imagen", "image/png", "imagen.jpg",
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));

    [Fact]
    public void RechazaXmlConDoctype() =>
        Assert.Throws<DomainException>(() =>
            _validator.Validar(
                "xml-sifen", "application/xml", "factura.xml",
                Encoding.UTF8.GetBytes("<!DOCTYPE foo><factura/>")));

    [Fact]
    public void RechazaNombreConRecorridoDeRuta() =>
        Assert.Throws<DomainException>(() =>
            _validator.Validar(
                "pdf", "application/pdf", "../comprobante.pdf",
                Encoding.ASCII.GetBytes("%PDF-1.7")));
}
