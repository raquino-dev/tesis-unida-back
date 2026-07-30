using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0017_PrivacidadConsentimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "politicas_privacidad",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_politica = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    url_documento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vigente_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_politicas_privacidad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consentimientos_privacidad",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    politica_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_politica = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    finalidad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    aceptado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consentimientos_privacidad", x => x.id);
                    table.ForeignKey(
                        name: "FK_consentimientos_privacidad_politicas_privacidad_politica_id",
                        column: x => x.politica_id,
                        principalSchema: "identidad",
                        principalTable: "politicas_privacidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consentimientos_privacidad_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identidad",
                table: "politicas_privacidad",
                columns: new[] { "id", "activa", "creado_en", "titulo", "url_documento", "version_politica", "vigente_desde" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000201"), true, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Política de privacidad", "https://example.invalid/privacidad/1.0", "1.0", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_consentimientos_privacidad_politica_id",
                schema: "identidad",
                table: "consentimientos_privacidad",
                column: "politica_id");

            migrationBuilder.CreateIndex(
                name: "IX_consentimientos_privacidad_usuario_id_politica_id_finalidad",
                schema: "identidad",
                table: "consentimientos_privacidad",
                columns: new[] { "usuario_id", "politica_id", "finalidad" },
                unique: true,
                filter: "revocado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_politicas_privacidad_version_politica",
                schema: "identidad",
                table: "politicas_privacidad",
                column: "version_politica",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consentimientos_privacidad",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "politicas_privacidad",
                schema: "identidad");
        }
    }
}
