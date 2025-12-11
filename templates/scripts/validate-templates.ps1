param(
    [switch]$RunDockerE2E = $false
)

set-strictmode -version latest
$ErrorActionPreference = "Stop"

# Clean template cache and uninstall any installed package
dotnet new uninstall MinimalCleanArch.Templates | Out-Null
dotnet new --debug:reinit | Out-Null

# Install from source template folder
dotnet new install "$PSScriptRoot/../mca" --force | Out-Null

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

foreach ($scenario in $scenarios) {
    $name = $scenario.Name
    $scenarioArgs = $scenario.Args
    $projName = "App_" + ($name -replace '[^A-Za-z0-9]', '_')
    $outDir = Join-Path $PSScriptRoot "../temp/validate/$name"
    if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

    Write-Host "==> Scaffolding $name"
    dotnet new mca -n $projName -o $outDir @scenarioArgs

    Push-Location $outDir
    try {
        Write-Host "==> Restore $name"
        dotnet restore
        Write-Host "==> Build $name"
        dotnet build
        Write-Host "==> Test $name"
        dotnet test
    }
    finally {
        Pop-Location
    }
}

Write-Host "Validation complete."
