#!/usr/bin/env pwsh
# Starts the Aspire AppHost (orchestrates DB/cache containers + API).
# Requires Docker Desktop (or compatible runtime) for container resources.
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"
$rootDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

$appHostProject = Get-ChildItem -Path $rootDir -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*.AppHost" } |
    ForEach-Object {
        Get-ChildItem -Path $_.FullName -Filter "*.AppHost.csproj" -File -ErrorAction SilentlyContinue
    } |
    Select-Object -First 1

if (-not $appHostProject) {
    # Single-project layout keeps AppHost as a sibling folder under the app root
    $appHostProject = Get-ChildItem -Path $rootDir -Filter "*.AppHost.csproj" -File -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

if (-not $appHostProject) {
    throw "No *.AppHost.csproj found under $rootDir. Scaffold with --aspire."
}

Write-Host "Running Aspire AppHost: $($appHostProject.FullName)" -ForegroundColor Cyan
Write-Host "Tip: dashboard URL is printed by the host after startup." -ForegroundColor Gray

$dotnetArgs = @("run", "--project", $appHostProject.FullName)
if ($RemainingArgs -and $RemainingArgs.Count -gt 0) {
    $dotnetArgs += "--"
    $dotnetArgs += $RemainingArgs
}

& dotnet @dotnetArgs
exit $LASTEXITCODE
