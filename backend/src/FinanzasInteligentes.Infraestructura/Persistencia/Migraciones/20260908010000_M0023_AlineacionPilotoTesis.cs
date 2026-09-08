using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>
/// Alinea decisiones académicas consolidadas del piloto sin alterar respuestas
/// históricas: precios aprobados, bloque complementario y postuso versionado.
/// </summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260908010000_M0023_AlineacionPilotoTesis")]
public partial class M0023_AlineacionPilotoTesis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE suscripciones.planes SET precio = 39000
             WHERE codigo = 'premium-mensual';
            UPDATE suscripciones.planes SET precio = 390000
             WHERE codigo = 'premium-anual';
            """);

        migrationBuilder.Sql("""
            INSERT INTO piloto.instrumentos
                (id, codigo, version_instrumento, titulo, descripcion, activo, creado_en)
            VALUES
                ('00000000-0000-4000-8000-000000000103',
                 'preuso-complementario', '1.0', 'Bloque complementario inicial',
                 'Bloque adicional solicitado por tutoría durante la fase inicial del piloto. Se analiza de forma descriptiva y no se utiliza como medición basal pre/post.',
                 true, now())
            ON CONFLICT (codigo, version_instrumento) DO NOTHING;

            INSERT INTO piloto.preguntas
                (id, instrumento_id, orden, tipo, texto, requerida, minimo, maximo)
            VALUES
                ('00000000-0000-4000-8000-000000000401', '00000000-0000-4000-8000-000000000103', 1, 'escala', 'Consideraría contratar un plan Premium si las funcionalidades ofrecidas aportan valor a la organización de mis finanzas personales o familiares.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000402', '00000000-0000-4000-8000-000000000103', 2, 'escala', 'Es probable que considere adquirir una suscripción si la aplicación satisface mis necesidades de control financiero.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000403', '00000000-0000-4000-8000-000000000103', 3, 'escala', 'Tener acceso previo a una versión gratuita influiría positivamente en mi decisión de contratar posteriormente un plan Premium.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000404', '00000000-0000-4000-8000-000000000103', 4, 'escala', 'Considero importante que una aplicación financiera permita realizar operaciones básicas aun cuando no exista conexión a Internet.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000405', '00000000-0000-4000-8000-000000000103', 5, 'escala', 'Considero importante que una aplicación financiera proteja el acceso a la información mediante controles adicionales de seguridad cuando sea necesario.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000406', '00000000-0000-4000-8000-000000000103', 6, 'escala', 'Considero importante poder revisar y corregir la información detectada automáticamente de un comprobante antes de registrarla definitivamente.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000407', '00000000-0000-4000-8000-000000000103', 7, 'escala', 'Considero importante que las principales operaciones de una aplicación financiera respondan de forma rápida y estable.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000408', '00000000-0000-4000-8000-000000000103', 8, 'escala', 'Considero importante que la información personal y la información compartida con un grupo familiar permanezcan claramente separadas.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000409', '00000000-0000-4000-8000-000000000103', 9, 'escala', 'Estaría dispuesto/a a pagar Gs. 39.000 mensuales por una versión Premium si considero útiles sus funcionalidades.', true, 1, 5),
                ('00000000-0000-4000-8000-00000000040a', '00000000-0000-4000-8000-000000000103', 10, 'escala', 'Estaría dispuesto/a a pagar Gs. 390.000 anuales por una versión Premium si considero útiles sus funcionalidades.', true, 1, 5),
                ('00000000-0000-4000-8000-00000000040b', '00000000-0000-4000-8000-000000000103', 11, 'escala', 'Considero razonable pagar por una aplicación de control financiero cuando ofrece funciones adicionales de automatización, análisis, seguridad y gestión familiar.', true, 1, 5)
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            UPDATE piloto.instrumentos
               SET activo = false
             WHERE codigo = 'postuso' AND activo = true;

            INSERT INTO piloto.instrumentos
                (id, codigo, version_instrumento, titulo, descripcion, activo, creado_en)
            VALUES
                ('00000000-0000-4000-8000-000000000104',
                 'postuso', '2.0', 'Cuestionario final',
                 'Después de completar los 28 días de observación del piloto. Respondé según tu experiencia real con la aplicación.',
                 true, now())
            ON CONFLICT (codigo, version_instrumento) DO UPDATE SET activo = true;

            INSERT INTO piloto.preguntas
                (id, instrumento_id, orden, tipo, texto, requerida, minimo, maximo)
            VALUES
                ('00000000-0000-4000-8000-000000000501', '00000000-0000-4000-8000-000000000104', 1, 'escala', 'La aplicación fue fácil de aprender.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000502', '00000000-0000-4000-8000-000000000104', 2, 'escala', 'Pude registrar gastos con menos esfuerzo que mediante mi procedimiento habitual.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000503', '00000000-0000-4000-8000-000000000104', 3, 'escala', 'La captura documental redujo la carga manual.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000504', '00000000-0000-4000-8000-000000000104', 4, 'escala', 'Los reportes me ayudaron a comprender mis gastos.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000505', '00000000-0000-4000-8000-000000000104', 5, 'escala', 'Las proyecciones y explicaciones fueron comprensibles.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000506', '00000000-0000-4000-8000-000000000104', 6, 'escala', 'La separación entre información privada y familiar fue clara.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000507', '00000000-0000-4000-8000-000000000104', 7, 'escala', 'Los controles de seguridad me generaron confianza.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000508', '00000000-0000-4000-8000-000000000104', 8, 'escala', 'Los mensajes de error y validación fueron claros.', true, 1, 5),
                ('00000000-0000-4000-8000-000000000509', '00000000-0000-4000-8000-000000000104', 9, 'escala', 'La aplicación puede ayudarme a mejorar el control financiero.', true, 1, 5),
                ('00000000-0000-4000-8000-00000000050a', '00000000-0000-4000-8000-000000000104', 10, 'escala', 'Utilizaría la aplicación de forma continua.', true, 1, 5),
                ('00000000-0000-4000-8000-00000000050b', '00000000-0000-4000-8000-000000000104', 11, 'texto', '¿Qué funcionalidad le resultó más útil?', true, NULL, NULL),
                ('00000000-0000-4000-8000-00000000050c', '00000000-0000-4000-8000-000000000104', 12, 'texto', '¿Qué parte fue confusa o difícil?', true, NULL, NULL),
                ('00000000-0000-4000-8000-00000000050d', '00000000-0000-4000-8000-000000000104', 13, 'texto', '¿Qué mejoraría antes de usarla regularmente?', true, NULL, NULL),
                ('00000000-0000-4000-8000-00000000050e', '00000000-0000-4000-8000-000000000104', 14, 'texto', '¿Qué preocupación de seguridad o privacidad mantiene?', true, NULL, NULL)
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE suscripciones.planes SET precio = 45000
             WHERE codigo = 'premium-mensual';
            UPDATE suscripciones.planes SET precio = 450000
             WHERE codigo = 'premium-anual';
            """);

        migrationBuilder.Sql("""
            UPDATE piloto.instrumentos SET activo = false
             WHERE id = '00000000-0000-4000-8000-000000000104';
            UPDATE piloto.instrumentos SET activo = true
             WHERE id = '00000000-0000-4000-8000-000000000102';

            DO $rollback$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM piloto.respuestas
                     WHERE instrumento_id = '00000000-0000-4000-8000-000000000104'
                ) THEN
                    DELETE FROM piloto.preguntas
                     WHERE instrumento_id = '00000000-0000-4000-8000-000000000104';
                    DELETE FROM piloto.instrumentos
                     WHERE id = '00000000-0000-4000-8000-000000000104';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM piloto.respuestas
                     WHERE instrumento_id = '00000000-0000-4000-8000-000000000103'
                ) THEN
                    UPDATE piloto.instrumentos SET activo = false
                     WHERE id = '00000000-0000-4000-8000-000000000103';
                ELSE
                    DELETE FROM piloto.preguntas
                     WHERE instrumento_id = '00000000-0000-4000-8000-000000000103';
                    DELETE FROM piloto.instrumentos
                     WHERE id = '00000000-0000-4000-8000-000000000103';
                END IF;
            END
            $rollback$;
            """);
    }
}
