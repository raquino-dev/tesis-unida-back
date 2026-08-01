#!/usr/bin/env sh
set -eu

: "${BACKUP_DATABASE_URL:?Configure BACKUP_DATABASE_URL en formato postgresql://}"
: "${BACKUP_S3_URI:?Configure BACKUP_S3_URI, por ejemplo s3://bucket/piloto}"
: "${BACKUP_ENCRYPTION_PASSWORD:?Configure BACKUP_ENCRYPTION_PASSWORD}"

timestamp=$(date -u +%Y%m%dT%H%M%SZ)
object="$BACKUP_S3_URI/postgres-$timestamp.dump.gz.enc"

pg_dump "$BACKUP_DATABASE_URL" --format=custom --no-owner --no-acl \
  | gzip -9 \
  | openssl enc -aes-256-cbc -pbkdf2 -salt \
      -pass env:BACKUP_ENCRYPTION_PASSWORD \
  | aws s3 cp - "$object" --sse AES256 --only-show-errors

echo "Respaldo lógico cifrado creado: $object"
