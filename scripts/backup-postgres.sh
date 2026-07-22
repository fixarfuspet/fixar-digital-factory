#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=postgres-common.sh
source "$SCRIPT_DIR/postgres-common.sh"

require_env FIXAR_PG_USER
require_env FIXAR_PG_DATABASE
reject_production_target "$FIXAR_PG_DATABASE"

PG_DUMP="$(find_pg_tool pg_dump)"
PSQL="$(find_pg_tool psql)"
OUTPUT_DIR="${FIXAR_BACKUP_DIR:-$SCRIPT_DIR/../artifacts/postgres-backups}"
mkdir -p "$OUTPUT_DIR"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_file="$OUTPUT_DIR/${FIXAR_PG_DATABASE}_${timestamp}.dump"
manifest_file="$backup_file.manifest.json"
args=(-h "${FIXAR_PG_HOST:-127.0.0.1}" -p "${FIXAR_PG_PORT:-5432}" -U "$FIXAR_PG_USER")

"$PG_DUMP" "${args[@]}" --format=custom --no-owner --no-privileges --file "$backup_file" "$FIXAR_PG_DATABASE"
[[ -s "$backup_file" ]] || { printf 'Yedek boş: %s\n' "$backup_file" >&2; exit 4; }

db_size="$($PSQL "${args[@]}" -d "$FIXAR_PG_DATABASE" -Atqc 'select pg_database_size(current_database())')"
table_count="$($PSQL "${args[@]}" -d "$FIXAR_PG_DATABASE" -Atqc "select count(*) from information_schema.tables where table_schema='public' and table_type='BASE TABLE'")"
backup_size="$(stat -f '%z' "$backup_file" 2>/dev/null || stat -c '%s' "$backup_file")"
checksum="$(shasum -a 256 "$backup_file" | awk '{print $1}')"

printf '{\n  "database": "%s",\n  "createdUtc": "%s",\n  "backupBytes": %s,\n  "databaseBytes": %s,\n  "tableCount": %s,\n  "sha256": "%s"\n}\n' \
  "$FIXAR_PG_DATABASE" "$timestamp" "$backup_size" "$db_size" "$table_count" "$checksum" > "$manifest_file"

printf 'BACKUP_FILE=%s\nMANIFEST_FILE=%s\n' "$backup_file" "$manifest_file"
