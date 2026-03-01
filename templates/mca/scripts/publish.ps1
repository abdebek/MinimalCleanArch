param(
    [string]$ImageName = "mca-api",
    [string]$Tag = "local",
    [string]$Registry = "",
    [string]$Dockerfile = "Dockerfile",
    [switch]$Push,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..")
$dockerfilePath = Join-Path $projectRoot $Dockerfile

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed or not available in PATH."
}

if (-not (Test-Path $dockerfilePath)) {
    throw "Dockerfile not found: $dockerfilePath"
}

$fullImageName = if ([string]::IsNullOrWhiteSpace($Registry)) {
    "${ImageName}:$Tag"
}
else {
    "$($Registry.TrimEnd('/'))/${ImageName}:$Tag"
}

if (-not $NoBuild) {
    & docker build -f $dockerfilePath -t $fullImageName $projectRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed with exit code $LASTEXITCODE."
    }
}

if ($Push) {
    & docker push $fullImageName
    if ($LASTEXITCODE -ne 0) {
        throw "Docker push failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Image ready: $fullImageName" -ForegroundColor Green
