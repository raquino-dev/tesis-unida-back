#!/usr/bin/env sh
set -eu

deploy_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
env_file=${1:-"$deploy_dir/.env.production"}

if [ ! -f "$env_file" ]; then
  echo "No existe $env_file. Copie env.production.example y complete los secretos." >&2
  exit 1
fi

for secret in \
  supabase-ca.crt \
  data-protection.pfx \
  google-service-account.json \
  tls-fullchain.pem \
  tls-privkey.pem
do
  if [ ! -s "$deploy_dir/secrets/$secret" ]; then
    echo "Falta deploy/secrets/$secret o está vacío." >&2
    exit 1
  fi
done

docker compose \
  --project-directory "$deploy_dir" \
  --env-file "$env_file" \
  -f "$deploy_dir/compose.production.yaml" \
  config --quiet

echo "Configuración de producción válida."
