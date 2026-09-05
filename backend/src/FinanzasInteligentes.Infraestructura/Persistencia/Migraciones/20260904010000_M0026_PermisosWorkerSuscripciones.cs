using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>
/// Concede al Worker únicamente los permisos requeridos para generar avisos
/// de vencimiento de suscripciones.
/// </summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260904010000_M0026_PermisosWorkerSuscripciones")]
public partial class M0026_PermisosWorkerSuscripciones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $permisos$
            DECLARE
              rol text;
            BEGIN
              FOR rol IN
                SELECT rolname
                FROM pg_roles
                WHERE rolname = 'finanzas_worker' OR rolname LIKE 'finanzas_worker.%'
              LOOP
                EXECUTE format('GRANT USAGE ON SCHEMA suscripciones TO %I', rol);
                EXECUTE format(
                  'GRANT SELECT ON suscripciones.suscripciones, suscripciones.avisos TO %I',
                  rol);
                EXECUTE format('GRANT INSERT ON suscripciones.avisos TO %I', rol);
              END LOOP;
            END
            $permisos$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $permisos$
            DECLARE
              rol text;
            BEGIN
              FOR rol IN
                SELECT rolname
                FROM pg_roles
                WHERE rolname = 'finanzas_worker' OR rolname LIKE 'finanzas_worker.%'
              LOOP
                EXECUTE format('REVOKE INSERT ON suscripciones.avisos FROM %I', rol);
                EXECUTE format(
                  'REVOKE SELECT ON suscripciones.suscripciones, suscripciones.avisos FROM %I',
                  rol);
                EXECUTE format('REVOKE USAGE ON SCHEMA suscripciones FROM %I', rol);
              END LOOP;
            END
            $permisos$;
            """);
    }
}
