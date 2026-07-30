using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0010_PresupuestosYMetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metas_ahorro",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    monto_objetivo = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fecha_objetivo = table.Column<DateOnly>(type: "date", nullable: false),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metas_ahorro", x => x.id);
                    table.CheckConstraint("ck_metas_ahorro_moneda", "moneda = 'PYG'");
                    table.CheckConstraint("ck_metas_ahorro_monto", "monto_objetivo > 0");
                    table.CheckConstraint("ck_metas_ahorro_propietario", "(ambito = 'privado' AND usuario_id IS NOT NULL AND grupo_familiar_id IS NULL) OR (ambito = 'familiar' AND usuario_id IS NULL AND grupo_familiar_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_metas_ahorro_cuentas_cuenta_id",
                        column: x => x.cuenta_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metas_ahorro_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_metas_ahorro_usuarios_creado_por",
                        column: x => x.creado_por,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metas_ahorro_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "presupuestos",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presupuestos1", x => x.id);
                    table.CheckConstraint("ck_presupuestos_moneda", "moneda = 'PYG'");
                    table.CheckConstraint("ck_presupuestos_monto", "monto > 0");
                    table.CheckConstraint("ck_presupuestos_periodo", "periodo IN ('semanal', 'mensual', 'anual')");
                    table.ForeignKey(
                        name: "FK_presupuestos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aportes_meta",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    cuenta_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    aportado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hash_idempotencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aportes_meta", x => x.id);
                    table.CheckConstraint("ck_aportes_meta_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_aportes_meta_cuentas_cuenta_origen_id",
                        column: x => x.cuenta_origen_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_aportes_meta_metas_ahorro_meta_id",
                        column: x => x.meta_id,
                        principalSchema: "finanzas",
                        principalTable: "metas_ahorro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_aportes_meta_usuarios_aportado_por",
                        column: x => x.aportado_por,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "presupuesto_categorias",
                schema: "finanzas",
                columns: table => new
                {
                    presupuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presupuesto_categorias", x => new { x.presupuesto_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_presupuesto_categorias_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "finanzas",
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_presupuesto_categorias_presupuestos_presupuesto_id",
                        column: x => x.presupuesto_id,
                        principalSchema: "finanzas",
                        principalTable: "presupuestos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aportes_meta_aportado_por",
                schema: "finanzas",
                table: "aportes_meta",
                column: "aportado_por");

            migrationBuilder.CreateIndex(
                name: "IX_aportes_meta_cuenta_origen_id",
                schema: "finanzas",
                table: "aportes_meta",
                column: "cuenta_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_aportes_meta_meta_id_fecha_id",
                schema: "finanzas",
                table: "aportes_meta",
                columns: new[] { "meta_id", "fecha", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_aportes_meta_meta_id_hash_idempotencia",
                schema: "finanzas",
                table: "aportes_meta",
                columns: new[] { "meta_id", "hash_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metas_ahorro_creado_por",
                schema: "finanzas",
                table: "metas_ahorro",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "IX_metas_ahorro_cuenta_id",
                schema: "finanzas",
                table: "metas_ahorro",
                column: "cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_ahorro_grupo_familiar_id_estado",
                schema: "finanzas",
                table: "metas_ahorro",
                columns: new[] { "grupo_familiar_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_metas_ahorro_usuario_id_estado",
                schema: "finanzas",
                table: "metas_ahorro",
                columns: new[] { "usuario_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_presupuesto_categorias_categoria_id",
                schema: "finanzas",
                table: "presupuesto_categorias",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_presupuestos_usuario_id_estado",
                schema: "finanzas",
                table: "presupuestos",
                columns: new[] { "usuario_id", "estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aportes_meta",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "presupuesto_categorias",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "metas_ahorro",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "presupuestos",
                schema: "finanzas");
        }
    }
}