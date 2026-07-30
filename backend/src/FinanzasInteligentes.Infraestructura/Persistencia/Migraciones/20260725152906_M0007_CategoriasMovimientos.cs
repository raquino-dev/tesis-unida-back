using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0007_CategoriasMovimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movimientos_categorias",
                schema: "finanzas",
                columns: table => new
                {
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_categorias", x => new { x.movimiento_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_movimientos_categorias_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "finanzas",
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_categorias_movimientos_movimiento_id",
                        column: x => x.movimiento_id,
                        principalSchema: "finanzas",
                        principalTable: "movimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_categorias_categoria_id",
                schema: "finanzas",
                table: "movimientos_categorias",
                column: "categoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos_categorias",
                schema: "finanzas");
        }
    }
}