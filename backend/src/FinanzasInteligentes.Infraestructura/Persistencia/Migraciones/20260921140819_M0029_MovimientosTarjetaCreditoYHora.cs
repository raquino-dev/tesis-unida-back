using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0029_MovimientosTarjetaCreditoYHora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "hora",
                schema: "finanzas",
                table: "movimientos",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos",
                column: "tarjeta_credito_id");

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_tarjetas_credito_tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos",
                column: "tarjeta_credito_id",
                principalSchema: "finanzas",
                principalTable: "tarjetas_credito",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_tarjetas_credito_tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "hora",
                schema: "finanzas",
                table: "movimientos");

            migrationBuilder.DropColumn(
                name: "tarjeta_credito_id",
                schema: "finanzas",
                table: "movimientos");
        }
    }
}
