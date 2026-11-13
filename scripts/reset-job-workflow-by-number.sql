-- Reset a job so purchases, invoice, completion, and payment can be recorded again.
-- Prefer POST /api/v1/admin/jobs/{id}/reset-workflow so invoice rows are removed in app code.
-- Usage (Render Postgres shell or local):
--   psql "$DATABASE_URL" -v job_number="'AP-2026-00006'" -f scripts/reset-job-workflow-by-number.sql
--
-- Restores inventory quantities for shop purchase lines, deletes all purchase lines,
-- sets status to On Going (0), clears completed/closed timestamps, and keeps only the
-- initial "Job taken at front desk." history row.

\set ON_ERROR_STOP on

BEGIN;

WITH target AS (
  SELECT "Id" AS job_id
  FROM "ServiceJobs"
  WHERE "JobNumber" = :job_number
)
UPDATE "InventoryItems" i
SET
  "QuantityOnHand" = i."QuantityOnHand" + pl."Quantity",
  "UpdatedAtUtc" = NOW()
FROM "JobPurchaseLines" pl, target t
WHERE pl."ServiceJobId" = t.job_id
  AND pl."InventoryItemId" = i."Id";

WITH target AS (
  SELECT "Id" AS job_id FROM "ServiceJobs" WHERE "JobNumber" = :job_number
)
DELETE FROM "JobPurchaseLines" pl
USING target t
WHERE pl."ServiceJobId" = t.job_id;

UPDATE "ServiceJobs"
SET
  "Status" = 0,
  "CompletedAtUtc" = NULL,
  "ClosedAtUtc" = NULL
WHERE "JobNumber" = :job_number;

WITH target AS (
  SELECT "Id" AS job_id FROM "ServiceJobs" WHERE "JobNumber" = :job_number
)
DELETE FROM "JobStatusHistories" h
USING target t
WHERE h."ServiceJobId" = t.job_id
  AND NOT (
    h."FromStatus" = 0
    AND h."ToStatus" = 0
    AND h."Note" = 'Job taken at front desk.'
  );

COMMIT;

SELECT
  s."JobNumber",
  s."Status",
  s."CompletedAtUtc",
  s."ClosedAtUtc",
  (SELECT COUNT(*) FROM "JobPurchaseLines" p WHERE p."ServiceJobId" = s."Id") AS purchase_lines,
  (SELECT COUNT(*) FROM "JobStatusHistories" h WHERE h."ServiceJobId" = s."Id") AS history_rows
FROM "ServiceJobs" s
WHERE s."JobNumber" = :job_number;
