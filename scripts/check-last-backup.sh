#!/usr/bin/env bash
set -euo pipefail
dir="${FIXAR_BACKUP_DIR:?FIXAR_BACKUP_DIR zorunlu}"
database="${FIXAR_PG_DATABASE:?FIXAR_PG_DATABASE zorunlu}"
max_age_hours="${FIXAR_BACKUP_MAX_AGE_HOURS:-26}"
latest="$(find "$dir" -type f -name "${database}_*.dump" -print0 | xargs -0 ls -1t 2>/dev/null | head -1 || true)"
[[ -n "$latest" && -s "$latest" ]] || { printf 'BACKUP_STATUS=missing\n' >&2; exit 2; }
now="$(date +%s)"
mtime="$(stat -f '%m' "$latest" 2>/dev/null || stat -c '%Y' "$latest")"
age_hours="$(( (now - mtime) / 3600 ))"
[[ "$age_hours" -le "$max_age_hours" ]] || { printf 'BACKUP_STATUS=stale age_hours=%s file=%s\n' "$age_hours" "$latest" >&2; exit 3; }
manifest="$latest.manifest.json"
[[ -s "$manifest" ]] || { printf 'BACKUP_STATUS=manifest_missing file=%s\n' "$latest" >&2; exit 4; }
expected="$(sed -n 's/.*"sha256": "\([0-9a-f]*\)".*/\1/p' "$manifest")"
actual="$(shasum -a 256 "$latest" | awk '{print $1}')"
[[ -n "$expected" && "$expected" == "$actual" ]] || { printf 'BACKUP_STATUS=checksum_failed\n' >&2; exit 5; }
printf 'BACKUP_STATUS=healthy age_hours=%s file=%s\n' "$age_hours" "$latest"
