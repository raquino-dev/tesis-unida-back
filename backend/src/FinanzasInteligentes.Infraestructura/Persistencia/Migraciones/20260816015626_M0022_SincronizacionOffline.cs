using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0022_SincronizacionOffline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sincronizacion");

            migrationBuilder.CreateTable(
                name: "cambios",
                schema: "sincronizacion",
                columns: table => new
                {
                    secuencia = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_entidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cambios", x => x.secuencia);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cambios_usuario_id_secuencia",
                schema: "sincronizacion",
                table: "cambios",
                columns: new[] { "usuario_id", "secuencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cambios",
                schema: "sincronizacion");
        }
    }
}
