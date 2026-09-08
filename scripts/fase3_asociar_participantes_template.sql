-- Fase 3 · Plantilla privada para asociar U01–U10
-- NO completar UUID reales en Git. Copiar este archivo localmente y reemplazar
-- solamente en la copia privada ejecutada por el responsable autorizado.
-- Ejecutar únicamente después de desplegar M0028 y verificar producción.
--
-- IMPORTANTE: los marcadores <REEMPLAZAR_...> son deliberadamente inválidos.
-- La plantilla debe fallar si alguien intenta ejecutarla sin personalizarla.

BEGIN;

INSERT INTO piloto.participantes
    (usuario_id, codigo_anonimo, fecha_inicio, fecha_fin_planificada, activo)
VALUES
    ('<REEMPLAZAR_U01_UUID>', 'U01', DATE '2026-08-17', DATE '2026-09-14', true),
    ('<REEMPLAZAR_U02_UUID>', 'U02', DATE '2026-08-17', DATE '2026-09-14', true),
    ('<REEMPLAZAR_U03_UUID>', 'U03', DATE '2026-08-17', DATE '2026-09-14', true),
    ('<REEMPLAZAR_U04_UUID>', 'U04', DATE '2026-08-18', DATE '2026-09-15', true),
    ('<REEMPLAZAR_U05_UUID>', 'U05', DATE '2026-08-19', DATE '2026-09-16', true),
    ('<REEMPLAZAR_U06_UUID>', 'U06', DATE '2026-08-19', DATE '2026-09-16', true),
    ('<REEMPLAZAR_U07_UUID>', 'U07', DATE '2026-08-25', DATE '2026-09-22', true),
    ('<REEMPLAZAR_U08_UUID>', 'U08', DATE '2026-08-25', DATE '2026-09-22', true),
    ('<REEMPLAZAR_U09_UUID>', 'U09', DATE '2026-08-28', DATE '2026-09-25', true),
    ('<REEMPLAZAR_U10_UUID>', 'U10', DATE '2026-08-31', DATE '2026-09-28', true)
ON CONFLICT (usuario_id) DO UPDATE SET
    codigo_anonimo = EXCLUDED.codigo_anonimo,
    fecha_inicio = EXCLUDED.fecha_inicio,
    fecha_fin_planificada = EXCLUDED.fecha_fin_planificada,
    activo = EXCLUDED.activo;

-- Verificación sanitizada: no devolver usuario_id.
SELECT codigo_anonimo, fecha_inicio, fecha_fin_planificada, activo
FROM piloto.participantes
ORDER BY codigo_anonimo;

-- Sustituir ROLLBACK por COMMIT únicamente en la copia privada cuando se hayan
-- reemplazado y verificado los diez UUID reales.
ROLLBACK;
