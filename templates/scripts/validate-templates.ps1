param(
    [string]$LocalFeedPath = "$PSScriptRoot/../../artifacts/packages",
    [string]$TemplatePackagePath = "$PSScriptRoot/../../artifacts/packages",
    [string]$McaVersion = "0.1.21-preview",
    [string]$Framework = "net10.0",
    [switch]$RunDockerE2E = $false,
    [switch]$SkipAspire = $false,
    # Keep scaffolds under temp/validate for inspection (default: delete on success)
    [switch]$KeepOutput = $false,
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

function Write-IsolatedDirectoryBuildProps {
    param([string]$ProjectDir)

    # Generated apps under repo temp/ inherit repository Directory.Build.props
    # (TreatWarningsAsErrors, MinVer, CPM). Isolate so NuGet audit advisories and
    # packaging policy do not fail template validation builds.
    $buildProps = @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NuGetAudit>false</NuGetAudit>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
"@
    Set-Content -Path (Join-Path $ProjectDir "Directory.Build.props") -Value $buildProps -Encoding UTF8

    $packagesProps = @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
"@
    Set-Content -Path (Join-Path $ProjectDir "Directory.Packages.props") -Value $packagesProps -Encoding UTF8
}

function Assert-AspireScaffold {
    param(
        [string]$OutDir,
        [string]$ProjName,
        [bool]$SingleProject
    )

    $appHostDir = Join-Path $OutDir "$ProjName.AppHost"
    # Multi-project emits ServiceDefaults under src/; single-project emits it next to the web csproj.
    $serviceDefaultsDir = if ($SingleProject) {
        Join-Path $OutDir "$ProjName.ServiceDefaults"
    } else {
        Join-Path $OutDir "src" "$ProjName.ServiceDefaults"
    }
    $appHostCsproj = Join-Path $appHostDir "$ProjName.AppHost.csproj"
    $serviceDefaultsCsproj = Join-Path $serviceDefaultsDir "$ProjName.ServiceDefaults.csproj"

    if (-not (Test-Path -Path $appHostCsproj -PathType Leaf)) {
        throw "Aspire AppHost project missing: $appHostCsproj"
    }
    if (-not (Test-Path -Path $serviceDefaultsCsproj -PathType Leaf)) {
        throw "Aspire ServiceDefaults project missing: $serviceDefaultsCsproj"
    }

    $appHostProgram = Get-Content -Path (Join-Path $appHostDir "Program.cs") -Raw
    if ($appHostProgram -notmatch 'AddDatabase\("appdb"\)') {
        throw "AppHost must use stable connection name appdb (got unexpected Program.cs)"
    }
    if ($appHostProgram -match 'AddDatabase\("mca"\)') {
        throw "AppHost still uses connection name mca (must be appdb to avoid sourceName rewrite)"
    }

    if ($SingleProject) {
        $apiProgramPath = Join-Path $OutDir "Program.cs"
        if ($appHostProgram -notmatch [regex]::Escape("Projects.$ProjName")) {
            throw "Single-project AppHost should reference Projects.$ProjName"
        }
        if ($appHostProgram -match [regex]::Escape("Projects.${ProjName}_Api")) {
            throw "Single-project AppHost should not reference Projects.${ProjName}_Api"
        }
        $mainCsproj = Join-Path $OutDir "$ProjName.csproj"
        if (-not (Test-Path -Path $mainCsproj -PathType Leaf)) {
            throw "Single-project web csproj missing: $mainCsproj"
        }
        $mainCsprojText = Get-Content -Path $mainCsproj -Raw
        if ($mainCsprojText -notmatch 'Compile Remove=.*AppHost') {
            throw "Single-project csproj must exclude nested AppHost/ServiceDefaults compile items"
        }
    }
    else {
        $apiProgramPath = Join-Path $OutDir "src" "$ProjName.Api" "Program.cs"
        if ($appHostProgram -notmatch [regex]::Escape("Projects.${ProjName}_Api")) {
            throw "Multi-project AppHost should reference Projects.${ProjName}_Api"
        }
    }

    if (-not (Test-Path -Path $apiProgramPath -PathType Leaf)) {
        throw "API Program.cs missing: $apiProgramPath"
    }

    $apiProgram = Get-Content -Path $apiProgramPath -Raw
    if ($apiProgram -notmatch 'GetConnectionString\("appdb"\)') {
        throw "API must read ConnectionStrings:appdb under Aspire"
    }
    if ($apiProgram -notmatch 'AddServiceDefaults\(\)') {
        throw "API must call AddServiceDefaults() under Aspire"
    }
    if ($apiProgram -notmatch 'MapDefaultEndpoints\(\)') {
        throw "API must call MapDefaultEndpoints() under Aspire"
    }

    $composeFiles = @(Get-ChildItem -Path $OutDir -Filter "docker-compose.yml" -Recurse -ErrorAction SilentlyContinue)
    if ($composeFiles.Count -gt 0) {
        throw "docker-compose.yml must be omitted when --aspire is set"
    }

    # Aspire ships run-apphost helpers; docker-only deploy scripts must stay out
    $runAppHost = @(
        (Join-Path $OutDir "scripts/run-apphost.sh"),
        (Join-Path $OutDir "scripts/run-apphost.ps1")
    )
    foreach ($path in $runAppHost) {
        if (-not (Test-Path -Path $path -PathType Leaf)) {
            throw "Aspire scaffold missing helper script: $path"
        }
    }

    $dockerOnlyScripts = @(Get-ChildItem -Path (Join-Path $OutDir "scripts") -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^(compose-|deploy\.|kind-smoke\.|publish\.)' })
    if ($dockerOnlyScripts.Count -gt 0) {
        throw "Docker-only scripts should be omitted when --aspire is set: $($dockerOnlyScripts.Name -join ', ')"
    }

    Write-Host "==> Aspire scaffold checks passed for $ProjName"
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
        # USERPROFILE is Windows-only; HOME / UserProfile works on macOS/Linux
        $homeDir = if (-not [string]::IsNullOrWhiteSpace($env:HOME)) {
            $env:HOME
        } elseif (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
            $env:USERPROFILE
        } else {
            [Environment]::GetFolderPath("UserProfile")
        }
        if ([string]::IsNullOrWhiteSpace($homeDir)) {
            return
        }
        Join-Path $homeDir ".nuget/packages"
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
    # Prefer the real package; exclude symbol packages (*.snupkg and *.symbols.nupkg).
    $templatePackage = Get-ChildItem -Path $TemplatePackagePath -Filter "MinimalCleanArch.Templates*.nupkg" |
        Where-Object {
            $_.Name -notlike "*.snupkg" -and
            $_.Name -notlike "*.symbols.nupkg"
        } |
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
if ($SkipAspire) {
    Write-Host "==> Skipping Aspire scenarios (-SkipAspire)"
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
    @{ Name = "multi-default"; Args = @(); Aspire = $false; SingleProject = $false },
    @{ Name = "multi-recommended-sqlite"; Args = @("--recommended"); Aspire = $false; SingleProject = $false },
    @{ Name = "multi-auth-sqlite"; Args = @("--auth", "--tests"); Aspire = $false; SingleProject = $false },
    @{ Name = "multi-all-sqlserver"; Args = @("--all", "--db", "sqlserver", "--tests"); Aspire = $false; SingleProject = $false },
    @{ Name = "multi-all-postgres"; Args = @("--all", "--db", "postgres", "--tests"); Aspire = $false; SingleProject = $false },
    @{ Name = "single-default"; Args = @("--single-project"); Aspire = $false; SingleProject = $true },
    @{ Name = "single-recommended"; Args = @("--single-project", "--recommended"); Aspire = $false; SingleProject = $true },
    @{ Name = "single-auth-sqlite"; Args = @("--single-project", "--auth", "--tests"); Aspire = $false; SingleProject = $true },
    @{ Name = "single-all-sqlite"; Args = @("--single-project", "--all", "--tests"); Aspire = $false; SingleProject = $true },
    @{ Name = "multi-storage-sqlite"; Args = @("--storage", "--healthchecks"); Aspire = $false; SingleProject = $false }
)

if (-not $SkipAspire) {
    $scenarios += @(
        @{
            Name = "multi-recommended-aspire-postgres"
            Args = @("--recommended", "--aspire", "--db", "postgres")
            Aspire = $true
            SingleProject = $false
        },
        @{
            Name = "single-recommended-aspire-sqlserver"
            Args = @("--single-project", "--recommended", "--aspire", "--db", "sqlserver")
            Aspire = $true
            SingleProject = $true
        }
    )
}

if ($RunDockerE2E) {
    $env:RUN_DOCKER_E2E = "1"
}

$runStamp = Get-Date -Format "yyyyMMddHHmmssfff"
$workRoot = Join-Path $PSScriptRoot "../../temp/validate/$runStamp"
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
Write-Host "==> Validation output: $workRoot"
if ($KeepOutput) {
    Write-Host "==> KeepOutput enabled (scaffolds will not be deleted)"
}
$restoreConfigPath = Join-Path $workRoot "NuGet.Config"
New-RestoreConfig -ConfigPath $restoreConfigPath -FeedPath $localFeed -UseNugetOrg:$IncludeNugetOrg

# Isolate parent of scaffolds from repo Directory.Build.props (MSBuild walks up)
Write-IsolatedDirectoryBuildProps -ProjectDir $workRoot

function Remove-ValidationOutput {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -Path $Path)) {
        return
    }
    try {
        Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue |
            ForEach-Object {
                try { $_.Attributes = 'Normal' } catch { }
            }
        Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
        Write-Host "==> Cleaned validation output: $Path"
    }
    catch {
        Write-Host "WARNING: failed to clean validation output '$Path': $_" -ForegroundColor Yellow
    }
}

$buildProps = @(
    "-p:UseSharedCompilation=false",
    "-p:BuildInParallel=false",
    "-p:NuGetAudit=false",
    "-p:TreatWarningsAsErrors=false"
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
    $isAspire = [bool]$scenario.Aspire
    $isSingle = [bool]$scenario.SingleProject
    $projName = "App_" + ($name -replace '[^A-Za-z0-9]', '_')
    $outDir = Join-Path $workRoot $name

    try {
        Invoke-Checked -Description "Scaffolding $name" -Action {
            dotnet new mca -n $projName -o $outDir @scenarioArgs @templateArgs
        }

        Write-IsolatedDirectoryBuildProps -ProjectDir $outDir

        if ($isAspire) {
            Assert-AspireScaffold -OutDir $outDir -ProjName $projName -SingleProject:$isSingle
        }

        Push-Location $outDir
        try {
            $solution = Get-ChildItem -Path $outDir -Filter "*.slnx" -ErrorAction SilentlyContinue | Select-Object -First 1
            if (-not $solution) {
                $solution = Get-ChildItem -Path $outDir -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
            }

            # Prefer AppHost for Aspire (builds ServiceDefaults + API + host graph)
            $restoreTarget = $null
            if ($isAspire) {
                $appHostCsproj = Join-Path $outDir "$projName.AppHost" "$projName.AppHost.csproj"
                if (Test-Path -Path $appHostCsproj -PathType Leaf) {
                    $restoreTarget = $appHostCsproj
                }
            }
            if (-not $restoreTarget -and $solution) {
                $restoreTarget = $solution.FullName
            }
            if (-not $restoreTarget) {
                $entryProject = Get-ChildItem -Path $outDir -Filter "*.csproj" -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -notlike "*.AppHost.csproj" -and $_.Name -notlike "*.ServiceDefaults.csproj" } |
                    Select-Object -First 1
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
                    # Solution builds already compiled test projects
                    Invoke-Checked -Description "Test $name" -Action {
                        dotnet test $solution.FullName --no-build --no-restore --nologo
                    }
                } else {
                    # Single-project layout: tests are not referenced by the web csproj
                    foreach ($testProject in $testProjects) {
                        Invoke-Checked -Description "Restore tests $name ($($testProject.Name))" -Action {
                            dotnet restore $testProject.FullName --configfile $restoreConfigPath
                        }
                        Invoke-Checked -Description "Test $name ($($testProject.Name))" -Action {
                            dotnet test $testProject.FullName --nologo @buildProps
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
        # Do not Write-Error here: with ErrorActionPreference=Stop it aborts the loop
        # and skips remaining scenarios (including Aspire).
        Write-Host "ERROR: Scenario '$name' failed: $_" -ForegroundColor Red
    }
}

if ($failedScenarios.Count -gt 0) {
    Write-Host "Validation failed for scenario(s): $($failedScenarios -join ', ')" -ForegroundColor Red
    Write-Host "Output kept for debugging: $workRoot" -ForegroundColor Yellow
    exit 1
}

Write-Host "Validation complete. All scenarios passed." -ForegroundColor Green

if (-not $KeepOutput) {
    Remove-ValidationOutput -Path $workRoot
} else {
    Write-Host "Output retained (-KeepOutput): $workRoot"
}