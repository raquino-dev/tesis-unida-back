using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0016_AdministracionUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rol",
                schema: "identidad",
                table: "usuarios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "usuario");

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuarios_rol",
                schema: "identidad",
                table: "usuarios",
                sql: "rol IN ('usuario', 'administrador')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuarios_estado",
                schema: "identidad",
                table: "usuarios",
                sql: "estado IN ('activo', 'inactivo', 'eliminado')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usuarios_rol",
                schema: "identidad",
                table: "usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "ck_usuarios_estado",
                schema: "identidad",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "rol",
                schema: "identidad",
                table: "usuarios");
        }
    }
}
