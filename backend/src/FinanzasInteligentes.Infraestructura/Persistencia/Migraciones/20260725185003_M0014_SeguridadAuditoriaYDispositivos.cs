using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0014_SeguridadAuditoriaYDispositivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "seguridad");

            migrationBuilder.EnsureSchema(
                name: "auditoria");

            migrationBuilder.CreateTable(
                name: "dispositivos",
                schema: "seguridad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identificador_instalacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    plataforma = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version_so = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version_app = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    push_token_cifrado = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    zona_horaria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    confiable = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acceso_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispositivos", x => x.id);
                    table.ForeignKey(
                        name: "FK_dispositivos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eventos",
                schema: "auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recurso_tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    datos_anteriores = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    datos_nuevos = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos", x => x.id);
                    table.ForeignKey(
                        name: "FK_eventos_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_eventos_usuarios_actor_usuario_id",
                        column: x => x.actor_usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "eventos",
                schema: "seguridad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispositivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    exitoso = table.Column<bool>(type: "boolean", nullable: false),
                    origen_aproximado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    dispositivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos1", x => x.id);
                    table.ForeignKey(
                        name: "FK_eventos_dispositivos_dispositivo_id",
                        column: x => x.dispositivo_id,
                        principalSchema: "seguridad",
                        principalTable: "dispositivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_eventos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dispositivos_usuario_id_identificador_instalacion",
                schema: "seguridad",
                table: "dispositivos",
                columns: new[] { "usuario_id", "identificador_instalacion" },
                unique: true,
                filter: "revocado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_actor_usuario_id_ocurrido_en",
                schema: "auditoria",
                table: "eventos",
                columns: new[] { "actor_usuario_id", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_correlation_id",
                schema: "auditoria",
                table: "eventos",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_grupo_familiar_id",
                schema: "auditoria",
                table: "eventos",
                column: "grupo_familiar_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_recurso_tipo_recurso_id_ocurrido_en",
                schema: "auditoria",
                table: "eventos",
                columns: new[] { "recurso_tipo", "recurso_id", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_dispositivo_id",
                schema: "seguridad",
                table: "eventos",
                column: "dispositivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_usuario_id_ocurrido_en",
                schema: "seguridad",
                table: "eventos",
                columns: new[] { "usuario_id", "ocurrido_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos",
                schema: "auditoria");

            migrationBuilder.DropTable(
                name: "eventos",
                schema: "seguridad");

            migrationBuilder.DropTable(
                name: "dispositivos",
                schema: "seguridad");
        }
    }
}