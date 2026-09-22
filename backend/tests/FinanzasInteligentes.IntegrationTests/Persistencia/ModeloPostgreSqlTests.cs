using FinanzasInteligentes.Infraestructura.Persistencia;
using FinanzasInteligentes.Dominio.Piloto;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
        Assert.Equal("infra", db.Model.FindEntityType("FinanzasInteligentes.Dominio.Infraestructura.Entidades.EntregaOutbox")?.GetSchema());
    }

    [Fact]
    public void ModeloMapeaFechasDeCreacionDelPilotoEnSnakeCase()
    {
        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo;Username=modelo")
            .Options;
        using var db = new FinanzasDbContext(options);

        AssertCreadoEn<PreguntaInstrumentoPiloto>(db);
        AssertCreadoEn<RespuestaInstrumentoPiloto>(db);
        AssertCreadoEn<DetalleRespuestaInstrumentoPiloto>(db);
    }

    [Fact]
    public void MovimientoVinculaTarjetaDeCreditoOpcional()
    {
        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo;Username=modelo")
            .Options;
        using var db = new FinanzasDbContext(options);
        var entity = db.Model.FindEntityType(typeof(Movimiento));
        Assert.NotNull(entity);
        var table = StoreObjectIdentifier.Table(entity!.GetTableName()!, entity.GetSchema());
        Assert.Equal("tarjeta_credito_id",
            entity.FindProperty(nameof(Movimiento.TarjetaCreditoId))?.GetColumnName(table));
        Assert.Equal("hora",
            entity.FindProperty(nameof(Movimiento.Hora))?.GetColumnName(table));
        Assert.Equal("operacion_tarjeta",
            entity.FindProperty(nameof(Movimiento.OperacionTarjeta))?.GetColumnName(table));
        var designEntity = db.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Movimiento));
        Assert.NotNull(designEntity);
        Assert.Contains(designEntity!.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_movimientos_operacion_tarjeta");
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(TarjetaCredito) &&
            fk.Properties.Single().Name == nameof(Movimiento.TarjetaCreditoId));
    }

    [Fact]
    public void CursorDeMovimientosSeTraduceAPostgreSql()
    {
        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo;Username=modelo")
            .Options;
        using var db = new FinanzasDbContext(options);
        var fecha = new DateOnly(2026, 9, 20);
        var id = Guid.Parse("01900000-0000-7000-8000-000000000001");

        var sql = db.Movimientos
            .Where(x => x.Fecha < fecha ||
                (x.Fecha == fecha && x.Id.CompareTo(id) < 0))
            .ToQueryString();

        Assert.Contains("fecha", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertCreadoEn<TEntity>(FinanzasDbContext db)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        var table = StoreObjectIdentifier.Table(entity!.GetTableName()!, entity.GetSchema());
        Assert.Equal("creado_en", entity.FindProperty("CreadoEn")?.GetColumnName(table));
    }
}
