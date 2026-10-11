#!/usr/bin/env bash
# Convenience wrapper: pack-local feed validation of `dotnet new mca` scaffolds.
# Requires PowerShell 7+ (`pwsh`) because the validator is implemented in PowerShell.
#
# Typical usage (from repo root):
#   ./scripts/pack.sh --package-version 0.1.20-preview
#   ./scripts/validate-templates.sh -McaVersion 0.1.20-preview
#
# Aspire multi/single AppHost builds are included by default; pass -SkipAspire to omit.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TARGET="$REPO_ROOT/templates/scripts/validate-templates.ps1"

if [[ ! -f "$TARGET" ]]; then
  echo "Validation script not found: $TARGET" >&2
  exit 1
fi

if ! command -v pwsh >/dev/null 2>&1; then
  echo "pwsh (PowerShell 7+) is required to run template validation." >&2
  echo "Install: https://learn.microsoft.com/powershell/scripting/install/installing-powershell" >&2
  exit 1
fi

exec pwsh -NoProfile -File "$TARGET" "$@"
