#!/usr/bin/env bash
set -euo pipefail

find_pg_tool() {
  local tool="$1"
  if command -v "$tool" >/dev/null 2>&1; then
    command -v "$tool"
    return
  fi

  local candidate
  for candidate in \
    "/Applications/Postgres.app/Contents/Versions/latest/bin/$tool" \
    "$HOME/Desktop/Postgres.app/Contents/Versions/latest/bin/$tool"; do
    if [[ -x "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return
    fi
  done

  printf 'PostgreSQL aracı bulunamadı: %s\n' "$tool" >&2
  exit 2
}

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    printf 'Zorunlu ortam değişkeni eksik: %s\n' "$name" >&2
    exit 2
  fi
}

reject_production_target() {
  local database="$1"
  if [[ "${FIXAR_ALLOW_PRODUCTION_DATABASE:-false}" != "true" ]] &&
     [[ "$database" =~ (^|[_-])(prod|production)($|[_-])|^fixar_os$ ]]; then
    printf 'Production benzeri veritabanı adı güvenlik nedeniyle reddedildi: %s\n' "$database" >&2
    exit 3
  fi
}

pg_args() {
  printf '%s\n' -h "${FIXAR_PG_HOST:-127.0.0.1}" -p "${FIXAR_PG_PORT:-5432}" -U "$FIXAR_PG_USER"
}
