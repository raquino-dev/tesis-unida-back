using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0013_DocumentosYExportaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "documentos");

            migrationBuilder.AddColumn<Guid>(
                name: "documento_id",
                schema: "finanzas",
                table: "movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "archivos",
                schema: "documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clave_objeto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    nombre_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamano_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash_idempotencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archivos", x => x.id);
                    table.CheckConstraint("ck_archivos_tamano", "tamano_bytes > 0 AND tamano_bytes <= 10485760");
                    table.ForeignKey(
                        name: "FK_archivos_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_archivos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exportaciones",
                schema: "documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    desde = table.Column<DateOnly>(type: "date", nullable: false),
                    hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hash_solicitud = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    clave_objeto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    finalizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cantidad_movimientos = table.Column<long>(type: "bigint", nullable: false),
                    total_ingresos = table.Column<long>(type: "bigint", nullable: false),
                    total_gastos = table.Column<long>(type: "bigint", nullable: false),
                    total_transferido = table.Column<long>(type: "bigint", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exportaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_exportaciones_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exportaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procesamientos",
                schema: "documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    archivo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proveedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version_modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    confianza = table.Column<double>(type: "double precision", nullable: true),
                    resultado = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    advertencias = table.Column<string[]>(type: "text[]", nullable: false),
                    iniciado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hash_idempotencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procesamientos", x => x.id);
                    table.CheckConstraint("ck_procesamientos_confianza", "confianza IS NULL OR (confianza >= 0 AND confianza <= 1)");
                    table.ForeignKey(
                        name: "FK_procesamientos_archivos_archivo_id",
                        column: x => x.archivo_id,
                        principalSchema: "documentos",
                        principalTable: "archivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_documento_id",
                schema: "finanzas",
                table: "movimientos",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "IX_archivos_clave_objeto",
                schema: "documentos",
                table: "archivos",
                column: "clave_objeto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_archivos_grupo_familiar_id",
                schema: "documentos",
                table: "archivos",
                column: "grupo_familiar_id");

            migrationBuilder.CreateIndex(
                name: "IX_archivos_usuario_id_hash_idempotencia",
                schema: "documentos",
                table: "archivos",
                columns: new[] { "usuario_id", "hash_idempotencia" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_archivos_usuario_id_sha256",
                schema: "documentos",
                table: "archivos",
                columns: new[] { "usuario_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_exportaciones_estado_creado_en",
                schema: "documentos",
                table: "exportaciones",
                columns: new[] { "estado", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "IX_exportaciones_grupo_familiar_id",
                schema: "documentos",
                table: "exportaciones",
                column: "grupo_familiar_id");

            migrationBuilder.CreateIndex(
                name: "IX_exportaciones_usuario_id_hash_solicitud",
                schema: "documentos",
                table: "exportaciones",
                columns: new[] { "usuario_id", "hash_solicitud" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_procesamientos_archivo_id_hash_idempotencia",
                schema: "documentos",
                table: "procesamientos",
                columns: new[] { "archivo_id", "hash_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procesamientos_estado_creado_en",
                schema: "documentos",
                table: "procesamientos",
                columns: new[] { "estado", "creado_en" });

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_archivos_documento_id",
                schema: "finanzas",
                table: "movimientos",
                column: "documento_id",
                principalSchema: "documentos",
                principalTable: "archivos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_archivos_documento_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropTable(
                name: "exportaciones",
                schema: "documentos");

            migrationBuilder.DropTable(
                name: "procesamientos",
                schema: "documentos");

            migrationBuilder.DropTable(
                name: "archivos",
                schema: "documentos");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_documento_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "documento_id",
                schema: "finanzas",
                table: "movimientos");
        }
    }
}