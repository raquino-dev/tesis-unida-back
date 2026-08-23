using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

[DbContext(typeof(FinanzasDbContext))]
[Migration("20260822010000_M0024_AliasUsuario")]
public partial class M0024_AliasUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "alias",
            schema: "identidad",
            table: "usuarios",
            type: "citext",
            maxLength: 24,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE identidad.usuarios
            SET alias = 'usuario_' || substring(replace(id::text, '-', '') from 1 for 16)
            WHERE alias IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "alias",
            schema: "identidad",
            table: "usuarios",
            type: "citext",
            maxLength: 24,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "citext",
            oldMaxLength: 24,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_usuarios_alias",
            schema: "identidad",
            table: "usuarios",
            column: "alias",
            unique: true,
            filter: "anonimizado_en IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_usuarios_alias",
            schema: "identidad",
            table: "usuarios");

        migrationBuilder.DropColumn(
            name: "alias",
            schema: "identidad",
            table: "usuarios");
    }
}
