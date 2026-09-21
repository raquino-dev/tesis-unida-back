using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0030_OperacionesTarjeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "operacion_tarjeta",
                schema: "finanzas",
                table: "movimientos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Los movimientos de tarjeta anteriores a esta versión no distinguían
            // el motivo del ingreso. Se conservan como reintegros, que reproduce el
            // efecto histórico sobre la deuda sin debitar una cuenta bancaria.
            migrationBuilder.Sql(
                """
                UPDATE finanzas.movimientos
                SET operacion_tarjeta = CASE
                    WHEN tipo = 'gasto' THEN 'compra'
                    ELSE 'reintegro'
                END
                WHERE tarjeta_credito_id IS NOT NULL
                  AND operacion_tarjeta IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimientos_operacion_tarjeta",
                schema: "finanzas",
                table: "movimientos",
                sql: "(tarjeta_credito_id IS NULL AND operacion_tarjeta IS NULL) OR (tarjeta_credito_id IS NOT NULL AND ((tipo = 'gasto' AND operacion_tarjeta = 'compra') OR (tipo = 'ingreso' AND operacion_tarjeta IN ('reintegro', 'pago'))))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_movimientos_operacion_tarjeta",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "operacion_tarjeta",
                schema: "finanzas",
                table: "movimientos");
        }
    }
}
