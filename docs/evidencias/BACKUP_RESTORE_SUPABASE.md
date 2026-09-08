# EVID-E2E-01 · Procedimiento de backup y restauración Supabase

## Alcance

Validar recuperación antes de cualquier merge que pueda ejecutar migraciones sobre la base productiva del piloto.

## Evidencia mínima

Conservar, en versión RAW y luego sanitizada:

1. Estado del proyecto productivo Supabase Pro y evidencia de backup administrado disponible.
2. Terminal con versión de `pg_dump`/`pg_restore`.
3. Inicio y finalización del dump lógico sin mostrar connection string.
4. SHA-256 del dump lógico.
5. Cifrado exitoso y SHA-256 del archivo cifrado.
6. Proyecto temporal de restauración identificado sólo con un alias técnico.
7. Restauración exitosa.
8. Comparación de estructura y conteos agregados.
9. Aplicación controlada de `M0028_AlineacionPilotoTesis` sobre el restore.
10. Verificación posterior de integridad y elegibilidad del piloto.

## Variables locales

No guardar URLs ni credenciales en Git. Configurarlas solamente en la terminal del responsable:

```bash
export PROD_DB_URL='postgresql://...'
export RESTORE_DB_URL='postgresql://...'
export BACKUP_PASSPHRASE='...'
```

No capturar la terminal mientras se ingresan estas variables.

## Dump lógico

Para evitar incluir objetos administrados ajenos a la aplicación, el responsable debe confirmar los esquemas existentes y ajustar `APP_SCHEMAS` si fuera necesario.

```bash
TS=$(date +%Y%m%d_%H%M%S)
OUT="finanzas_prod_${TS}.dump"

APP_SCHEMAS=(
  identidad finanzas familias documentos analitica
  suscripciones infraestructura piloto sincronizacion
)

ARGS=()
for schema in "${APP_SCHEMAS[@]}"; do
  ARGS+=(--schema="$schema")
done

pg_dump "$PROD_DB_URL" \
  --format=custom \
  --no-owner \
  --no-privileges \
  "${ARGS[@]}" \
  --file="$OUT"

sha256sum "$OUT" | tee "${OUT}.sha256"
```

Si la instalación usa `public.__EFMigrationsHistory`, incorporarla explícitamente al dump o verificarla por separado antes de restaurar.

## Cifrado

Ejemplo con OpenSSL AES-256 y PBKDF2:

```bash
openssl enc -aes-256-cbc -salt -pbkdf2 \
  -in "$OUT" \
  -out "${OUT}.enc" \
  -pass env:BACKUP_PASSPHRASE

sha256sum "${OUT}.enc" | tee "${OUT}.enc.sha256"
```

El archivo sin cifrar no debe quedar en ubicaciones sincronizadas o compartidas. Una vez comprobado el cifrado y preservado el hash necesario para la evidencia, mover/eliminar la copia sin cifrar conforme a la política local.

## Restauración aislada

Crear un proyecto Supabase temporal distinto de producción. Antes de restaurar, verificar que `RESTORE_DB_URL` apunta al proyecto temporal.

```bash
pg_restore \
  --dbname="$RESTORE_DB_URL" \
  --no-owner \
  --no-privileges \
  --exit-on-error \
  "$OUT"
```

Si el archivo sin cifrar ya fue eliminado, descifrarlo sólo en una ruta local temporal:

```bash
openssl enc -d -aes-256-cbc -pbkdf2 \
  -in "${OUT}.enc" \
  -out "/tmp/${OUT}" \
  -pass env:BACKUP_PASSPHRASE

sha256sum "/tmp/${OUT}"
```

El SHA-256 del dump descifrado debe coincidir con el hash generado antes del cifrado.

## Verificación de integridad

No usar filas ni datos personales como evidencia. Comparar únicamente metadatos y conteos agregados de tablas críticas entre producción y restore.

Verificar como mínimo:

- esquemas de la aplicación presentes;
- tablas críticas presentes;
- historial EF consistente;
- conteos agregados de usuarios, instrumentos, respuestas, movimientos y grupos familiares;
- ausencia de errores de FK/constraints reportados durante restore;
- consultas de lectura representativas correctas.

Los conteos se guardan en RAW y la evidencia sanitizada puede mostrar igualdad (`producción = restore`) sin exponer valores si el volumen pudiera resultar identificable.

## Prueba de M0028 sobre restore

Después de validar que el restore reproduce la base previa al cambio:

1. construir el migrador desde la rama autorizada;
2. apuntarlo exclusivamente a `RESTORE_DB_URL`;
3. ejecutar migraciones;
4. comprobar que `M0028_AlineacionPilotoTesis` aparece en el historial;
5. validar:
   - precios 39.000 / 390.000;
   - `piloto.participantes` creada;
   - `preuso-complementario` activo;
   - `postuso` v2 activo y versión previa preservada;
   - respuestas históricas intactas;
   - política sin placeholder `example.invalid`.

## Gate de aprobación

Sólo marcar EVID-E2E-01 como `APROBADA` cuando:

- backup administrado comprobado;
- dump lógico generado;
- SHA-256 registrado;
- copia cifrada comprobada;
- restore aislado finaliza sin error crítico;
- integridad comprobada;
- M0028 probada exitosamente en el restore.

Hasta ese momento PR #22 y PR #16 permanecen sin merge.
