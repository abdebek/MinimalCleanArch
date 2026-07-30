#!/usr/bin/env bash
# Starts the Aspire AppHost (orchestrates DB/cache containers + API).
# Requires Docker Desktop (or compatible runtime) for container resources.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

APPHOST_PROJECT=""
while IFS= read -r -d '' candidate; do
  APPHOST_PROJECT="$candidate"
  break
done < <(find "$ROOT_DIR" -maxdepth 2 -type f -name '*.AppHost.csproj' -print0 2>/dev/null)

if [[ -z "$APPHOST_PROJECT" ]]; then
  echo "No *.AppHost.csproj found under $ROOT_DIR. Scaffold with --aspire." >&2
  exit 1
fi

echo "Running Aspire AppHost: $APPHOST_PROJECT"
echo "Tip: dashboard URL is printed by the host after startup."
exec dotnet run --project "$APPHOST_PROJECT" -- "$@"
