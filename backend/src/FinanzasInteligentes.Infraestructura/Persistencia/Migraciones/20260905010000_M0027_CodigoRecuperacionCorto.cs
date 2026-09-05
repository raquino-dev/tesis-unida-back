using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

[DbContext(typeof(FinanzasDbContext))]
[Migration("20260905010000_M0027_CodigoRecuperacionCorto")]
public partial class M0027_CodigoRecuperacionCorto : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS identidad."IX_recuperaciones_contrasena_hash_token";
            """);

        migrationBuilder.AddColumn<int>(
            name: "intentos_restantes",
            schema: "identidad",
            table: "recuperaciones_contrasena",
            type: "integer",
            nullable: false,
            defaultValue: 5);

        migrationBuilder.AddCheckConstraint(
            name: "CK_recuperaciones_contrasena_intentos",
            schema: "identidad",
            table: "recuperaciones_contrasena",
            sql: "intentos_restantes >= 0 AND intentos_restantes <= 5");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_recuperaciones_contrasena_intentos",
            schema: "identidad",
            table: "recuperaciones_contrasena");

        migrationBuilder.DropColumn(
            name: "intentos_restantes",
            schema: "identidad",
            table: "recuperaciones_contrasena");

        migrationBuilder.CreateIndex(
            name: "IX_recuperaciones_contrasena_hash_token",
            schema: "identidad",
            table: "recuperaciones_contrasena",
            column: "hash_token",
            unique: true);
    }
}
