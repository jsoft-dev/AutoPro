-- One-off: reset AP-2026-00006 on production (paste into Render Postgres shell).
\set ON_ERROR_STOP on

BEGIN;

WITH target AS (
  SELECT "Id" AS job_id FROM "ServiceJobs" WHERE "JobNumber" = 'AP-2026-00006'
)
UPDATE "InventoryItems" i
SET
  "QuantityOnHand" = i."QuantityOnHand" + pl."Quantity",
  "UpdatedAtUtc" = NOW()
FROM "JobPurchaseLines" pl, target t
WHERE pl."ServiceJobId" = t.job_id
  AND pl."InventoryItemId" = i."Id";

WITH target AS (
  SELECT "Id" AS job_id FROM "ServiceJobs" WHERE "JobNumber" = 'AP-2026-00006'
)
DELETE FROM "JobPurchaseLines" pl USING target t WHERE pl."ServiceJobId" = t.job_id;

UPDATE "ServiceJobs"
SET "Status" = 0, "CompletedAtUtc" = NULL, "ClosedAtUtc" = NULL
WHERE "JobNumber" = 'AP-2026-00006';

WITH target AS (
  SELECT "Id" AS job_id FROM "ServiceJobs" WHERE "JobNumber" = 'AP-2026-00006'
)
DELETE FROM "JobStatusHistories" h USING target t
WHERE h."ServiceJobId" = t.job_id
  AND NOT (h."FromStatus" = 0 AND h."ToStatus" = 0 AND h."Note" = 'Job taken at front desk.');

COMMIT;
