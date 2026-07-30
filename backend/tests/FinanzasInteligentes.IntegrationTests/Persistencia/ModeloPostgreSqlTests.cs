using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.IntegrationTests.Persistencia;

public sealed class ModeloPostgreSqlTests
{
    [Fact]
    public void ModeloAsignaEntidadesASusEsquemas()
    {
        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo;Username=modelo")
            .Options;
        using var db = new FinanzasDbContext(options);

        Assert.Equal("identidad", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Identidad.Usuario")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.Cuenta")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.TarjetaCredito")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.Presupuesto")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.MetaAhorro")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.AporteMeta")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.MovimientoRecurrente")?.GetSchema());
        Assert.Equal("finanzas", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasPersonales.Transferencia")?.GetSchema());
        Assert.Equal("analitica", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Analitica.AlertaFinanciera")?.GetSchema());
        Assert.Equal("documentos", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Documentos.DocumentoFinanciero")?.GetSchema());
        Assert.Equal("documentos", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Documentos.ProcesamientoDocumental")?.GetSchema());
        Assert.Equal("documentos", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Documentos.Exportacion")?.GetSchema());
        Assert.Equal("seguridad", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Seguridad.Dispositivo")?.GetSchema());
        Assert.Equal("seguridad", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Seguridad.EventoSeguridad")?.GetSchema());
        Assert.Equal("auditoria", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Seguridad.EventoAuditoria")?.GetSchema());
        Assert.Equal("familias", db.Model.FindEntityType("FinanzasInteligentes.Dominio.FinanzasFamiliares.GrupoFamiliar")?.GetSchema());
        Assert.Equal("suscripciones", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Suscripciones.Suscripcion")?.GetSchema());
        Assert.Equal("infra", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Infraestructura.Entidades.EventoOutbox")?.GetSchema());
    }
}