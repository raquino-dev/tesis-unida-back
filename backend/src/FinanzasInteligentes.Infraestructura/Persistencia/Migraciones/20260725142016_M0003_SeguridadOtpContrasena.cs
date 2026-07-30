using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0003_SeguridadOtpContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "desafios_otp",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hash_codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    destino = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    intentos_restantes = table.Column<int>(type: "integer", nullable: false),
                    verificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_desafios_otp", x => x.id);
                    table.ForeignKey(
                        name: "FK_desafios_otp_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recuperaciones_contrasena",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recuperaciones_contrasena", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verificaciones_otp",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desafio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    motivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verificaciones_otp", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_desafios_otp_usuario_id",
                schema: "identidad",
                table: "desafios_otp",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_recuperaciones_contrasena_hash_token",
                schema: "identidad",
                table: "recuperaciones_contrasena",
                column: "hash_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verificaciones_otp_desafio_id",
                schema: "identidad",
                table: "verificaciones_otp",
                column: "desafio_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "desafios_otp",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "recuperaciones_contrasena",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "verificaciones_otp",
                schema: "identidad");
        }
    }
}