# Participantes y ventanas del piloto

## Regla metodológica

La muestra principal está compuesta por 10 unidades familiares de Asunción, cada una representada por un adulto referente (`n=10`). Otros adultos del grupo familiar pueden utilizar las capacidades colaborativas, pero no se incorporan automáticamente como observaciones independientes del análisis pre/post.

Cada referente dispone de una ventana de observación de 28 días contados desde su incorporación individual. El cuestionario `postuso` se habilita a partir de `fecha_inicio + 28 días`.

## Fechas aprobadas

| Código | Inicio | Postuso habilitado desde |
|---|---|---|
| U01 | 17/08/2026 | 14/09/2026 |
| U02 | 17/08/2026 | 14/09/2026 |
| U03 | 17/08/2026 | 14/09/2026 |
| U04 | 18/08/2026 | 15/09/2026 |
| U05 | 19/08/2026 | 16/09/2026 |
| U06 | 19/08/2026 | 16/09/2026 |
| U07 | 25/08/2026 | 22/09/2026 |
| U08 | 25/08/2026 | 22/09/2026 |
| U09 | 28/08/2026 | 25/09/2026 |
| U10 | 31/08/2026 | 28/09/2026 |

## Asociación con usuarios reales

La migración M0023 crea `piloto.participantes`, pero no incorpora identificadores reales al repositorio. Después de aplicar la migración, la asociación `usuario_id ↔ Uxx` debe realizarse directamente en el entorno controlado, sin almacenar correos, nombres ni UUID reales en Git.

Plantilla orientativa:

```sql
INSERT INTO piloto.participantes
    (usuario_id, codigo_anonimo, fecha_inicio, fecha_fin_planificada)
VALUES
    ('<UUID_U01>', 'U01', DATE '2026-08-17', DATE '2026-09-14'),
    ('<UUID_U02>', 'U02', DATE '2026-08-17', DATE '2026-09-14'),
    ('<UUID_U03>', 'U03', DATE '2026-08-17', DATE '2026-09-14'),
    ('<UUID_U04>', 'U04', DATE '2026-08-18', DATE '2026-09-15'),
    ('<UUID_U05>', 'U05', DATE '2026-08-19', DATE '2026-09-16'),
    ('<UUID_U06>', 'U06', DATE '2026-08-19', DATE '2026-09-16'),
    ('<UUID_U07>', 'U07', DATE '2026-08-25', DATE '2026-09-22'),
    ('<UUID_U08>', 'U08', DATE '2026-08-25', DATE '2026-09-22'),
    ('<UUID_U09>', 'U09', DATE '2026-08-28', DATE '2026-09-25'),
    ('<UUID_U10>', 'U10', DATE '2026-08-31', DATE '2026-09-28');
```

No ejecutar esta plantilla sin reemplazar y verificar cada UUID en el entorno correspondiente.

## Despliegue

Como el merge de `main` despliega automáticamente y ejecuta migraciones sobre la base del piloto, no se debe fusionar la rama de Fase 2 hasta completar y evidenciar un backup y una restauración aislada. La asociación de participantes se realiza después de confirmar que M0023 fue aplicada correctamente.
