using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

public partial class M0002_IdentidadPerfilSesiones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ubicacion", schema: "identidad", table: "usuarios",
            type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "identificador_dispositivo", schema: "identidad", table: "sesiones",
            type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "nombre_dispositivo", schema: "identidad", table: "sesiones",
            type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "plataforma_dispositivo", schema: "identidad", table: "sesiones",
            type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>(name: "resumen_semanal", schema: "identidad", table: "preferencias",
            type: "boolean", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ubicacion", schema: "identidad", table: "usuarios");
        migrationBuilder.DropColumn(name: "identificador_dispositivo", schema: "identidad", table: "sesiones");
        migrationBuilder.DropColumn(name: "nombre_dispositivo", schema: "identidad", table: "sesiones");
        migrationBuilder.DropColumn(name: "plataforma_dispositivo", schema: "identidad", table: "sesiones");
        migrationBuilder.DropColumn(name: "resumen_semanal", schema: "identidad", table: "preferencias");
    }
}