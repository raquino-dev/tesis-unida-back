using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0008_FinanzasFamiliares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "familias");

            migrationBuilder.CreateTable(
                name: "grupos_familiares",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_familiares", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cajas_compartidas",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saldo = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cajas_compartidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cajas_compartidas_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    icono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_familias_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_compartidas",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compartida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_compartidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cuentas_compartidas_cuentas_cuenta_id",
                        column: x => x.cuenta_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_compartidas_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eliminaciones",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eliminaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_eliminaciones_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "integrantes",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integrantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_integrantes_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_integrantes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitaciones",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    usuario_destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hash_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash_codigo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_invitaciones_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "movimientos",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    categoria_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_familias_movimientos", x => x.id);
                    table.CheckConstraint("ck_movimientos_familiares_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_cuentas_cuenta_id",
                        column: x => x.cuenta_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operaciones_caja",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_privada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    saldo_anterior = table.Column<long>(type: "bigint", nullable: false),
                    saldo_posterior = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operaciones_caja", x => x.id);
                    table.ForeignKey(
                        name: "FK_operaciones_caja_cuentas_cuenta_privada_id",
                        column: x => x.cuenta_privada_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operaciones_caja_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "presupuestos",
                schema: "familias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_familiar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    categoria_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presupuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_presupuestos_grupos_familiares_grupo_familiar_id",
                        column: x => x.grupo_familiar_id,
                        principalSchema: "familias",
                        principalTable: "grupos_familiares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cajas_compartidas_grupo_familiar_id",
                schema: "familias",
                table: "cajas_compartidas",
                column: "grupo_familiar_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categorias_grupo_familiar_id_nombre",
                schema: "familias",
                table: "categorias",
                columns: new[] { "grupo_familiar_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_compartidas_cuenta_id",
                schema: "familias",
                table: "cuentas_compartidas",
                column: "cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_compartidas_grupo_familiar_id_cuenta_id",
                schema: "familias",
                table: "cuentas_compartidas",
                columns: new[] { "grupo_familiar_id", "cuenta_id" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_eliminaciones_grupo_familiar_id",
                schema: "familias",
                table: "eliminaciones",
                column: "grupo_familiar_id",
                unique: true,
                filter: "estado = 'pendiente'");

            migrationBuilder.CreateIndex(
                name: "IX_integrantes_grupo_familiar_id_usuario_id",
                schema: "familias",
                table: "integrantes",
                columns: new[] { "grupo_familiar_id", "usuario_id" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_integrantes_usuario_id",
                schema: "familias",
                table: "integrantes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitaciones_grupo_familiar_id",
                schema: "familias",
                table: "invitaciones",
                column: "grupo_familiar_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitaciones_hash_token",
                schema: "familias",
                table: "invitaciones",
                column: "hash_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_familias_movimientos_cuenta_id",
                schema: "familias",
                table: "movimientos",
                column: "cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_grupo_familiar_id_fecha_id",
                schema: "familias",
                table: "movimientos",
                columns: new[] { "grupo_familiar_id", "fecha", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_caja_cuenta_privada_id",
                schema: "familias",
                table: "operaciones_caja",
                column: "cuenta_privada_id");

            migrationBuilder.CreateIndex(
                name: "IX_operaciones_caja_grupo_familiar_id",
                schema: "familias",
                table: "operaciones_caja",
                column: "grupo_familiar_id");

            migrationBuilder.CreateIndex(
                name: "IX_presupuestos_grupo_familiar_id",
                schema: "familias",
                table: "presupuestos",
                column: "grupo_familiar_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cajas_compartidas",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "categorias",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "cuentas_compartidas",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "eliminaciones",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "integrantes",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "invitaciones",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "movimientos",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "operaciones_caja",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "presupuestos",
                schema: "familias");

            migrationBuilder.DropTable(
                name: "grupos_familiares",
                schema: "familias");
        }
    }
}