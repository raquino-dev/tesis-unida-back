#!/usr/bin/env sh
set -eu

deploy_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
env_file=${ENV_FILE:-"$deploy_dir/.env.production"}
compose="docker compose --project-directory $deploy_dir --env-file $env_file -f $deploy_dir/compose.production.yaml"

"$deploy_dir/scripts/validate-config.sh" "$env_file"

# shellcheck disable=SC2086
$compose pull api worker migrator nginx
# La dependencia service_completed_successfully ejecuta primero el migrador.
# shellcheck disable=SC2086
if ! $compose up -d --no-build --remove-orphans --wait; then
  echo "El despliegue no pudo iniciar todos los servicios." >&2
  echo "Estado de los contenedores:" >&2
  # shellcheck disable=SC2086
  $compose ps >&2 || true
  echo "Últimos registros del migrador:" >&2
  # shellcheck disable=SC2086
  $compose logs --no-color --tail=200 migrator >&2 || true
  exit 1
fi

public_api_url=$(sed -n 's/^PUBLIC_API_URL=//p' "$env_file" | tail -n 1)
curl --fail --silent --show-error \
  --retry 8 --retry-delay 5 \
  "$public_api_url/salud/listo" >/dev/null

echo "Despliegue saludable: $public_api_url"
