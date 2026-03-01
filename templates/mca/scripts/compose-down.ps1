param(
    [switch]$RemoveVolumes,
    [string]$ComposeFile = "docker-compose.yml"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..")
$composePath = Join-Path $projectRoot $ComposeFile

if (-not (Test-Path $composePath)) {
    throw "Compose file not found: $composePath"
}

$defaultProjectName = (Split-Path $projectRoot -Leaf).ToLowerInvariant()
$defaultProjectName = $defaultProjectName -replace "[^a-z0-9_-]", "-"
$defaultProjectName = $defaultProjectName.Trim("-")
if ([string]::IsNullOrWhiteSpace($defaultProjectName)) {
    $defaultProjectName = "mca"
}
if ($defaultProjectName -notmatch "^[a-z0-9]") {
    $defaultProjectName = "mca-$defaultProjectName"
}
if ([string]::IsNullOrWhiteSpace($env:COMPOSE_PROJECT_NAME)) {
    $env:COMPOSE_PROJECT_NAME = $defaultProjectName
}

$hasComposePlugin = [bool](Get-Command docker -ErrorAction SilentlyContinue)
$hasComposeBinary = [bool](Get-Command docker-compose -ErrorAction SilentlyContinue)

if (-not $hasComposePlugin -and -not $hasComposeBinary) {
    throw "Docker Compose not found. Install Docker Desktop or docker-compose."
}

function Invoke-Compose {
    param([string[]]$Arguments)

    if ($hasComposePlugin) {
        & docker compose @Arguments
    }
    else {
        & docker-compose @Arguments
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose command failed with exit code $LASTEXITCODE."
    }
}

$composeArgs = @("-f", $composePath, "down", "--remove-orphans")
if ($RemoveVolumes) {
    $composeArgs += "-v"
}

Invoke-Compose -Arguments $composeArgs
Write-Host "Compose services stopped." -ForegroundColor Green
