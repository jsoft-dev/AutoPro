#!/usr/bin/env bash
# Wipes local Docker Postgres data and leaves an empty database ready for migrations + seed.
#
# Usage (from repo root):
#   ./scripts/reset-local-database.sh
#
# Then restart the API once (dev launcher or `dotnet run`). On startup it runs
# MigrateAsync + DataSeeder (demo users, job-type service prices).

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required (compose Postgres). See scripts/init-autopro-db.sql for bare-metal Postgres." >&2
  exit 1
fi

echo "Stopping Postgres and removing volume autopro_pg_data (all jobs, vehicles, inventory, invoices)…"
docker compose down -v

echo "Starting fresh Postgres container…"
docker compose up -d postgres

echo "Waiting for Postgres…"
for i in $(seq 1 30); do
  if docker compose exec -T postgres pg_isready -U autopro -d autopro >/dev/null 2>&1; then
    echo "Postgres is ready."
    break
  fi
  if [[ "$i" -eq 30 ]]; then
    echo "Postgres did not become ready in time." >&2
    exit 1
  fi
  sleep 1
done

cat <<'EOF'

Database volume is empty.

Next step: restart the AutoPro API so migrations and seed run automatically.

You will get:
  • Tables from latest EF migrations (including invoices + job-type prices)
  • Demo users: cr.demo and ao.demo — password ChangeMe123!
  • Default service charges in Business settings
  • 250 mock shop parts (MOCK-*), same as Render after migrate

Optional extra mock parts (if you need more than the migration inserts):
  dotnet run --project tools/SeedInventoryMock/SeedInventoryMock.csproj

EOF
