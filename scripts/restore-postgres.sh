#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=postgres-common.sh
source "$SCRIPT_DIR/postgres-common.sh"

require_env FIXAR_PG_USER
require_env FIXAR_RESTORE_DATABASE
require_env FIXAR_BACKUP_FILE
reject_production_target "$FIXAR_RESTORE_DATABASE"
[[ -s "$FIXAR_BACKUP_FILE" ]] || { printf 'Geçerli yedek bulunamadı.\n' >&2; exit 2; }
[[ "$FIXAR_RESTORE_DATABASE" != "${FIXAR_PG_DATABASE:-}" ]] || { printf 'Kaynak veritabanının üzerine restore reddedildi.\n' >&2; exit 3; }

CREATEDB="$(find_pg_tool createdb)"
DROPDB="$(find_pg_tool dropdb)"
PG_RESTORE="$(find_pg_tool pg_restore)"
args=(-h "${FIXAR_PG_HOST:-127.0.0.1}" -p "${FIXAR_PG_PORT:-5432}" -U "$FIXAR_PG_USER")

if [[ "${FIXAR_RECREATE_RESTORE_DATABASE:-false}" == "true" ]]; then
  "$DROPDB" "${args[@]}" --if-exists "$FIXAR_RESTORE_DATABASE"
  "$CREATEDB" "${args[@]}" "$FIXAR_RESTORE_DATABASE"
fi

"$PG_RESTORE" "${args[@]}" --exit-on-error --no-owner --no-privileges --dbname "$FIXAR_RESTORE_DATABASE" "$FIXAR_BACKUP_FILE"
printf 'RESTORED_DATABASE=%s\n' "$FIXAR_RESTORE_DATABASE"
