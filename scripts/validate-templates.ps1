#!/usr/bin/env pwsh
# Convenience wrapper: pack-local feed validation of `dotnet new mca` scaffolds.
# Forwards all arguments to templates/scripts/validate-templates.ps1
#
# Typical usage (from repo root):
#   ./scripts/pack.ps1 -PackageVersion 0.1.20-preview
#   ./scripts/validate-templates.ps1 -McaVersion 0.1.20-preview
#
# Aspire multi/single AppHost builds are included by default; pass -SkipAspire to omit.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $repoRoot "templates/scripts/validate-templates.ps1"

if (-not (Test-Path -Path $target -PathType Leaf)) {
    throw "Validation script not found: $target"
}

& $target @args
exit $LASTEXITCODE
