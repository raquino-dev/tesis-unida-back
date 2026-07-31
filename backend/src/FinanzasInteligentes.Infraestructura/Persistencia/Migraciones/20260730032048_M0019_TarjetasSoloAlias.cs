using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0019_TarjetasSoloAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emisor",
                schema: "finanzas",
                table: "tarjetas_credito");

            migrationBuilder.DropColumn(
                name: "ultimos_cuatro",
                schema: "finanzas",
                table: "tarjetas_credito");

            migrationBuilder.RenameColumn(
                name: "nombre",
                schema: "finanzas",
                table: "tarjetas_credito",
                newName: "alias");

            migrationBuilder.RenameIndex(
                name: "IX_tarjetas_credito_usuario_id_nombre",
                schema: "finanzas",
                table: "tarjetas_credito",
                newName: "IX_tarjetas_credito_usuario_id_alias");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "alias",
                schema: "finanzas",
                table: "tarjetas_credito",
                newName: "nombre");

            migrationBuilder.RenameIndex(
                name: "IX_tarjetas_credito_usuario_id_alias",
                schema: "finanzas",
                table: "tarjetas_credito",
                newName: "IX_tarjetas_credito_usuario_id_nombre");

            migrationBuilder.AddColumn<string>(
                name: "emisor",
                schema: "finanzas",
                table: "tarjetas_credito",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ultimos_cuatro",
                schema: "finanzas",
                table: "tarjetas_credito",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");
        }
    }
}
