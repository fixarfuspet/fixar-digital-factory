#!/usr/bin/env bash
set -euo pipefail
base="${FIXAR_HEALTH_BASE_URL:-https://localhost}"
curl_args=(--fail --silent --show-error --max-time 10)
if [[ "${1:-}" == "--dry-run" ]]; then
  printf 'HEALTH_DRY_RUN frontend=%s/login live=%s/health/live ready=%s/health/ready\n' "$base" "$base" "$base"
  exit 0
fi
[[ "${FIXAR_HEALTH_INSECURE:-false}" == true ]] && curl_args+=(--insecure)
curl "${curl_args[@]}" "$base/login" >/dev/null
curl "${curl_args[@]}" "$base/health/live" >/dev/null
curl "${curl_args[@]}" "$base/health/ready" >/dev/null
printf 'FIXAR_HEALTH=healthy base=%s\n' "$base"
