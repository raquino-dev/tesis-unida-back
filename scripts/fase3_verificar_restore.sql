-- Fase 3 · Verificación de restore sin exponer PII
-- Ejecutar por separado en producción y en el proyecto temporal restaurado.
-- Comparar resultados agregados; no guardar filas individuales en evidencia sanitizada.

SELECT current_database() AS database_name;

SELECT schema_name
FROM information_schema.schemata
WHERE schema_name IN (
  'identidad','finanzas','familias','documentos','analitica',
  'suscripciones','infraestructura','piloto','sincronizacion'
)
ORDER BY schema_name;

SELECT table_schema, COUNT(*) AS table_count
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema IN (
    'identidad','finanzas','familias','documentos','analitica',
    'suscripciones','infraestructura','piloto','sincronizacion'
  )
GROUP BY table_schema
ORDER BY table_schema;

-- Conteos agregados de entidades críticas.
SELECT 'identidad.usuarios' AS entity, COUNT(*)::bigint AS total FROM identidad.usuarios
UNION ALL
SELECT 'finanzas.movimientos', COUNT(*)::bigint FROM finanzas.movimientos
UNION ALL
SELECT 'familias.grupos_familiares', COUNT(*)::bigint FROM familias.grupos_familiares
UNION ALL
SELECT 'piloto.instrumentos', COUNT(*)::bigint FROM piloto.instrumentos
UNION ALL
SELECT 'piloto.preguntas', COUNT(*)::bigint FROM piloto.preguntas
UNION ALL
SELECT 'piloto.respuestas', COUNT(*)::bigint FROM piloto.respuestas
ORDER BY entity;

-- Integridad básica de respuestas del piloto sin recuperar el contenido de respuestas.
SELECT
  COUNT(*) AS respuestas,
  COUNT(DISTINCT usuario_id) AS usuarios_con_respuesta,
  COUNT(DISTINCT instrumento_id) AS instrumentos_respondidos
FROM piloto.respuestas;

-- Historial EF (si existe en public).
SELECT "MigrationId"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId";
