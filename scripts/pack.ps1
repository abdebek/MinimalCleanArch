#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./artifacts/packages",
    [switch]$SkipBuild = $false,
    [switch]$IncludeSymbols = $true
)

$ErrorActionPreference = "Stop"

Write-Host "Building and packing MinimalCleanArch packages..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan
Write-Host "Skip Build: $SkipBuild" -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Yellow
}

# Clean previous artifacts
Write-Host "Cleaning previous artifacts..." -ForegroundColor Yellow
Remove-Item "$OutputPath/*.nupkg" -Force -ErrorAction SilentlyContinue
Remove-Item "$OutputPath/*.snupkg" -Force -ErrorAction SilentlyContinue

# Projects to pack (in dependency order - core first, then dependent packages)
$projects = @(
    "src/MinimalCleanArch/MinimalCleanArch.csproj",
    "src/MinimalCleanArch.Audit/MinimalCleanArch.Audit.csproj",
    "src/MinimalCleanArch.Messaging/MinimalCleanArch.Messaging.csproj",
    "src/MinimalCleanArch.DataAccess/MinimalCleanArch.DataAccess.csproj",
    "src/MinimalCleanArch.Extensions/MinimalCleanArch.Extensions.csproj",
    "src/MinimalCleanArch.Validation/MinimalCleanArch.Validation.csproj",
    "src/MinimalCleanArch.Validation/MinimalCleanArch.Validation.csproj",
    "src/MinimalCleanArch.Security/MinimalCleanArch.Security.csproj"
)

$successCount = 0
$totalProjects = $projects.Count

foreach ($project in $projects) {
    if (!(Test-Path $project)) {
        Write-Warning "Project file not found: $project - Skipping"
        continue
    }
    
    Write-Host "Processing ($($successCount + 1)/$totalProjects): $project..." -ForegroundColor Yellow
    
    try {
        if (!$SkipBuild) {
            Write-Host "  Building..." -ForegroundColor Gray
            dotnet build $project --configuration $Configuration --verbosity minimal --nologo
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for $project"
            }
        }
        
        Write-Host "  Packing..." -ForegroundColor Gray
        $packArgs = @(
            "pack", $project,
            "--configuration", $Configuration,
            "--output", $OutputPath,
            "--verbosity", "minimal",
            "--nologo",
            "-p:GenerateDocumentationFile=false"
        )
        
        if ($SkipBuild) {
            $packArgs += "--no-build"
        }
        
        if ($IncludeSymbols) {
            $packArgs += "--include-symbols"
        }
        
        & dotnet @packArgs
        
        if ($LASTEXITCODE -ne 0) {
            throw "Pack failed for $project"
        }
        
        $successCount++
        Write-Host "  Success" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to process ${project}: $_"
        exit 1
    }
}

Write-Host ""
Write-Host "Packaging completed successfully!" -ForegroundColor Green
Write-Host "Processed $successCount/$totalProjects projects" -ForegroundColor Cyan

# List created packages
Write-Host ""
Write-Host "Created packages:" -ForegroundColor Green
$packages = Get-ChildItem $OutputPath -Filter "*.nupkg" | Sort-Object Name
if ($packages.Count -eq 0) {
    Write-Warning "No packages were created!"
} else {
    foreach ($package in $packages) {
        $size = [Math]::Round($package.Length / 1KB, 1)
        Write-Host "  Package: $($package.Name) ($size KB)" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Total packages: $($packages.Count)" -ForegroundColor Green
    
    # Show symbols packages if they exist
    $symbolPackages = Get-ChildItem $OutputPath -Filter "*.snupkg" | Sort-Object Name
    if ($symbolPackages.Count -gt 0) {
        Write-Host ""
        Write-Host "Symbol packages:" -ForegroundColor Green
        foreach ($symbolPackage in $symbolPackages) {
            $size = [Math]::Round($symbolPackage.Length / 1KB, 1)
            Write-Host "  Symbol: $($symbolPackage.Name) ($size KB)" -ForegroundColor Cyan
        }
    }
}

Write-Host ""
Write-Host "All packages ready for publishing!" -ForegroundColor Green