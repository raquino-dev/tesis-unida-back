using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

[DbContext(typeof(FinanzasDbContext))]
[Migration("20260823010000_M0025_FechasCreacionInstrumentosPiloto")]
public partial class M0025_FechasCreacionInstrumentosPiloto : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            schema: "piloto",
            table: "preguntas",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            schema: "piloto",
            table: "respuestas",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "creado_en",
            schema: "piloto",
            table: "respuestas_detalle",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "creado_en",
            schema: "piloto",
            table: "preguntas");

        migrationBuilder.DropColumn(
            name: "creado_en",
            schema: "piloto",
            table: "respuestas");

        migrationBuilder.DropColumn(
            name: "creado_en",
            schema: "piloto",
            table: "respuestas_detalle");
    }
}
