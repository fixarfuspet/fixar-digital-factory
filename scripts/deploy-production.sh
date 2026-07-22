#!/usr/bin/env bash
source "$(dirname "$0")/production-common.sh"
require_production_env
execute=false
[[ "${1:-}" == "--execute" ]] && execute=true
release="${FIXAR_RELEASE:-$(git -C "$ROOT" rev-parse HEAD)}"
printf 'DEPLOY_PLAN release=%s env=%s\n' "$release" "$ENV_FILE"
printf '1 validate compose\n2 require verified backup\n3 build images\n4 apply reviewed migration SQL\n5 start services\n6 health check\n'
$execute || { printf 'DRY_RUN_ONLY: --execute gerekli.\n'; exit 0; }
[[ -s "${FIXAR_VERIFIED_BACKUP:-}" ]] || { printf 'FIXAR_VERIFIED_BACKUP zorunlu.\n' >&2; exit 4; }
compose config --quiet
compose build --pull
[[ -s "${FIXAR_MIGRATION_SQL:-}" ]] || { printf 'FIXAR_MIGRATION_SQL zorunlu.\n' >&2; exit 4; }
compose exec -T postgres psql -v ON_ERROR_STOP=1 -U "${POSTGRES_USER:?}" -d "${POSTGRES_DB:?}" < "$FIXAR_MIGRATION_SQL"
compose up -d --remove-orphans
"$ROOT/scripts/health-check.sh"
printf '%s\n' "$release" > "$ROOT/.last-successful-release"
