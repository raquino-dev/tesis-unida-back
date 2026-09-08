using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

/// <summary>
/// Alinea decisiones académicas consolidadas del piloto sin alterar las respuestas
/// ya registradas: precios aprobados y bloque complementario solicitado por tutoría.
/// </summary>
[DbContext(typeof(FinanzasDbContext))]
[Migration("20260908010000_M0023_AlineacionPilotoTesis")]
public partial class M0023_AlineacionPilotoTesis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE suscripciones.planes
               SET precio = 39000
             WHERE codigo = 'premium-mensual';

            UPDATE suscripciones.planes
               SET precio = 390000
             WHERE codigo = 'premium-anual';
            """);

        migrationBuilder.Sql("""
            INSERT INTO piloto.instrumentos
                (id, codigo, version_instrumento, titulo, descripcion, activo, creado_en)
            VALUES
                ('00000000-0000-4000-8000-000000000103',
                 'preuso-complementario',
                 '1.0',
                 'Bloque complementario inicial',
                 'Bloque adicional solicitado por tutoría durante la fase inicial del piloto. Se analiza de forma descriptiva y no se utiliza como medición basal pre/post.',
                 true,
                 now())
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE suscripciones.planes
               SET precio = 45000
             WHERE codigo = 'premium-mensual';

            UPDATE suscripciones.planes
               SET precio = 450000
             WHERE codigo = 'premium-anual';
            """);

        migrationBuilder.Sql("""
            DO $rollback$
            BEGIN
                IF EXISTS (
                    SELECT 1
                      FROM piloto.respuestas
                     WHERE instrumento_id = '00000000-0000-4000-8000-000000000103'
                ) THEN
                    UPDATE piloto.instrumentos
                       SET activo = false
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
