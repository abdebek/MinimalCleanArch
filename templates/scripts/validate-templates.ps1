param(
    [string]$LocalFeedPath = "$PSScriptRoot/../../artifacts/packages",
    [string]$TemplatePackagePath = "$PSScriptRoot/../../artifacts/packages",
    [string]$McaVersion = "0.1.18-preview",
    [string]$Framework = "net10.0",
    [switch]$RunDockerE2E = $false,
    [bool]$IncludeNugetOrg = $true
)

set-strictmode -version latest
$ErrorActionPreference = "Stop"

function Invoke-Checked {
    param(
        [string]$Description,
        [scriptblock]$Action,
        [switch]$AllowFailure = $false
    )

    Write-Host "==> $Description"
    & $Action
    $exitCode = $LASTEXITCODE

    if (($exitCode -ne 0) -and -not $AllowFailure) {
        throw "$Description failed with exit code $exitCode"
    }
}

function New-RestoreConfig {
    param(
        [string]$ConfigPath,
        [string]$FeedPath,
        [switch]$UseNugetOrg = $false
    )

    $sources = @(
        "    <add key=`"LocalFeed`" value=`"$FeedPath`" />"
    )
    if ($UseNugetOrg) {
        $sources += "    <add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`" protocolVersion=`"3`" />"
    }

    $configContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
$($sources -join "`n")
  </packageSources>
</configuration>
"@

    Set-Content -Path $ConfigPath -Value $configContent -Encoding UTF8
}

function Clear-LocalMinimalCleanArchPackageCache {
    param(
        [string]$Version
    )

    if ([string]::IsNullOrWhiteSpace($Version)) {
        return
    }

    $globalPackages = if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
        $env:NUGET_PACKAGES
    } else {
        Join-Path $env:USERPROFILE ".nuget/packages"
    }

    if (-not (Test-Path -Path $globalPackages -PathType Container)) {
        return
    }

    Get-ChildItem -Path $globalPackages -Directory -Filter "minimalcleanarch*" -ErrorAction SilentlyContinue |
        ForEach-Object {
            $versionPath = Join-Path $_.FullName $Version
            if (Test-Path -Path $versionPath -PathType Container) {
                Remove-Item -Path $versionPath -Recurse -Force
            }
        }
}

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

Clear-LocalMinimalCleanArchPackageCache -Version $McaVersion

# Clean template cache and uninstall any installed package
Invoke-Checked -Description "Uninstalling previous template package (if installed)" -AllowFailure -Action {
    dotnet new uninstall MinimalCleanArch.Templates | Out-Null
}
Invoke-Checked -Description "Resetting template cache" -Action {
    dotnet new --debug:reinit | Out-Null
}

# Install from template package
Invoke-Checked -Description "Installing template package" -Action {
    dotnet new install "$templatePackage" --force | Out-Null
}

# Scaffolds to validate
$scenarios = @(
    @{ Name = "multi-default"; Args = @() },
    @{ Name = "multi-recommended-sqlite"; Args = @("--recommended") },
    @{ Name = "multi-auth-sqlite"; Args = @("--auth", "--tests") },
    @{ Name = "multi-all-sqlserver"; Args = @("--all", "--db", "sqlserver", "--tests") },
    @{ Name = "multi-all-postgres"; Args = @("--all", "--db", "postgres", "--tests") },
    @{ Name = "single-default"; Args = @("--single-project") },
    @{ Name = "single-recommended"; Args = @("--single-project", "--recommended") },
    @{ Name = "single-auth-sqlite"; Args = @("--single-project", "--auth", "--tests") },
    @{ Name = "single-all-sqlite"; Args = @("--single-project", "--all", "--tests") }
)

if ($RunDockerE2E) {
    $env:RUN_DOCKER_E2E = "1"
}

$runStamp = Get-Date -Format "yyyyMMddHHmmssfff"
$workRoot = Join-Path $PSScriptRoot "../../temp/validate/$runStamp"
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
Write-Host "==> Validation output: $workRoot"
$restoreConfigPath = Join-Path $workRoot "NuGet.Config"
New-RestoreConfig -ConfigPath $restoreConfigPath -FeedPath $localFeed -UseNugetOrg:$IncludeNugetOrg

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

$failedScenarios = @()

foreach ($scenario in $scenarios) {
    $name = $scenario.Name
    $scenarioArgs = $scenario.Args
    $projName = "App_" + ($name -replace '[^A-Za-z0-9]', '_')
    $outDir = Join-Path $workRoot $name

    try {
        Invoke-Checked -Description "Scaffolding $name" -Action {
            dotnet new mca -n $projName -o $outDir @scenarioArgs @templateArgs
        }

        Push-Location $outDir
        try {
            $solution = Get-ChildItem -Path $outDir -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
            $restoreTarget = $null
            if ($solution) {
                $restoreTarget = $solution.FullName
            }
            if (-not $restoreTarget) {
                $entryProject = Get-ChildItem -Path $outDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
                if (-not $entryProject) {
                    throw "No solution or project file found in scaffold output: $outDir"
                }
                $restoreTarget = $entryProject.FullName
            }

            Invoke-Checked -Description "Restore $name" -Action {
                dotnet restore $restoreTarget --configfile $restoreConfigPath
            }

            Invoke-Checked -Description "Build $name" -Action {
                dotnet build $restoreTarget --no-restore @buildProps
            }

            $testProjects = @(Get-ChildItem -Path (Join-Path $outDir "tests") -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue)
            if ($testProjects.Count -gt 0) {
                if ($solution) {
                    Invoke-Checked -Description "Test $name" -Action {
                        dotnet test $solution.FullName --no-build --no-restore
                    }
                } else {
                    foreach ($testProject in $testProjects) {
                        Invoke-Checked -Description "Test $name ($($testProject.Name))" -Action {
                            dotnet test $testProject.FullName --no-build --no-restore
                        }
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
    catch {
        $failedScenarios += $name
        Write-Error "Scenario '$name' failed: $_"
    }
}

if ($failedScenarios.Count -gt 0) {
    throw "Validation failed for scenario(s): $($failedScenarios -join ', ')"
}

Write-Host "Validation complete. All scenarios passed."

