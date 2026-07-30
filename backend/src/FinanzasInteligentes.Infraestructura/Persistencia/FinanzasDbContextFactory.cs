using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanzasInteligentes.Infraestructura.Persistencia;

public sealed class FinanzasDbContextFactory : IDesignTimeDbContextFactory<FinanzasDbContext>
{
    public FinanzasDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FINANZAS_MIGRACIONES_POSTGRESQL")
            ?? throw new InvalidOperationException(
                "Falta FINANZAS_MIGRACIONES_POSTGRESQL para ejecutar migraciones.");

        var options = new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra"))
            .Options;

        return new FinanzasDbContext(options);
    }
}
