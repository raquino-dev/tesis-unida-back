using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0004_SeguridadRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_verificaciones_otp_usuario_id",
                schema: "identidad",
                table: "verificaciones_otp",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_recuperaciones_contrasena_usuario_id",
                schema: "identidad",
                table: "recuperaciones_contrasena",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_recuperaciones_contrasena_usuarios_usuario_id",
                schema: "identidad",
                table: "recuperaciones_contrasena",
                column: "usuario_id",
                principalSchema: "identidad",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_verificaciones_otp_desafios_otp_desafio_id",
                schema: "identidad",
                table: "verificaciones_otp",
                column: "desafio_id",
                principalSchema: "identidad",
                principalTable: "desafios_otp",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_verificaciones_otp_usuarios_usuario_id",
                schema: "identidad",
                table: "verificaciones_otp",
                column: "usuario_id",
                principalSchema: "identidad",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recuperaciones_contrasena_usuarios_usuario_id",
                schema: "identidad",
                table: "recuperaciones_contrasena");

            migrationBuilder.DropForeignKey(
                name: "FK_verificaciones_otp_desafios_otp_desafio_id",
                schema: "identidad",
                table: "verificaciones_otp");

            migrationBuilder.DropForeignKey(
                name: "FK_verificaciones_otp_usuarios_usuario_id",
                schema: "identidad",
                table: "verificaciones_otp");

            migrationBuilder.DropIndex(
                name: "IX_verificaciones_otp_usuario_id",
                schema: "identidad",
                table: "verificaciones_otp");

            migrationBuilder.DropIndex(
                name: "IX_recuperaciones_contrasena_usuario_id",
                schema: "identidad",
                table: "recuperaciones_contrasena");
        }
    }
}