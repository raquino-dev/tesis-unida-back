#!/usr/bin/env sh
set -eu

: "${IMAGE_REGISTRY:?Configure IMAGE_REGISTRY, por ejemplo ghcr.io/organizacion}"
tag=${1:?Uso: IMAGE_REGISTRY=ghcr.io/organizacion rollback.sh SHA_PREVIO}

deploy_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
env_file=${ENV_FILE:-"$deploy_dir/.env.production"}
backup="$env_file.rollback"

cp "$env_file" "$backup"
sed -i \
  -e "s|^API_IMAGE=.*|API_IMAGE=$IMAGE_REGISTRY/finanzas-api:$tag|" \
  -e "s|^WORKER_IMAGE=.*|WORKER_IMAGE=$IMAGE_REGISTRY/finanzas-worker:$tag|" \
  -e "s|^MIGRATOR_IMAGE=.*|MIGRATOR_IMAGE=$IMAGE_REGISTRY/finanzas-migrator:$tag|" \
  "$env_file"

if "$deploy_dir/scripts/deploy.sh"; then
  rm -f "$backup"
  echo "Rollback completado al artefacto $tag."
  exit 0
fi

mv "$backup" "$env_file"
echo "El rollback falló; se restauró el archivo de configuración anterior." >&2
exit 1
