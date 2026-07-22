#!/usr/bin/env bash
source "$(dirname "$0")/production-common.sh"
require_production_env
printf 'ROLLBACK_PLAN maintenance -> stop writes -> restore verified backup to new DB -> previous images -> health\n'
[[ "${1:-}" == "--execute" ]] || { printf 'DRY_RUN_ONLY: --execute gerekli.\n'; exit 0; }
[[ "${FIXAR_CONFIRM_ROLLBACK:-}" == "ROLLBACK" ]] || { printf 'FIXAR_CONFIRM_ROLLBACK=ROLLBACK zorunlu.\n' >&2; exit 4; }
[[ -n "${FIXAR_PREVIOUS_RELEASE:-}" ]] || { printf 'FIXAR_PREVIOUS_RELEASE zorunlu.\n' >&2; exit 4; }
export FIXAR_RELEASE="$FIXAR_PREVIOUS_RELEASE"
compose up -d --no-deps api web nginx
"$ROOT/scripts/health-check.sh"
