# Fase 2 · Alineación tesis ↔ backend

Rama: `tesis/fase-2-alineacion-2026-09`

## Decisiones aplicadas

- RF-01 a RF-26 pasan a ser el alcance funcional formal.
- Piloto: 10 unidades familiares de Asunción, un referente adulto por unidad (`n=10`).
- Incorporación escalonada: 17/08/2026–31/08/2026.
- Observación: 28 días por unidad familiar desde su incorporación.
- Premium: Gs. 39.000 mensual y Gs. 390.000 anual; sin cobros reales en el piloto.
- Android es la única plataforma de validación.
- RF-26 offline permanece dentro del alcance.

## Cambios técnicos

- `M0023_AlineacionPilotoTesis` corrige los precios vigentes sin modificar `M0009` histórico.
- `M0023` agrega `preuso-complementario` como instrumento separado, solicitado por tutoría y de análisis descriptivo.
- `M0023` publica `postuso` v2.0, preservando la versión 1.0 y cualquier respuesta histórica.
- `InstrumentosPilotoHandler` admite el código `preuso-complementario`.
- La documentación de alcance y trazabilidad se extiende formalmente hasta RF-26.

## Preservación

No se reescriben migraciones históricas ni respuestas de instrumentos ya contestados. Los cambios académicos que alteran instrumentos se publican mediante nuevas versiones/códigos.

## Evidencia que corresponde a fases posteriores

Esta fase no convierte pruebas con mocks en E2E. S3, Textract, SES, FCM, Billing, backup/restore, sincronización multi-dispositivo y métricas de disponibilidad deben conservar evidencia separada durante la fase de pruebas/evidencias.
