#!/usr/bin/env bash
# Wipes the Neon database used by the Render-hosted API and re-seeds demo data.
#
# Usage (from repo root):
#   ConnectionStrings__DefaultConnection='Host=...; Database=neondb; ...' \
#   RESET_HOSTED_DATABASE_CONFIRM=yes \
#   ./scripts/reset-hosted-database.sh
#
# Copy ConnectionStrings__DefaultConnection from Render → autopro-api → Environment
# (Neon connection string; append SSL if needed). Pooler URLs are OK — the tool
# switches to a direct host for DROP SCHEMA and migrations.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [[ "${RESET_HOSTED_DATABASE_CONFIRM:-}" != "yes" ]]; then
  echo "Set RESET_HOSTED_DATABASE_CONFIRM=yes to confirm wipe of ALL hosted data." >&2
  exit 1
fi

if [[ -z "${ConnectionStrings__DefaultConnection:-}" ]]; then
  echo "Set ConnectionStrings__DefaultConnection to your Neon URL." >&2
  exit 1
fi

dotnet run --project tools/ResetHostedDatabase/ResetHostedDatabase.csproj
