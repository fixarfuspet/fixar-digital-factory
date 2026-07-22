#!/usr/bin/env bash
set -euo pipefail
mode="${1:-}"
base="${FIXAR_HEALTH_BASE_URL:-https://localhost}"
log_dir="${FIXAR_LOG_DIR:-/var/log/fixar-os}"
disk_path="${FIXAR_DISK_PATH:-/}"
disk_limit="${FIXAR_DISK_LIMIT_PERCENT:-85}"
window_minutes="${FIXAR_MONITOR_WINDOW_MINUTES:-15}"

if [[ "$mode" == "--dry-run" ]]; then
  printf 'MONITOR_DRY_RUN base=%s log_dir=%s disk=%s limit=%s backup_check=%s\n' "$base" "$log_dir" "$disk_path" "$disk_limit" "${FIXAR_BACKUP_DIR:-not-configured}"
  exit 0
fi

status=healthy
failures=()
if ! FIXAR_HEALTH_BASE_URL="$base" "$(dirname "$0")/health-check.sh" >/dev/null; then failures+=(health); status=unhealthy; fi
used="$(df -P "$disk_path" | awk 'NR==2 {gsub(/%/,"",$5); print $5}')"
if [[ "$used" -ge "$disk_limit" ]]; then failures+=(disk); status=unhealthy; fi
if [[ -n "${FIXAR_BACKUP_DIR:-}" && -n "${FIXAR_PG_DATABASE:-}" ]]; then
  if ! "$(dirname "$0")/check-last-backup.sh" >/dev/null; then failures+=(backup); status=unhealthy; fi
else
  failures+=(backup_not_configured); status=unhealthy
fi

since_epoch="$(( $(date +%s) - window_minutes * 60 ))"
five_xx=0; login_failures=0; critical_jobs=0
if [[ -d "$log_dir" ]]; then
  five_xx="$(find "$log_dir" -type f -mmin "-$window_minutes" -name '*.log' -exec grep -Ehc 'HTTP[^ ]* 5[0-9]{2}|StatusCode[=: ]+5[0-9]{2}' {} + 2>/dev/null | awk '{s+=$1} END{print s+0}')"
  login_failures="$(find "$log_dir" -type f -mmin "-$window_minutes" -name '*.log' -exec grep -Ehic 'LOGIN_FAILED|invalid credentials|login failed' {} + 2>/dev/null | awk '{s+=$1} END{print s+0}')"
  critical_jobs="$(find "$log_dir" -type f -mmin "-$window_minutes" -name '*.log' -exec grep -Ehic 'critical job.*fail|background job.*fail' {} + 2>/dev/null | awk '{s+=$1} END{print s+0}')"
fi
[[ "$five_xx" -le "${FIXAR_MAX_5XX:-5}" ]] || { failures+=(five_xx); status=unhealthy; }
[[ "$critical_jobs" -eq 0 ]] || { failures+=(critical_jobs); status=unhealthy; }

joined="$(IFS=,; echo "${failures[*]:-}")"
printf '{"timestamp":"%s","status":"%s","diskPercent":%s,"http5xx":%s,"loginFailures":%s,"criticalJobFailures":%s,"failures":"%s"}\n' \
  "$(date -u +%FT%TZ)" "$status" "$used" "$five_xx" "$login_failures" "$critical_jobs" "$joined"
[[ "$status" == healthy ]]
