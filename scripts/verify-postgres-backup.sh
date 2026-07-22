#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=postgres-common.sh
source "$SCRIPT_DIR/postgres-common.sh"

require_env FIXAR_PG_USER
require_env FIXAR_PG_DATABASE
require_env FIXAR_RESTORE_DATABASE
reject_production_target "$FIXAR_PG_DATABASE"
reject_production_target "$FIXAR_RESTORE_DATABASE"
PSQL="$(find_pg_tool psql)"
args=(-h "${FIXAR_PG_HOST:-127.0.0.1}" -p "${FIXAR_PG_PORT:-5432}" -U "$FIXAR_PG_USER" -Atq)

tables=(Users Roles Customers Products Orders StockItems AuditLogs)
status=pass
details=()
for table in "${tables[@]}"; do
  exists="$($PSQL "${args[@]}" -d "$FIXAR_PG_DATABASE" -c "select to_regclass('public.\"$table\"') is not null")"
  [[ "$exists" == "t" ]] || continue
  source_count="$($PSQL "${args[@]}" -d "$FIXAR_PG_DATABASE" -c "select count(*) from \"$table\"")"
  restore_count="$($PSQL "${args[@]}" -d "$FIXAR_RESTORE_DATABASE" -c "select count(*) from \"$table\"")"
  [[ "$source_count" == "$restore_count" ]] || status=fail
  details+=("{\"table\":\"$table\",\"source\":$source_count,\"restored\":$restore_count}")
done

report="${FIXAR_VERIFY_REPORT:-$SCRIPT_DIR/../artifacts/postgres-backup-verification.json}"
mkdir -p "$(dirname "$report")"
{
  printf '{\n  "status":"%s",\n  "sourceDatabase":"%s",\n  "restoreDatabase":"%s",\n  "tables":[\n' \
    "$status" "$FIXAR_PG_DATABASE" "$FIXAR_RESTORE_DATABASE"
  for index in "${!details[@]}"; do
    [[ "$index" -eq 0 ]] || printf ',\n'
    printf '    %s' "${details[$index]}"
  done
  printf '\n  ]\n}\n'
} > "$report"
printf 'VERIFY_REPORT=%s\nSTATUS=%s\n' "$report" "$status"
[[ "$status" == "pass" ]]
