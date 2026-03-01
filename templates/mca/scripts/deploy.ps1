param(
    [ValidateSet("compose", "kind")]
    [string]$Target = "compose",
    [switch]$NoBuild,
    [switch]$SkipSmokeTest,
    [int]$TimeoutSeconds = 180,
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ImageTag = "mca-api:local",
    [string]$Namespace = "mca",
    [string]$ClusterName = "mca-local",
    [int]$KindLocalPort = 5080
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

switch ($Target) {
    "compose" {
        & (Join-Path $scriptDir "compose-up.ps1") `
            -NoBuild:$NoBuild `
            -SkipSmokeTest:$SkipSmokeTest `
            -TimeoutSeconds $TimeoutSeconds `
            -BaseUrl $BaseUrl
    }
    "kind" {
        & (Join-Path $scriptDir "kind-smoke.ps1") `
            -ImageTag $ImageTag `
            -Namespace $Namespace `
            -ClusterName $ClusterName `
            -LocalPort $KindLocalPort `
            -SkipBuild:$NoBuild `
            -SkipSmokeTest:$SkipSmokeTest `
            -TimeoutSeconds $TimeoutSeconds
    }
}
