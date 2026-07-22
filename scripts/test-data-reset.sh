#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=postgres-common.sh
source "$SCRIPT_DIR/postgres-common.sh"

require_env FIXAR_PG_USER
require_env FIXAR_PG_DATABASE
reject_production_target "$FIXAR_PG_DATABASE"
PSQL="$(find_pg_tool psql)"
args=(-X -v ON_ERROR_STOP=1 -h "${FIXAR_PG_HOST:-127.0.0.1}" -p "${FIXAR_PG_PORT:-5432}" -U "$FIXAR_PG_USER" -d "$FIXAR_PG_DATABASE")

query=$(cat <<'SQL'
WITH candidates AS (
  SELECT 'Customers' AS entity, COUNT(*) AS count FROM "Customers" WHERE "CustomerCode" LIKE 'TEST-%'
  UNION ALL SELECT 'Products', COUNT(*) FROM "Products" WHERE "Code" LIKE 'TEST-%'
  UNION ALL SELECT 'Orders', COUNT(*) FROM "Orders" WHERE "OrderNumber" LIKE 'TEST-%'
  UNION ALL SELECT 'Materials', COUNT(*) FROM "Materials" WHERE "Code" LIKE 'TEST-%'
  UNION ALL SELECT 'Suppliers', COUNT(*) FROM "Suppliers" WHERE "Code" LIKE 'TEST-%'
)
SELECT entity, count FROM candidates ORDER BY entity;
SQL
)

printf 'DRY_RUN database=%s marker=TEST-\n' "$FIXAR_PG_DATABASE"
"$PSQL" "${args[@]}" -P pager=off -c "$query"

if [[ "${FIXAR_TEST_DATA_APPLY:-false}" == "true" ]]; then
  require_env FIXAR_BACKUP_FILE
  [[ -s "$FIXAR_BACKUP_FILE" ]] || { printf 'Apply için geçerli backup zorunludur.\n' >&2; exit 4; }
  printf 'Apply modu henüz bilinçli olarak kapalıdır; ilişki sırası onaylanmadan veri silinmez.\n' >&2
  exit 5
fi
