using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0012_TransferenciasYAnalitica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analitica");

            migrationBuilder.AddColumn<Guid>(
                name: "transferencia_id",
                schema: "finanzas",
                table: "movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "alertas",
                schema: "analitica",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    que_ocurrio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    datos_utilizados = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    impacto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    recomendacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    clave_deduplicacion = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    leida = table.Column<bool>(type: "boolean", nullable: false),
                    leida_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archivada = table.Column<bool>(type: "boolean", nullable: false),
                    archivada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas", x => x.id);
                    table.ForeignKey(
                        name: "FK_alertas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transferencias",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    movimiento_egreso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movimiento_ingreso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    anulada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hash_idempotencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias", x => x.id);
                    table.CheckConstraint("ck_transferencias_cuentas", "cuenta_origen_id <> cuenta_destino_id");
                    table.CheckConstraint("ck_transferencias_moneda", "moneda = 'PYG'");
                    table.CheckConstraint("ck_transferencias_monto", "monto > 0");
                    table.CheckConstraint("ck_transferencias_movimientos", "(estado = 'procesando' AND movimiento_egreso_id IS NULL AND movimiento_ingreso_id IS NULL) OR (estado IN ('confirmada', 'anulada') AND movimiento_egreso_id IS NOT NULL AND movimiento_ingreso_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_transferencias_cuentas_cuenta_destino_id",
                        column: x => x.cuenta_destino_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_cuentas_cuenta_origen_id",
                        column: x => x.cuenta_origen_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_transferencia_id",
                schema: "finanzas",
                table: "movimientos",
                column: "transferencia_id");

            migrationBuilder.CreateIndex(
                name: "IX_alertas_usuario_id_clave_deduplicacion",
                schema: "analitica",
                table: "alertas",
                columns: new[] { "usuario_id", "clave_deduplicacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alertas_usuario_id_creado_en",
                schema: "analitica",
                table: "alertas",
                columns: new[] { "usuario_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_cuenta_destino_id",
                schema: "finanzas",
                table: "transferencias",
                column: "cuenta_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_cuenta_origen_id",
                schema: "finanzas",
                table: "transferencias",
                column: "cuenta_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_usuario_id_fecha_id",
                schema: "finanzas",
                table: "transferencias",
                columns: new[] { "usuario_id", "fecha", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_usuario_id_hash_idempotencia",
                schema: "finanzas",
                table: "transferencias",
                columns: new[] { "usuario_id", "hash_idempotencia" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_transferencias_transferencia_id",
                schema: "finanzas",
                table: "movimientos",
                column: "transferencia_id",
                principalSchema: "finanzas",
                principalTable: "transferencias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_transferencias_transferencia_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropTable(
                name: "alertas",
                schema: "analitica");

            migrationBuilder.DropTable(
                name: "transferencias",
                schema: "finanzas");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_transferencia_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "transferencia_id",
                schema: "finanzas",
                table: "movimientos");
        }
    }
}