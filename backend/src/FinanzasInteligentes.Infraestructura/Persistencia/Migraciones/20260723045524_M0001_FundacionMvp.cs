using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0001_FundacionMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finanzas");

            migrationBuilder.EnsureSchema(
                name: "infra");

            migrationBuilder.EnsureSchema(
                name: "identidad");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "categorias",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    icono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    es_predeterminada = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cuentas",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    saldo_actual = table.Column<long>(type: "bigint", nullable: false),
                    saldo_inicial = table.Column<long>(type: "bigint", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    icono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    incluida_en_total = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas", x => x.id);
                    table.CheckConstraint("ck_cuentas_moneda", "moneda = 'PYG'");
                });

            migrationBuilder.CreateTable(
                name: "idempotencias",
                schema: "infra",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metodo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ruta = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    hash_solicitud = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo_respuesta = table.Column<short>(type: "smallint", nullable: true),
                    cuerpo_respuesta = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotencias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_eventos",
                schema: "infra",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    agregado_tipo = table.Column<string>(type: "text", nullable: false),
                    agregado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    disponible_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    procesado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_eventos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    hash_contrasena = table.Column<string>(type: "text", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    idioma = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    zona_horaria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    anonimizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movimientos",
                schema: "finanzas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    monto = table.Column<long>(type: "bigint", nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    anulado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos", x => x.id);
                    table.CheckConstraint("ck_movimientos_moneda", "moneda = 'PYG'");
                    table.CheckConstraint("ck_movimientos_monto", "monto > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_cuentas_cuenta_id",
                        column: x => x.cuenta_id,
                        principalSchema: "finanzas",
                        principalTable: "cuentas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preferencias",
                schema: "identidad",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tema = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notificaciones_push = table.Column<bool>(type: "boolean", nullable: false),
                    notificaciones_correo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preferencias", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_preferencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sesiones",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_refresh_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    familia_token = table.Column<Guid>(type: "uuid", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones", x => x.id);
                    table.ForeignKey(
                        name: "FK_sesiones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_usuario_id_nombre",
                schema: "finanzas",
                table: "cuentas",
                columns: new[] { "usuario_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_idempotencias_usuario_id_clave",
                schema: "infra",
                table: "idempotencias",
                columns: new[] { "usuario_id", "clave" },
                unique: true,
                filter: "usuario_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_cuenta_id",
                schema: "finanzas",
                table: "movimientos",
                column: "cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_usuario_id_fecha_id",
                schema: "finanzas",
                table: "movimientos",
                columns: new[] { "usuario_id", "fecha", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_eventos_estado_disponible_en",
                schema: "infra",
                table: "outbox_eventos",
                columns: new[] { "estado", "disponible_en" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_hash_refresh_token",
                schema: "identidad",
                table: "sesiones",
                column: "hash_refresh_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_usuario_id",
                schema: "identidad",
                table: "sesiones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_correo",
                schema: "identidad",
                table: "usuarios",
                column: "correo",
                unique: true,
                filter: "anonimizado_en IS NULL");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_api') THEN
                    GRANT USAGE ON SCHEMA identidad, finanzas, infra TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA identidad, finanzas TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_api;
                  END IF;
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_worker') THEN
                    GRANT USAGE ON SCHEMA identidad, finanzas, infra TO finanzas_worker;
                    GRANT SELECT ON ALL TABLES IN SCHEMA identidad, finanzas TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_worker;
                  END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "idempotencias",
                schema: "infra");

            migrationBuilder.DropTable(
                name: "movimientos",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "outbox_eventos",
                schema: "infra");

            migrationBuilder.DropTable(
                name: "preferencias",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "sesiones",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "cuentas",
                schema: "finanzas");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "identidad");
        }
    }
}