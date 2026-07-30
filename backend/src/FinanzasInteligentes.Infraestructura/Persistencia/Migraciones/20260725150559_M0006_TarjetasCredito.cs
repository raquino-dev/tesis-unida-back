using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0006_TarjetasCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tarjetas_credito",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    emisor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ultimos_cuatro = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    dia_cierre = table.Column<long>(type: "bigint", nullable: false),
                    dia_vencimiento = table.Column<long>(type: "bigint", nullable: false),
                    limite_credito = table.Column<long>(type: "bigint", nullable: false),
                    saldo_utilizado = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarjetas_credito", x => x.id);
                    table.CheckConstraint("ck_tarjetas_credito_dias", "dia_cierre BETWEEN 1 AND 31 AND dia_vencimiento BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_tarjetas_credito_limites", "limite_credito >= 0 AND saldo_utilizado >= 0");
                    table.CheckConstraint("ck_tarjetas_credito_moneda", "moneda = 'PYG'");
                    table.ForeignKey(
                        name: "FK_tarjetas_credito_cuentas_cuenta_pago_id",
                        column: x => x.cuenta_pago_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tarjetas_credito_cuenta_pago_id",
                schema: "finanzas",
                table: "tarjetas_credito",
                column: "cuenta_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_tarjetas_credito_usuario_id_nombre",
                schema: "finanzas",
                table: "tarjetas_credito",
                columns: new[] { "usuario_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tarjetas_credito",
                schema: "finanzas");
        }
    }
}