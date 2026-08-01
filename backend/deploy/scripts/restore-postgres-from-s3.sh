#!/usr/bin/env sh
set -eu

: "${RESTORE_DATABASE_URL:?Configure RESTORE_DATABASE_URL; nunca use producción para una prueba}"
: "${BACKUP_ENCRYPTION_PASSWORD:?Configure BACKUP_ENCRYPTION_PASSWORD}"

object=${1:?Uso: restore-postgres-from-s3.sh s3://bucket/ruta/respaldo.dump.gz.enc}
confirmation=${2:-}
if [ "$confirmation" != "CONFIRMAR_RESTAURACION" ]; then
  echo "Restauración cancelada. Añada CONFIRMAR_RESTAURACION como segundo argumento." >&2
  exit 1
fi

aws s3 cp "$object" - --only-show-errors \
  | openssl enc -d -aes-256-cbc -pbkdf2 \
      -pass env:BACKUP_ENCRYPTION_PASSWORD \
  | gzip -d \
  | pg_restore \
      --dbname="$RESTORE_DATABASE_URL" \
      --clean --if-exists --no-owner --no-acl --exit-on-error

echo "Restauración completada y validada por pg_restore."
