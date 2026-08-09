using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>Instrumentos versionados del piloto y sus respuestas anonimizables.</summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260809010000_M0021_InstrumentosPiloto")]
public partial class M0021_InstrumentosPiloto : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "piloto");

        migrationBuilder.CreateTable(
            name: "instrumentos",
            schema: "piloto",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                version_instrumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false),
                creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_instrumentos", x => x.id));

        migrationBuilder.CreateTable(
            name: "preguntas",
            schema: "piloto",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                instrumento_id = table.Column<Guid>(type: "uuid", nullable: false),
                orden = table.Column<int>(type: "integer", nullable: false),
                tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                requerida = table.Column<bool>(type: "boolean", nullable: false),
                minimo = table.Column<int>(type: "integer", nullable: true),
                maximo = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_preguntas", x => x.id);
                table.ForeignKey("FK_preguntas_instrumentos_instrumento_id", x => x.instrumento_id,
                    principalSchema: "piloto", principalTable: "instrumentos", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_preguntas_orden", "orden > 0");
                table.CheckConstraint("CK_preguntas_tipo", "tipo IN ('escala', 'texto')");
            });

        migrationBuilder.CreateTable(
            name: "respuestas",
            schema: "piloto",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                instrumento_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_instrumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                respondido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_respuestas", x => x.id);
                table.ForeignKey("FK_respuestas_instrumentos_instrumento_id", x => x.instrumento_id,
                    principalSchema: "piloto", principalTable: "instrumentos", principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_respuestas_usuarios_usuario_id", x => x.usuario_id,
                    principalSchema: "identidad", principalTable: "usuarios", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "respuestas_detalle",
            schema: "piloto",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                respuesta_id = table.Column<Guid>(type: "uuid", nullable: false),
                pregunta_id = table.Column<Guid>(type: "uuid", nullable: false),
                valor_escala = table.Column<int>(type: "integer", nullable: true),
                valor_texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_respuestas_detalle", x => x.id);
                table.ForeignKey("FK_respuestas_detalle_preguntas_pregunta_id", x => x.pregunta_id,
                    principalSchema: "piloto", principalTable: "preguntas", principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_respuestas_detalle_respuestas_respuesta_id", x => x.respuesta_id,
                    principalSchema: "piloto", principalTable: "respuestas", principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_respuestas_detalle_valor", "(valor_escala IS NOT NULL AND valor_texto IS NULL) OR (valor_escala IS NULL AND valor_texto IS NOT NULL)");
            });

        migrationBuilder.CreateIndex(name: "IX_instrumentos_codigo_version", schema: "piloto", table: "instrumentos", columns: new[] { "codigo", "version_instrumento" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_instrumentos_codigo_activo", schema: "piloto", table: "instrumentos", column: "codigo", unique: true, filter: "activo = true");
        migrationBuilder.CreateIndex(name: "IX_preguntas_instrumento_orden", schema: "piloto", table: "preguntas", columns: new[] { "instrumento_id", "orden" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_respuestas_usuario_instrumento", schema: "piloto", table: "respuestas", columns: new[] { "usuario_id", "instrumento_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_respuestas_instrumento", schema: "piloto", table: "respuestas", column: "instrumento_id");
        migrationBuilder.CreateIndex(name: "IX_respuestas_detalle_respuesta_pregunta", schema: "piloto", table: "respuestas_detalle", columns: new[] { "respuesta_id", "pregunta_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_respuestas_detalle_pregunta", schema: "piloto", table: "respuestas_detalle", column: "pregunta_id");

        migrationBuilder.Sql("""
            INSERT INTO piloto.instrumentos (id, codigo, version_instrumento, titulo, descripcion, activo, creado_en) VALUES
            ('00000000-0000-4000-8000-000000000101', 'preuso', '1.0', 'Cuestionario inicial', 'Antes de utilizar la aplicación. Respondé según tu situación actual.', true, now()),
            ('00000000-0000-4000-8000-000000000102', 'postuso', '1.0', 'Cuestionario final', 'Después de utilizar la aplicación durante el piloto.', true, now());

            INSERT INTO piloto.preguntas (id, instrumento_id, orden, tipo, texto, requerida, minimo, maximo) VALUES
            ('00000000-0000-4000-8000-000000000201', '00000000-0000-4000-8000-000000000101', 1, 'escala', 'Siento que actualmente tengo control sobre mis gastos.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000202', '00000000-0000-4000-8000-000000000101', 2, 'escala', 'Registro mis gastos de forma constante.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000203', '00000000-0000-4000-8000-000000000101', 3, 'escala', 'Me resulta fácil identificar en qué se distribuye mi dinero.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000204', '00000000-0000-4000-8000-000000000101', 4, 'escala', 'Logro cumplir el presupuesto que me propongo.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000205', '00000000-0000-4000-8000-000000000101', 5, 'escala', 'Me siento seguro al utilizar aplicaciones para organizar mis finanzas.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000206', '00000000-0000-4000-8000-000000000101', 6, 'escala', 'Comprendo mi situación financiera a partir de los registros que realizo.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000207', '00000000-0000-4000-8000-000000000101', 7, 'escala', 'Considero útil contar con alertas o recordatorios sobre mis gastos.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000208', '00000000-0000-4000-8000-000000000101', 8, 'escala', 'Considero que una aplicación móvil puede facilitar mi control financiero.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000209', '00000000-0000-4000-8000-000000000101', 9, 'texto', '¿Cómo registra actualmente sus gastos?', true, NULL, NULL),
            ('00000000-0000-4000-8000-00000000020a', '00000000-0000-4000-8000-000000000101', 10, 'texto', '¿Cuál es su principal dificultad para mantener el control?', true, NULL, NULL),
            ('00000000-0000-4000-8000-00000000020b', '00000000-0000-4000-8000-000000000101', 11, 'texto', '¿Qué esperaría de una aplicación de este tipo?', true, NULL, NULL),
            ('00000000-0000-4000-8000-000000000301', '00000000-0000-4000-8000-000000000102', 1, 'escala', 'La aplicación me ayudó a registrar mis gastos con facilidad.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000302', '00000000-0000-4000-8000-000000000102', 2, 'escala', 'La aplicación me ayudó a tener mayor control sobre mis gastos.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000303', '00000000-0000-4000-8000-000000000102', 3, 'escala', 'Pude comprender la información presentada en tableros y reportes.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000304', '00000000-0000-4000-8000-000000000102', 4, 'escala', 'Las alertas, recomendaciones o proyecciones me resultaron comprensibles.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000305', '00000000-0000-4000-8000-000000000102', 5, 'escala', 'El registro mediante comprobantes me resultó útil.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000306', '00000000-0000-4000-8000-000000000102', 6, 'escala', 'Me sentí seguro al utilizar la aplicación.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000307', '00000000-0000-4000-8000-000000000102', 7, 'escala', 'La aplicación me resultó útil para organizar mis finanzas.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000308', '00000000-0000-4000-8000-000000000102', 8, 'escala', 'Usaría esta aplicación con frecuencia.', true, 1, 5),
            ('00000000-0000-4000-8000-000000000309', '00000000-0000-4000-8000-000000000102', 9, 'texto', '¿Qué funcionalidad le resultó más útil?', true, NULL, NULL),
            ('00000000-0000-4000-8000-00000000030a', '00000000-0000-4000-8000-000000000102', 10, 'texto', '¿Qué parte fue confusa o difícil?', true, NULL, NULL),
            ('00000000-0000-4000-8000-00000000030b', '00000000-0000-4000-8000-000000000102', 11, 'texto', '¿Qué mejoraría antes de usarla regularmente?', true, NULL, NULL),
            ('00000000-0000-4000-8000-00000000030c', '00000000-0000-4000-8000-000000000102', 12, 'texto', '¿Qué preocupación de seguridad o privacidad mantiene?', true, NULL, NULL);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "respuestas_detalle", schema: "piloto");
        migrationBuilder.DropTable(name: "respuestas", schema: "piloto");
        migrationBuilder.DropTable(name: "preguntas", schema: "piloto");
        migrationBuilder.DropTable(name: "instrumentos", schema: "piloto");
    }
}
