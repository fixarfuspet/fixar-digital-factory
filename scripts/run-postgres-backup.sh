#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="${FIXAR_BACKUP_ENV_FILE:-/etc/fixar-os/backup.env}"
[[ -r "$env_file" ]] || { printf 'Backup env okunamıyor: %s\n' "$env_file" >&2; exit 2; }
set -a
# shellcheck disable=SC1090
source "$env_file"
set +a
require=(FIXAR_PG_USER FIXAR_PG_DATABASE FIXAR_BACKUP_DIR)
for name in "${require[@]}"; do [[ -n "${!name:-}" ]] || { printf 'Eksik: %s\n' "$name" >&2; exit 2; }; done
mkdir -p "${FIXAR_BACKUP_LOG_DIR:-$FIXAR_BACKUP_DIR/logs}"
log="${FIXAR_BACKUP_LOG_DIR:-$FIXAR_BACKUP_DIR/logs}/backup-$(date -u +%Y%m%d).log"
if "$ROOT/scripts/backup-postgres.sh" >>"$log" 2>&1; then
  printf '%s success\n' "$(date -u +%FT%TZ)" >>"$log"
else
  code=$?
  printf '%s failure exit=%s\n' "$(date -u +%FT%TZ)" "$code" >>"$log"
  exit "$code"
fi
