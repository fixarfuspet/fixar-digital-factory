#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT/docker-compose.production.yml"
ENV_FILE="${FIXAR_ENV_FILE:-$ROOT/.env.production}"
require_production_env() {
  [[ -s "$ENV_FILE" ]] || { printf 'Production env bulunamadı: %s\n' "$ENV_FILE" >&2; exit 2; }
  grep -Eq '^JWT_SECRET=.{64,}$' "$ENV_FILE" || { printf 'JWT_SECRET en az 64 karakter olmalıdır.\n' >&2; exit 3; }
  ! grep -Eq 'change-me|REPLACE_WITH|FIXAR_DEV_TEST_PASSWORD' "$ENV_FILE" || { printf 'Placeholder/development secret reddedildi.\n' >&2; exit 3; }
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
}
compose() { docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "$@"; }
