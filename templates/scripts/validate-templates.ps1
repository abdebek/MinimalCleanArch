param(
    [string]$LocalFeedPath = "$PSScriptRoot/../../artifacts/packages",
    [string]$TemplatePackagePath = "$PSScriptRoot/../../artifacts/packages",
    [string]$McaVersion = "0.1.9-preview",
    [string]$Framework = "net10.0",
    [switch]$RunDockerE2E = $false,
    [switch]$IncludeNugetOrg = $false
)

set-strictmode -version latest
$ErrorActionPreference = "Stop"

$resolvedFeed = Resolve-Path -Path $LocalFeedPath -ErrorAction SilentlyContinue
if (-not $resolvedFeed) {
    throw "Local feed path not found: $LocalFeedPath"
}
$localFeed = $resolvedFeed.Path

$templatePackage = $null
if (Test-Path -Path $TemplatePackagePath -PathType Leaf) {
    $templatePackage = (Resolve-Path -Path $TemplatePackagePath).Path
} elseif (Test-Path -Path $TemplatePackagePath -PathType Container) {
    $templatePackage = Get-ChildItem -Path $TemplatePackagePath -Filter "MinimalCleanArch.Templates*.nupkg" |
        Where-Object { $_.Name -notlike "*.snupkg" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($templatePackage) {
        $templatePackage = $templatePackage.FullName
    }
}
if (-not $templatePackage) {
    throw "Template package not found in: $TemplatePackagePath"
}

Write-Host "==> Local feed: $localFeed"
Write-Host "==> Template package: $templatePackage"
if (-not [string]::IsNullOrWhiteSpace($McaVersion)) {
    Write-Host "==> Template MCA version: $McaVersion"
}
if (-not [string]::IsNullOrWhiteSpace($Framework)) {
    Write-Host "==> Template target framework: $Framework"
}
if ($IncludeNugetOrg) {
    Write-Host "==> Using nuget.org in restore sources"
}

# Clean template cache and uninstall any installed package
dotnet new uninstall MinimalCleanArch.Templates | Out-Null
dotnet new --debug:reinit | Out-Null

# Install from template package
dotnet new install "$templatePackage" --force | Out-Null

# Scaffolds to validate
$scenarios = @(
    @{ Name = "multi-default"; Args = @() },
    @{ Name = "multi-recommended-sqlite"; Args = @("--recommended") },
    @{ Name = "multi-all-sqlserver"; Args = @("--all", "--db", "sqlserver", "--tests") },
    @{ Name = "multi-all-postgres"; Args = @("--all", "--db", "postgres", "--tests") },
    @{ Name = "single-default"; Args = @("--single-project") },
    @{ Name = "single-recommended"; Args = @("--single-project", "--recommended") },
    @{ Name = "single-all-sqlite"; Args = @("--single-project", "--all", "--tests") }
)

if ($RunDockerE2E) {
    $env:RUN_DOCKER_E2E = "1"
}

$runStamp = Get-Date -Format "yyyyMMddHHmmssfff"
$workRoot = Join-Path $PSScriptRoot "../../temp/validate/$runStamp"
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
Write-Host "==> Validation output: $workRoot"

$buildProps = @(
    "-p:UseSharedCompilation=false",
    "-p:BuildInParallel=false"
)

$templateArgs = @()
if (-not [string]::IsNullOrWhiteSpace($McaVersion)) {
    $templateArgs = @("--mcaVersion", $McaVersion)
}
if (-not [string]::IsNullOrWhiteSpace($Framework)) {
    $templateArgs += @("--framework", $Framework)
}

foreach ($scenario in $scenarios) {
    $name = $scenario.Name
    $scenarioArgs = $scenario.Args
    $projName = "App_" + ($name -replace '[^A-Za-z0-9]', '_')
    $outDir = Join-Path $workRoot $name

    Write-Host "==> Scaffolding $name"
    dotnet new mca -n $projName -o $outDir @scenarioArgs @templateArgs

    Push-Location $outDir
    try {
        Write-Host "==> Restore $name"
        $restoreSources = @("--source", $localFeed)
        if ($IncludeNugetOrg) {
            $restoreSources += @("--source", "https://api.nuget.org/v3/index.json")
        }
        dotnet restore @restoreSources
        Write-Host "==> Build $name"
        dotnet build --no-restore @buildProps
        $testProjects = @(Get-ChildItem -Path (Join-Path $outDir "tests") -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue)
        if ($testProjects.Count -gt 0) {
            Write-Host "==> Test $name"
            $solution = Get-ChildItem -Path $outDir -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($solution) {
                dotnet test $solution.FullName --no-build --no-restore
            } else {
                foreach ($testProject in $testProjects) {
                    dotnet test $testProject.FullName --no-build --no-restore
                }
            }
        } else {
            Write-Host "==> Test $name (skipped - no test projects)"
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "Validation complete."
