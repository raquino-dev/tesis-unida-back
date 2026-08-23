using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>Acceso mínimo para los esquemas del piloto y la sincronización.</summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260818010000_M0023_PermisosPilotoSincronizacion")]
public partial class M0023_PermisosPilotoSincronizacion : Migration
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
                WHERE rolname = 'finanzas_api' OR rolname LIKE 'finanzas_api.%'
              LOOP
                EXECUTE format('GRANT USAGE ON SCHEMA piloto, sincronizacion TO %I', rol);
                EXECUTE format('GRANT SELECT ON piloto.instrumentos, piloto.preguntas TO %I', rol);
                EXECUTE format('GRANT SELECT, INSERT ON piloto.respuestas, piloto.respuestas_detalle TO %I', rol);
                EXECUTE format('GRANT SELECT, INSERT ON sincronizacion.cambios TO %I', rol);
                EXECUTE format('GRANT USAGE ON SEQUENCE sincronizacion.cambios_secuencia_seq TO %I', rol);
              END LOOP;

              FOR rol IN
                SELECT rolname
                FROM pg_roles
                WHERE rolname = 'finanzas_worker' OR rolname LIKE 'finanzas_worker.%'
              LOOP
                EXECUTE format('GRANT USAGE ON SCHEMA sincronizacion TO %I', rol);
                EXECUTE format('GRANT INSERT ON sincronizacion.cambios TO %I', rol);
                EXECUTE format('GRANT USAGE ON SEQUENCE sincronizacion.cambios_secuencia_seq TO %I', rol);
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
                WHERE rolname = 'finanzas_api' OR rolname LIKE 'finanzas_api.%'
              LOOP
                EXECUTE format('REVOKE USAGE ON SEQUENCE sincronizacion.cambios_secuencia_seq FROM %I', rol);
                EXECUTE format('REVOKE SELECT, INSERT ON sincronizacion.cambios FROM %I', rol);
                EXECUTE format('REVOKE SELECT, INSERT ON piloto.respuestas, piloto.respuestas_detalle FROM %I', rol);
                EXECUTE format('REVOKE SELECT ON piloto.instrumentos, piloto.preguntas FROM %I', rol);
                EXECUTE format('REVOKE USAGE ON SCHEMA piloto, sincronizacion FROM %I', rol);
              END LOOP;

              FOR rol IN
                SELECT rolname
                FROM pg_roles
                WHERE rolname = 'finanzas_worker' OR rolname LIKE 'finanzas_worker.%'
              LOOP
                EXECUTE format('REVOKE USAGE ON SEQUENCE sincronizacion.cambios_secuencia_seq FROM %I', rol);
                EXECUTE format('REVOKE INSERT ON sincronizacion.cambios FROM %I', rol);
                EXECUTE format('REVOKE USAGE ON SCHEMA sincronizacion FROM %I', rol);
              END LOOP;
            END
            $permisos$;
            """);
    }
}
