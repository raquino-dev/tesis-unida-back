using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0011_MovimientosRecurrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "periodo_recurrencia",
                schema: "finanzas",
                table: "movimientos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "recurrencia_id",
                schema: "finanzas",
                table: "movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurrencias",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    frecuencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cantidad_ocurrencias = table.Column<long>(type: "bigint", nullable: true),
                    ocurrencias_completadas = table.Column<long>(type: "bigint", nullable: false),
                    proxima_ejecucion = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ultima_ejecucion_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurrencias", x => x.id);
                    table.CheckConstraint("ck_recurrencias_fechas", "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");
                    table.CheckConstraint("ck_recurrencias_frecuencia", "frecuencia IN ('diaria', 'semanal', 'quincenal', 'mensual', 'anual')");
                    table.CheckConstraint("ck_recurrencias_moneda", "moneda = 'PYG'");
                    table.CheckConstraint("ck_recurrencias_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_recurrencias_cuentas_cuenta_id",
                        column: x => x.cuenta_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurrencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurrencia_categorias",
                schema: "finanzas",
                columns: table => new
                {
                    recurrencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurrencia_categorias", x => new { x.recurrencia_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_recurrencia_categorias_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "finanzas",
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurrencia_categorias_recurrencias_recurrencia_id",
                        column: x => x.recurrencia_id,
                        principalSchema: "finanzas",
                        principalTable: "recurrencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_recurrencia_id_periodo_recurrencia",
                schema: "finanzas",
                table: "movimientos",
                columns: new[] { "recurrencia_id", "periodo_recurrencia" },
                unique: true,
                filter: "recurrencia_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_recurrencia_categorias_categoria_id",
                schema: "finanzas",
                table: "recurrencia_categorias",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurrencias_cuenta_id",
                schema: "finanzas",
                table: "recurrencias",
                column: "cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurrencias_estado_proxima_ejecucion",
                schema: "finanzas",
                table: "recurrencias",
                columns: new[] { "estado", "proxima_ejecucion" });

            migrationBuilder.CreateIndex(
                name: "IX_recurrencias_usuario_id_cuenta_id_descripcion",
                schema: "finanzas",
                table: "recurrencias",
                columns: new[] { "usuario_id", "cuenta_id", "descripcion" },
                unique: true,
                filter: "estado = 'activa' AND eliminado_en IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_recurrencias_recurrencia_id",
                schema: "finanzas",
                table: "movimientos",
                column: "recurrencia_id",
                principalSchema: "finanzas",
                principalTable: "recurrencias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_recurrencias_recurrencia_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropTable(
                name: "recurrencia_categorias",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "recurrencias",
                schema: "finanzas");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_recurrencia_id_periodo_recurrencia",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "periodo_recurrencia",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "recurrencia_id",
                schema: "finanzas",
                table: "movimientos");
        }
    }
}