using FinanzasInteligentes.Aplicacion.Analitica;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ActualizarMovimiento;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.AnularMovimiento;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.CrearMovimiento;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Infraestructura.Persistencia;
using FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanzasInteligentes.IntegrationTests.FinanzasPersonales;

public sealed class OperacionesTarjetaPostgreSqlTests
{
    [PostgreSqlFact]
    public async Task CompraReintegroYPagoMantienenSaldosYAnaliticaConsistentes()
    {
        var connectionString = Environment.GetEnvironmentVariable("FINANZAS_INTEGRATION_POSTGRESQL")!;
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        Assert.Contains("integration", databaseName, StringComparison.OrdinalIgnoreCase);

        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var db = new FinanzasDbContext(options);
        await db.Database.EnsureDeletedAsync();

        try
        {
            await db.Database.MigrateAsync();

            var usuario = Usuario.Crear(
                "tarjetas.integration@example.com", "Integración", "hash-seguro",
                alias: "tarjetas_test");
            var cuentaVinculada = Cuenta.Crear(
                usuario.Id, "Cuenta vinculada", "cuenta-ahorro", 1_000_000);
            var cuentaDePago = Cuenta.Crear(
                usuario.Id, "Cuenta de pago", "cuenta-corriente", 500_000);
            var tarjeta = TarjetaCredito.Crear(
                usuario.Id, "Tarjeta integración", cuentaVinculada.Id,
                15, 25, 2_000_000, "PYG", "#008F66");

            db.Usuarios.Add(usuario);
            db.Cuentas.AddRange(cuentaVinculada, cuentaDePago);
            db.TarjetasCredito.Add(tarjeta);
            await db.SaveChangesAsync();

            var fecha = new DateOnly(2026, 9, 21);
            var crear = CrearHandler(db);
            await crear.Handle(new(
                usuario.Id, "test-compra", "privado", cuentaVinculada.Id,
                "gasto", 300_000, "Compra con tarjeta", fecha,
                TarjetaCreditoId: tarjeta.Id, OperacionTarjeta: "compra"), default);
            await crear.Handle(new(
                usuario.Id, "test-reintegro", "privado", cuentaVinculada.Id,
                "ingreso", 50_000, "Reintegro de compra", fecha,
                TarjetaCreditoId: tarjeta.Id, OperacionTarjeta: "reintegro"), default);
            var pago = await crear.Handle(new(
                usuario.Id, "test-pago", "privado", cuentaDePago.Id,
                "ingreso", 100_000, "Pago de tarjeta", fecha,
                TarjetaCreditoId: tarjeta.Id, OperacionTarjeta: "pago"), default);

            db.ChangeTracker.Clear();
            await AssertSaldos(db, tarjeta.Id, 150_000,
                cuentaVinculada.Id, 1_000_000, cuentaDePago.Id, 400_000);
            await AssertAnalitica(db, usuario.Id, fecha, 0, 250_000);

            var actualizar = ActualizarHandler(db);
            var pagoActualizado = await actualizar.Handle(new(
                usuario.Id, pago.Id, pago.Version, "Pago ajustado", null, null,
                "test-pago-actualizado", Monto: 120_000), default);

            db.ChangeTracker.Clear();
            await AssertSaldos(db, tarjeta.Id, 130_000,
                cuentaVinculada.Id, 1_000_000, cuentaDePago.Id, 380_000);

            var anular = AnularHandler(db);
            await anular.Handle(new(
                usuario.Id, pago.Id, pagoActualizado.Version, "test-pago-anulado"), default);

            db.ChangeTracker.Clear();
            await AssertSaldos(db, tarjeta.Id, 250_000,
                cuentaVinculada.Id, 1_000_000, cuentaDePago.Id, 500_000);
            await AssertAnalitica(db, usuario.Id, fecha, 0, 250_000);
            Assert.Equal("anulado", (await db.Movimientos.SingleAsync(x => x.Id == pago.Id)).Estado);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static CrearMovimientoHandler CrearHandler(FinanzasDbContext db) =>
        new(new FinanzasRepository(db), new DocumentosRepository(db), new UnidadDeTrabajo(db));

    private static ActualizarMovimientoHandler ActualizarHandler(FinanzasDbContext db) =>
        new(new FinanzasRepository(db), new DocumentosRepository(db), new UnidadDeTrabajo(db));

    private static AnularMovimientoHandler AnularHandler(FinanzasDbContext db) =>
        new(new FinanzasRepository(db), new UnidadDeTrabajo(db));

    private static async Task AssertSaldos(
        FinanzasDbContext db,
        Guid tarjetaId,
        long saldoTarjeta,
        Guid cuentaVinculadaId,
        long saldoCuentaVinculada,
        Guid cuentaPagoId,
        long saldoCuentaPago)
    {
        Assert.Equal(saldoTarjeta,
            (await db.TarjetasCredito.AsNoTracking().SingleAsync(x => x.Id == tarjetaId)).SaldoUtilizado);
        Assert.Equal(saldoCuentaVinculada,
            (await db.Cuentas.AsNoTracking().SingleAsync(x => x.Id == cuentaVinculadaId)).SaldoActual);
        Assert.Equal(saldoCuentaPago,
            (await db.Cuentas.AsNoTracking().SingleAsync(x => x.Id == cuentaPagoId)).SaldoActual);
    }

    private static async Task AssertAnalitica(
        FinanzasDbContext db,
        Guid usuarioId,
        DateOnly fecha,
        long ingresos,
        long gastos)
    {
        var handler = new DashboardYReportesHandler(new FinanzasRepository(db));
        var reporte = await handler.ObtenerReporte(
            usuarioId, "personalizado", fecha, fecha, null, null, null, default);
        Assert.Equal(ingresos, reporte.Ingresos);
        Assert.Equal(gastos, reporte.Gastos);
        Assert.Equal(ingresos - gastos, reporte.Balance);
    }
}

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("FINANZAS_INTEGRATION_POSTGRESQL")))
            Skip = "Requiere FINANZAS_INTEGRATION_POSTGRESQL y una instancia PostgreSQL desechable.";
    }
}
