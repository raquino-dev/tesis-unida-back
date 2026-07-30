using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0009_Suscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "suscripciones");

            migrationBuilder.CreateTable(
                name: "planes",
                schema: "suscripciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    precio = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    periodicidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacidades = table.Column<string[]>(type: "text[]", nullable: false),
                    destacado = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suscripciones",
                schema: "suscripciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    hash_comprobante = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    iniciada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cancelada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fin_periodo_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo_cancelacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suscripciones", x => x.id);
                    table.CheckConstraint("ck_suscripciones_periodo", "fin_periodo_en > iniciada_en");
                    table.ForeignKey(
                        name: "FK_suscripciones_planes_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "suscripciones",
                        principalTable: "planes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_suscripciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "suscripciones",
                table: "planes",
                columns: new[] { "id", "activo", "capacidades", "codigo", "creado_en", "descripcion", "destacado", "moneda", "nombre", "periodicidad", "precio" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), true, new[] { "cuentas", "categorias", "movimientos" }, "gratis", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Funciones esenciales de finanzas personales.", false, "PYG", "Gratis", "sin-vencimiento", 0L },
                    { new Guid("00000000-0000-0000-0000-000000000102"), true, new[] { "ocr", "predicciones", "exportaciones", "alertas-prioritarias" }, "premium-mensual", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Todas las capacidades premium con renovación mensual.", true, "PYG", "Premium mensual", "mensual", 45000L },
                    { new Guid("00000000-0000-0000-0000-000000000103"), true, new[] { "ocr", "predicciones", "exportaciones", "alertas-prioritarias" }, "premium-anual", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Todas las capacidades premium con renovación anual.", false, "PYG", "Premium anual", "anual", 450000L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_planes_codigo",
                schema: "suscripciones",
                table: "planes",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripciones_plan_id",
                schema: "suscripciones",
                table: "suscripciones",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_suscripciones_proveedor_hash_comprobante",
                schema: "suscripciones",
                table: "suscripciones",
                columns: new[] { "proveedor", "hash_comprobante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripciones_usuario_id",
                schema: "suscripciones",
                table: "suscripciones",
                column: "usuario_id",
                unique: true,
                filter: "estado IN ('activa', 'en_gracia')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suscripciones",
                schema: "suscripciones");

            migrationBuilder.DropTable(
                name: "planes",
                schema: "suscripciones");
        }
    }
}