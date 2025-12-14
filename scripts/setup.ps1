#!/usr/bin/env pwsh

param(
    [string]$AuthorName = "Abdullah D.",
    [string]$CompanyName = "Waanfeetan LLC",
    [string]$GitHubUser = "abdebek",
    [string]$ProjectUrl = "",
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up MinimalCleanArch for NuGet publishing..." -ForegroundColor Green

# Validate inputs
if ($AuthorName -eq "Abdullah D.") {
    $AuthorName = Read-Host "Enter Abdullah D."
}
if ($CompanyName -eq "Waanfeetan LLC") {
    $CompanyName = Read-Host "Enter Waanfeetan LLC name"
}
if ($GitHubUser -eq "abdebek") {
    $GitHubUser = Read-Host "Enter your GitHub username"
}
if ([string]::IsNullOrEmpty($ProjectUrl)) {
    $ProjectUrl = "https://github.com/$GitHubUser/MinimalCleanArch"
}

Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Author: $AuthorName" -ForegroundColor Gray
Write-Host "  Company: $CompanyName" -ForegroundColor Gray
Write-Host "  GitHub User: $GitHubUser" -ForegroundColor Gray
Write-Host "  Project URL: $ProjectUrl" -ForegroundColor Gray

if ($WhatIf) {
    Write-Host "What-If mode - no files will be modified" -ForegroundColor Yellow
}

# Create directory structure
$directories = @(
    "scripts",
    ".github/workflows",
    "artifacts/packages"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        if ($WhatIf) {
            Write-Host "Would create directory: $dir" -ForegroundColor Cyan
        } else {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "Created directory: $dir" -ForegroundColor Green
        }
    }
}

# Create/update Directory.Build.props
$directoryBuildProps = @"
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsAsErrors />
    
    <!-- Package Information -->
    <PackageVersion>0.0.1</PackageVersion>
    <Authors>$AuthorName</Authors>
    <Company>$CompanyName</Company>
    <Product>MinimalCleanArch</Product>
    <Copyright>Copyright © $CompanyName 2025</Copyright>
    <PackageProjectUrl>$ProjectUrl</PackageProjectUrl>
    <RepositoryUrl>$ProjectUrl</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
    
    <!-- Build Configuration -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <!-- Include README and icon for all packages -->
  <ItemGroup>
    <None Include="`$(MSBuildThisFileDirectory)../README.md" Pack="true" PackagePath="/" />
    <None Include="`$(MSBuildThisFileDirectory)icon.png" Pack="true" PackagePath="/" Condition="Exists('`$(MSBuildThisFileDirectory)icon.png')" />
  </ItemGroup>
</Project>
"@

if ($WhatIf) {
    Write-Host "Would create/update: src/Directory.Build.props" -ForegroundColor Cyan
} else {
    Set-Content -Path "src/Directory.Build.props" -Value $directoryBuildProps -Encoding UTF8
    Write-Host "Created: src/Directory.Build.props" -ForegroundColor Green
}

# Make scripts executable (if on Unix-like system)
$scriptFiles = @(
    "scripts/pack.sh",
    "scripts/publish.sh"
)

foreach ($script in $scriptFiles) {
    if (Test-Path $script) {
        if ($WhatIf) {
            Write-Host "Would make executable: $script" -ForegroundColor Cyan
        } else {
            if ($IsLinux -or $IsMacOS) {
                chmod +x $script
            }
            Write-Host "Made executable: $script" -ForegroundColor Green
        }
    }
}

# Instructions
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Green
Write-Host "1. Create a package icon (128x128 PNG) at: src/icon.png" -ForegroundColor Yellow
Write-Host "2. Update README.md with package documentation" -ForegroundColor Yellow
Write-Host "3. Get NuGet API key from: https://www.nuget.org/account/apikeys" -ForegroundColor Yellow
Write-Host "4. Set environment variable: " -ForegroundColor Yellow -NoNewline
Write-Host "`$env:NUGET_API_KEY='your-api-key'" -ForegroundColor Cyan

Write-Host ""
Write-Host "Build and Test:" -ForegroundColor Green
Write-Host "  ./scripts/pack.ps1" -ForegroundColor Cyan
Write-Host "  ./scripts/pack.sh" -ForegroundColor Cyan

Write-Host ""
Write-Host "Publish:" -ForegroundColor Green
Write-Host "  ./scripts/publish.ps1 -WhatIf" -ForegroundColor Cyan
Write-Host "  ./scripts/publish.ps1" -ForegroundColor Cyan

Write-Host ""
Write-Host "Git Tags:" -ForegroundColor Green
Write-Host "  git tag v0.0.1" -ForegroundColor Cyan
Write-Host "  git push origin v0.0.1" -ForegroundColor Cyan

Write-Host ""
Write-Host "Setup completed!" -ForegroundColor Green