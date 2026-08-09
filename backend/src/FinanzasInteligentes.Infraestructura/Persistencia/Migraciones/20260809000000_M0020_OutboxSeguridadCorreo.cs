using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>
/// Evita reintentos infinitos del outbox y garantiza que cada correo pueda
/// reservarse una sola vez antes de llamar a un proveedor externo.
/// </summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260809000000_M0020_OutboxSeguridadCorreo")]
public partial class M0020_OutboxSeguridadCorreo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ultimo_error",
            schema: "infra",
            table: "outbox_eventos",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "outbox_entregas",
            schema: "infra",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                evento_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                canal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                destinatario_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reservada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                enviada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_entregas", x => x.id);
                table.ForeignKey(
                    name: "fk_outbox_entregas_outbox_eventos_evento_outbox_id",
                    column: x => x.evento_outbox_id,
                    principalSchema: "infra",
                    principalTable: "outbox_eventos",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_entregas_canal_reservada_en",
            schema: "infra",
            table: "outbox_entregas",
            columns: new[] { "canal", "reservada_en" });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_entregas_evento_outbox_id_canal",
            schema: "infra",
            table: "outbox_entregas",
            columns: new[] { "evento_outbox_id", "canal" },
            unique: true);

        migrationBuilder.Sql(
            """
            DO $permisos$
            BEGIN
              IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_worker') THEN
                GRANT SELECT, INSERT, UPDATE ON TABLE infra.outbox_entregas TO finanzas_worker;
                GRANT USAGE ON SCHEMA seguridad TO finanzas_worker;
                GRANT SELECT ON TABLE seguridad.dispositivos TO finanzas_worker;
              END IF;
            END
            $permisos$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $permisos$
            BEGIN
              IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_worker') THEN
                REVOKE SELECT, INSERT, UPDATE ON TABLE infra.outbox_entregas FROM finanzas_worker;
                REVOKE SELECT ON TABLE seguridad.dispositivos FROM finanzas_worker;
                REVOKE USAGE ON SCHEMA seguridad FROM finanzas_worker;
              END IF;
            END
            $permisos$;
            """);
        migrationBuilder.DropTable(name: "outbox_entregas", schema: "infra");
        migrationBuilder.DropColumn(name: "ultimo_error", schema: "infra", table: "outbox_eventos");
    }
}
