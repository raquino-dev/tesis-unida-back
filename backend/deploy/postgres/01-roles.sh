#!/bin/sh
set -eu

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=api_password="$FINANZAS_API_PASSWORD" \
  --set=worker_password="$FINANZAS_WORKER_PASSWORD" <<'SQL'
CREATE ROLE finanzas_api NOLOGIN;
CREATE ROLE finanzas_worker NOLOGIN;
CREATE ROLE finanzas_api_local LOGIN PASSWORD :'api_password' IN ROLE finanzas_api;
CREATE ROLE finanzas_worker_local LOGIN PASSWORD :'worker_password' IN ROLE finanzas_worker;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
SQL
