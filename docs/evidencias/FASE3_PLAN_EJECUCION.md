# Fase 3 · Pruebas reproducibles y expediente de evidencias

## Objetivo

Convertir las afirmaciones técnicas del proyecto en evidencia reproducible, sanitizada y trazable antes de cerrar los resultados de la tesis.

## Regla de oro

No se modifica producción ni se ejecuta `M0028_AlineacionPilotoTesis` hasta completar y documentar un respaldo real y una restauración aislada verificable.

## Orden de ejecución

1. **EVID-E2E-01 · Supabase / recuperación**
   - Confirmar backup administrado de Supabase Pro.
   - Generar `pg_dump` lógico desde producción por el responsable autorizado.
   - Calcular SHA-256 del dump.
   - Cifrar la copia lógica antes de almacenarla.
   - Restaurar en proyecto Supabase temporal.
   - Verificar integridad estructural y conteos agregados sin exponer PII.
   - Probar `M0028` sobre el restore, no sobre producción.
2. **Gate de merge backend**
   - CI verde.
   - Backup/restore aprobado.
   - `M0028` aplicada correctamente en ambiente aislado.
   - Recién entonces autorizar merge del backend; `main` despliega automáticamente.
3. **Verificación post-deploy**
   - GitHub Actions correcto.
   - Migrador finaliza correctamente.
   - API/Worker inician sin errores críticos.
   - Health checks correctos.
   - Asociar U01–U10 en `piloto.participantes` fuera de Git.
4. **Build Android controlada**
   - Publicar `0.1.0+9` tras backend compatible.
   - Consentimiento `participacion-piloto-v2` obligatorio.
   - `preuso-complementario` disponible.
   - Postuso bloqueado hasta `fecha_inicio + 28 días`, usando calendario de `America/Asuncion`.
5. **EVID-E2E-02..08**
   - S3.
   - Textract/OCR.
   - SES.
   - FCM.
   - Google Play/Billing.
   - Lightsail/Cloudflare/TLS.
   - Offline/sincronización/conflictos.
6. **RNF y benchmark**
   - Rendimiento con máximo 10 usuarios virtuales técnicos, baja carga y acciones no destructivas.
   - Tiempo OCR: envío → resultado disponible para revisión.
   - Monitor HTTP de disponibilidad desde la fecha real de inicio de medición.
   - Benchmark OCR con ground truth (ideal 100; mínimo pragmático 30–50 si se documenta la limitación).
7. **Pipeline académico**
   - Exportación sólo lectura y anonimizada U01–U10.
   - Preparar CSV/SQL y estructura de análisis.
   - No cerrar resultados pre/post hasta completar U10.

## Dispositivos disponibles

- 1 Android físico.
- 1 emulador Android.

Esta combinación se considera suficiente para pruebas controladas de FCM, offline y conflicto. Si un escenario requiriera dos dispositivos físicos, se registra la limitación en lugar de afirmar una cobertura inexistente.

## Separación de evidencia

- `EVIDENCIA_RAW`: privada, fuera de Git; puede contener información operacional sensible y debe almacenarse localmente con respaldo privado.
- `EVIDENCIA_SANITIZADA`: sin PII, secretos, correos, UUID vinculables, comprobantes identificables ni montos individualizables; apta para anexos y defensa.

## Incidencias históricas a conservar

- Configuración inicial de OCR/Textract.
- Duplicación de correos de suscripción y mitigaciones SES/outbox.
- Restauración de compras y corrección posterior.
- Fallos de pipeline por migraciones/health checks.
- UX: reportes vacíos, grupos/categorías, consentimiento repetido y mensajes mal codificados.
- Mejoras de offline, selectores, comprobantes adjuntos, alias y gráficos.

Las incidencias se documentan como evolución del producto; no se ocultan ni se presentan como fallos vigentes si ya fueron corregidas y verificadas.
