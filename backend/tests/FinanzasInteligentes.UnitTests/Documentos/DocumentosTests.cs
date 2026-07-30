using FinanzasInteligentes.Dominio.Documentos;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Text.Json;

namespace FinanzasInteligentes.UnitTests.Documentos;

public sealed class DocumentosTests
{
    [Fact]
    public void DocumentoRechazaPropietarioIncoherente()
    {
        Assert.Throws<DomainException>(() => DocumentoFinanciero.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "privado", Guid.CreateVersion7(),
            "documentos/a", "factura.pdf", "pdf", "application/pdf", 100,
            new string('a', 64), new string('b', 64)));
    }

    [Fact]
    public void ProcesamientoCorregidoQuedaCompletadoYVersionado()
    {
        var proceso = ProcesamientoDocumental.Crear(
            Guid.CreateVersion7(), "ocr", new string('a', 64));
        proceso.Iniciar();
        proceso.Completar(new { monto = 1000 }, .5, "Revisar monto");

        proceso.Corregir(JsonDocument.Parse("""{"monto":1200}"""));

        Assert.Equal("completado", proceso.Estado);
        Assert.Empty(proceso.Advertencias);
        Assert.Equal(4, proceso.Version);
    }

    [Fact]
    public void ExportacionValidaFormatoYRango()
    {
        Assert.Throws<DomainException>(() => Exportacion.Crear(
            Guid.CreateVersion7(), "txt", "privado", null,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31),
            null, null, null, "cualquiera", new string('a', 64)));
    }
}