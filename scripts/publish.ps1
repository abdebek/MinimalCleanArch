#!/usr/bin/env pwsh

param(
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$PackagesPath = "./artifacts/packages",
    [switch]$WhatIf = $false,
    [switch]$SkipDuplicate = $true,
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing MinimalCleanArch packages..." -ForegroundColor Green
Write-Host "Source: $Source" -ForegroundColor Cyan
Write-Host "Packages Path: $PackagesPath" -ForegroundColor Cyan
Write-Host "What-If Mode: $WhatIf" -ForegroundColor Cyan

# Validate API key
if ([string]::IsNullOrEmpty($ApiKey)) {
    if ($WhatIf) {
        Write-Host "API key not set - continuing in What-If mode" -ForegroundColor Yellow
    } else {
        Write-Host "API key is required!" -ForegroundColor Red
        Write-Host "Solutions:" -ForegroundColor Yellow
        Write-Host "  1. Set NUGET_API_KEY environment variable" -ForegroundColor Gray
        Write-Host "  2. Pass -ApiKey parameter" -ForegroundColor Gray
        Write-Host "  3. Get API key from: https://www.nuget.org/account/apikeys" -ForegroundColor Gray
        exit 1
    }
}

# Validate packages directory
if (!(Test-Path $PackagesPath)) {
    Write-Error "Packages directory not found: $PackagesPath"
    exit 1
}

# Find packages (exclude symbol packages for main publishing)
$packages = Get-ChildItem "$PackagesPath/*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.nupkg" } | Sort-Object Name

if ($packages.Count -eq 0) {
    Write-Error "No packages found in $PackagesPath"
    Write-Host "Expected .nupkg files in the packages directory" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Found $($packages.Count) packages to publish:" -ForegroundColor Green
foreach ($package in $packages) {
    $size = [Math]::Round($package.Length / 1KB, 1)
    Write-Host "  Package: $($package.Name) ($size KB)" -ForegroundColor Cyan
}

if ($WhatIf) {
    Write-Host ""
    Write-Host "WHAT-IF MODE - No packages will be published" -ForegroundColor Yellow
    Write-Host "Remove -WhatIf to actually publish packages" -ForegroundColor Gray
}

Write-Host ""

$successCount = 0
$skippedCount = 0
$errorCount = 0

foreach ($package in $packages) {
    Write-Host "Publishing: $($package.Name)..." -ForegroundColor Yellow
    
    if ($WhatIf) {
        Write-Host "  WHAT-IF: Would publish $($package.FullName)" -ForegroundColor Cyan
        $successCount++
        continue
    }
    
    try {
        $pushArgs = @(
            "nuget", "push", $package.FullName,
            "--api-key", $ApiKey,
            "--source", $Source,
            "--timeout", $TimeoutSeconds
        )
        
        if ($SkipDuplicate) {
            $pushArgs += "--skip-duplicate"
        }
        
        & dotnet @pushArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Published successfully" -ForegroundColor Green
            $successCount++
        } elseif ($LASTEXITCODE -eq 409 -and $SkipDuplicate) {
            Write-Host "  Package already exists (skipped)" -ForegroundColor Yellow
            $skippedCount++
        } else {
            throw "dotnet nuget push failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Host "  Failed: $_" -ForegroundColor Red
        $errorCount++
        
        if (!$SkipDuplicate) {
            Write-Error "Failed to publish $($package.Name): $_"
            exit 1
        }
    }
    
    Write-Host ""
}

# Summary
Write-Host "Publishing Summary:" -ForegroundColor Green
Write-Host "  Successful: $successCount" -ForegroundColor Green
if ($skippedCount -gt 0) {
    Write-Host "  Skipped: $skippedCount" -ForegroundColor Yellow
}
if ($errorCount -gt 0) {
    Write-Host "  Errors: $errorCount" -ForegroundColor Red
}
Write-Host "  Total: $($packages.Count)" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host ""
    Write-Host "What-If mode completed - no packages were actually published" -ForegroundColor Yellow
} elseif ($errorCount -eq 0) {
    Write-Host ""
    Write-Host "All packages published successfully!" -ForegroundColor Green
    Write-Host "Visit https://www.nuget.org/profiles/[YourProfile] to view your packages" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "Some packages failed to publish. Check the output above." -ForegroundColor Yellow
    if (!$SkipDuplicate) {
        exit 1
    }
}
