#!/usr/bin/env pwsh
# Removes scaffold/build debris under repo temp/ (template tests + validate-templates).
param(
    [switch]$WhatIf = $false,
    [switch]$All = $false  # also remove anything else under temp/
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$tempRoot = Join-Path $repoRoot "temp"

if (-not (Test-Path -Path $tempRoot -PathType Container)) {
    Write-Host "Nothing to clean: $tempRoot does not exist."
    exit 0
}

$targets = @(
    (Join-Path $tempRoot "MCA_Tests"),
    (Join-Path $tempRoot "validate")
)

if ($All) {
    $targets = @($tempRoot)
}

function Get-DirSizeMb([string]$Path) {
    if (-not (Test-Path $Path)) { return 0 }
    $bytes = (Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum).Sum
    if (-not $bytes) { return 0 }
    return [math]::Round($bytes / 1MB, 1)
}

Write-Host "Cleaning template/validation temp output under: $tempRoot"
foreach ($target in $targets) {
    if (-not (Test-Path -Path $target)) {
        Write-Host "  skip (missing): $target"
        continue
    }

    $sizeMb = Get-DirSizeMb $target
    if ($WhatIf) {
        Write-Host "  WHAT-IF: would remove $target (~${sizeMb} MB)"
        continue
    }

    Write-Host "  removing $target (~${sizeMb} MB)..."
    Get-ChildItem -Path $target -Recurse -Force -ErrorAction SilentlyContinue |
        ForEach-Object { try { $_.Attributes = 'Normal' } catch { } }
    Remove-Item -Path $target -Recurse -Force
    Write-Host "  removed."
}

Write-Host "Done."
