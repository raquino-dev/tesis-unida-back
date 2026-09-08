# Fase 2 · Alineación tesis ↔ backend

Rama: `tesis/fase-2-alineacion-2026-09`

## Decisiones aplicadas

- RF-01 a RF-26 pasan a ser el alcance funcional formal.
- Piloto: 10 unidades familiares de Asunción, un referente adulto por unidad (`n=10`).
- Otros adultos de las familias pueden usar capacidades colaborativas, pero no forman observaciones independientes del pre/post.
- Incorporación escalonada: 17/08/2026–31/08/2026.
- Observación: 28 días por unidad familiar desde su incorporación individual.
- El postuso se habilita desde `fecha_inicio + 28 días` y el backend rechaza el envío anticipado.
- Premium: Gs. 39.000 mensual y Gs. 390.000 anual; sin cobros reales en el piloto.
- Product IDs de Google Play: `premium_monthly` y `premium_yearly`.
- Android es la única plataforma de validación.
- RF-26 offline permanece dentro del alcance.
- La política de privacidad permanece accesible dentro de la aplicación. No se agrega una web pública solo por la tesis; el placeholder histórico `example.invalid` se limpia en M0023.

## Cambios técnicos

- `M0023_AlineacionPilotoTesis` corrige los precios vigentes sin modificar `M0009` histórico.
- `M0023` crea `piloto.participantes` para registrar, sin PII adicional, el código anónimo y la ventana de 28 días.
- `M0023` agrega `preuso-complementario` como instrumento separado, solicitado por tutoría y de análisis descriptivo.
- `M0023` publica `postuso` v2.0, preservando la versión 1.0 y cualquier respuesta histórica.
- `InstrumentosPilotoHandler` admite el código `preuso-complementario` y controla la elegibilidad temporal del postuso.
- `IPilotoRepository`/`PilotoRepository` consultan la fecha final configurada para el referente autenticado.
- `docs/piloto/PARTICIPANTES_PILOTO.md` conserva únicamente U01–U10 y fechas; la relación con UUID reales se realiza fuera de Git.
- La documentación de alcance y trazabilidad se extiende formalmente hasta RF-26.

## Preservación

No se reescriben migraciones históricas ni respuestas de instrumentos ya contestados. Los cambios académicos que alteran instrumentos se publican mediante nuevas versiones/códigos. El preuso original sigue siendo la línea basal; el bloque complementario solicitado durante el piloto no se presenta como pretest retroactivo.

## Regla de despliegue

El merge de `main` despliega automáticamente y ejecuta migraciones contra la base del piloto. Por existir datos reales en Supabase, **no fusionar este PR hasta completar un backup y una restauración aislada con evidencia**. Luego aplicar el orden: backup/restore validado → merge backend/M0023 → verificación → asociación U01–U10 → distribución de la nueva AAB.

## Evidencia que corresponde a fases posteriores

Esta fase no convierte pruebas con mocks en E2E. S3, Textract, SES, FCM, Billing, backup/restore, sincronización multi-dispositivo y métricas de disponibilidad deben conservar evidencia separada durante la fase de pruebas/evidencias.
