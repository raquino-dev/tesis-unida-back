using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0018_SuscripcionesCompletas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reemplazada_por_id",
                schema: "suscripciones",
                table: "suscripciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "suscripcion_anterior_id",
                schema: "suscripciones",
                table: "suscripciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avisos",
                schema: "suscripciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suscripcion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    periodo_fin_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avisos", x => x.id);
                    table.ForeignKey(
                        name: "FK_avisos_suscripciones_suscripcion_id",
                        column: x => x.suscripcion_id,
                        principalSchema: "suscripciones",
                        principalTable: "suscripciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transacciones",
                schema: "suscripciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suscripcion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    proveedor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    referencia_externa_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transacciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_transacciones_suscripciones_suscripcion_id",
                        column: x => x.suscripcion_id,
                        principalSchema: "suscripciones",
                        principalTable: "suscripciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_avisos_suscripcion_id_tipo_periodo_fin_en",
                schema: "suscripciones",
                table: "avisos",
                columns: new[] { "suscripcion_id", "tipo", "periodo_fin_en" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transacciones_proveedor_referencia_externa_hash_tipo",
                schema: "suscripciones",
                table: "transacciones",
                columns: new[] { "proveedor", "referencia_externa_hash", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transacciones_suscripcion_id",
                schema: "suscripciones",
                table: "transacciones",
                column: "suscripcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_transacciones_usuario_id_ocurrido_en",
                schema: "suscripciones",
                table: "transacciones",
                columns: new[] { "usuario_id", "ocurrido_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avisos",
                schema: "suscripciones");

            migrationBuilder.DropTable(
                name: "transacciones",
                schema: "suscripciones");

            migrationBuilder.DropColumn(
                name: "reemplazada_por_id",
                schema: "suscripciones",
                table: "suscripciones");

            migrationBuilder.DropColumn(
                name: "suscripcion_anterior_id",
                schema: "suscripciones",
                table: "suscripciones");
        }
    }
}
