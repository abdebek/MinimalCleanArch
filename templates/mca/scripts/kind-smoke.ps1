param(
    [string]$ImageTag = "mca-api:local",
    [string]$Namespace = "mca",
    [string]$ClusterName = "mca-local",
    [string]$DeploymentName = "mca-api",
    [int]$LocalPort = 5080,
    [switch]$SkipBuild,
    [switch]$SkipSmokeTest,
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..")

function Assert-CommandExists {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not available in PATH."
    }
}

Assert-CommandExists -Name "docker"
Assert-CommandExists -Name "kind"
Assert-CommandExists -Name "kubectl"

if (-not $SkipBuild) {
    & docker build -t $ImageTag $projectRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed with exit code $LASTEXITCODE."
    }
}

$clusters = (& kind get clusters)
if ($LASTEXITCODE -ne 0) {
    throw "Unable to list kind clusters."
}

if ($clusters -notcontains $ClusterName) {
    & kind create cluster --name $ClusterName
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create kind cluster '$ClusterName'."
    }
}

& kind load docker-image $ImageTag --name $ClusterName
if ($LASTEXITCODE -ne 0) {
    throw "Failed to load image '$ImageTag' into kind cluster '$ClusterName'."
}

@"
apiVersion: v1
kind: Namespace
metadata:
  name: $Namespace
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: $DeploymentName
  namespace: $Namespace
spec:
  replicas: 1
  selector:
    matchLabels:
      app: $DeploymentName
  template:
    metadata:
      labels:
        app: $DeploymentName
    spec:
      containers:
      - name: api
        image: $ImageTag
        imagePullPolicy: IfNotPresent
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Development
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: $DeploymentName
  namespace: $Namespace
spec:
  selector:
    app: $DeploymentName
  ports:
  - name: http
    port: 80
    targetPort: 8080
"@ | kubectl apply -f -

if ($LASTEXITCODE -ne 0) {
    throw "Failed to apply Kubernetes resources."
}

& kubectl -n $Namespace rollout status "deployment/$DeploymentName" --timeout="$($TimeoutSeconds)s"
if ($LASTEXITCODE -ne 0) {
    throw "Deployment rollout did not complete successfully."
}

if ($SkipSmokeTest) {
    Write-Host "Kubernetes deployment completed (smoke test skipped)." -ForegroundColor Green
    return
}

$portForward = $null
try {
    $portForward = Start-Process -FilePath "kubectl" `
        -ArgumentList @("-n", $Namespace, "port-forward", "service/$DeploymentName", "$LocalPort`:80") `
        -PassThru `
        -WindowStyle Hidden

    Start-Sleep -Seconds 3

    & (Join-Path $scriptDir "smoke-test.ps1") `
        -BaseUrl "http://localhost:$LocalPort" `
        -Path "/api/todos" `
        -TimeoutSeconds $TimeoutSeconds
}
finally {
    if ($null -ne $portForward -and -not $portForward.HasExited) {
        Stop-Process -Id $portForward.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Kind smoke deployment completed." -ForegroundColor Green
