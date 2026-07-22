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
confirm=false
production_confirm=false
for arg in "$@"; do
  [[ "$arg" == --confirm-test-data-reset ]] && confirm=true
  [[ "$arg" == --confirm-production-test-data-reset ]] && production_confirm=true
done

inventory=$(cat <<'SQL'
SELECT 'Customers' entity,count(*) count,coalesce(min("CustomerCode"),'') example FROM "Customers" WHERE "CustomerCode" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%'
UNION ALL SELECT 'Products',count(*),coalesce(min("Code"),'') FROM "Products" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%'
UNION ALL SELECT 'Materials',count(*),coalesce(min("Code"),'') FROM "Materials" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%'
UNION ALL SELECT 'Recipes',count(*),coalesce(min("Code"),'') FROM "Recipes" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%'
UNION ALL SELECT 'Molds',count(*),coalesce(min("Code"),'') FROM "Molds" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%'
UNION ALL SELECT 'Quotes',count(*),coalesce(min("QuoteNumber"),'') FROM "Quotes" WHERE "QuoteNumber" ILIKE 'TEST-%' OR coalesce("Notes",'') ILIKE 'TEST%'
UNION ALL SELECT 'Orders',count(*),coalesce(min("OrderNumber"),'') FROM "Orders" WHERE "OrderNumber" ILIKE 'TEST-%'
UNION ALL SELECT 'WorkOrders',count(*),coalesce(min("WorkOrderNumber"),'') FROM "WorkOrders" WHERE "WorkOrderNumber" ILIKE 'TEST-%'
UNION ALL SELECT 'StationAssignments',count(*),coalesce(min("OperatorName"),'') FROM "StationAssignments" WHERE coalesce("OperatorName",'') ILIKE 'TEST%' OR coalesce("Note",'') ILIKE 'TEST%'
UNION ALL SELECT 'StockMovements',count(*),coalesce(min("SourceDocumentNo"),'') FROM "StockMovements" WHERE coalesce("SourceDocumentNo",'') ILIKE 'TEST-%' OR coalesce("Note",'') ILIKE 'TEST%'
UNION ALL SELECT 'FinancialTransactions',count(*),coalesce(min("TransactionNumber"),'') FROM "FinancialTransactions" WHERE "TransactionNumber" ILIKE 'TEST-%' OR coalesce("Description",'') ILIKE 'TEST%'
ORDER BY 1;
SQL
)
printf 'DRY_RUN database=%s marker=TEST\n' "$FIXAR_PG_DATABASE"
"$PSQL" "${args[@]}" -P pager=off -c "$inventory"
$confirm || { printf 'NO_CHANGES: --confirm-test-data-reset verilmedi.\n'; exit 0; }

require_env FIXAR_BACKUP_FILE
[[ -s "$FIXAR_BACKUP_FILE" && -s "$FIXAR_BACKUP_FILE.manifest.json" ]] || { printf 'Doğrulanmış backup ve manifest zorunlu.\n' >&2; exit 4; }
expected="$(sed -n 's/.*"sha256": "\([0-9a-f]*\)".*/\1/p' "$FIXAR_BACKUP_FILE.manifest.json")"
actual="$(shasum -a 256 "$FIXAR_BACKUP_FILE" | awk '{print $1}')"
[[ -n "$expected" && "$expected" == "$actual" ]] || { printf 'Backup checksum doğrulanamadı.\n' >&2; exit 4; }
if [[ "${ASPNETCORE_ENVIRONMENT:-}" == Production || "${FIXAR_ENVIRONMENT:-}" == Production ]]; then
  $production_confirm && [[ "${FIXAR_PRODUCTION_RESET_APPROVAL:-}" == "DELETE-CONFIRMED-TEST-DATA" ]] || {
    printf 'Production ikinci onayı eksik.\n' >&2; exit 5;
  }
fi

cleanup=$(cat <<'SQL'
BEGIN;
-- Sistem tabloları, kullanıcılar, roller, istasyonlar, makineler ve migration geçmişi bu planda yoktur.
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM "Recipes" r WHERE (r."Code" ILIKE 'TEST-%' OR r."Name" ILIKE 'TEST%')
    AND EXISTS (SELECT 1 FROM "WorkOrders" w WHERE w."RecipeId"=r."Id")
  ) THEN RAISE EXCEPTION 'TEST recipe has historical work orders; cleanup refused'; END IF;
  IF EXISTS (
    SELECT 1 FROM "Materials" m WHERE (m."Code" ILIKE 'TEST-%' OR m."Name" ILIKE 'TEST%')
    AND (EXISTS (SELECT 1 FROM "MaterialLots" l WHERE l."MaterialId"=m."Id")
      OR EXISTS (SELECT 1 FROM "MaterialConsumptions" c WHERE c."MaterialId"=m."Id"))
  ) THEN RAISE EXCEPTION 'TEST material has lot/consumption history; cleanup refused'; END IF;
  IF EXISTS (
    SELECT 1 FROM "Customers" c WHERE (c."CustomerCode" ILIKE 'TEST-%' OR c."Name" ILIKE 'TEST%')
    AND (EXISTS (SELECT 1 FROM "Orders" o WHERE o."CustomerId"=c."Id")
      OR EXISTS (SELECT 1 FROM "Quotes" q WHERE q."CustomerId"=c."Id"))
  ) THEN RAISE EXCEPTION 'TEST customer has order/quote history; cleanup refused'; END IF;
END $$;
DELETE FROM "RecipeItems" WHERE "RecipeId" IN (SELECT "Id" FROM "Recipes" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%');
DELETE FROM "Recipes" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%';
DELETE FROM "Materials" WHERE "Code" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%';
DELETE FROM "Customers" WHERE "CustomerCode" ILIKE 'TEST-%' OR "Name" ILIKE 'TEST%';
-- Finans, stok hareketi, üretim ve audit geçmişi otomatik silinmez.
SET CONSTRAINTS ALL IMMEDIATE;
COMMIT;
SQL
)
printf 'APPLY_START transaction=true\n'
"$PSQL" "${args[@]}" -c "$cleanup"
printf 'AFTER\n'
"$PSQL" "${args[@]}" -P pager=off -c "$inventory"
"$PSQL" "${args[@]}" -Atqc "select count(*) from pg_constraint where contype='f' and not convalidated" | grep -qx 0
printf 'TEST_DATA_RESET=success\n'
