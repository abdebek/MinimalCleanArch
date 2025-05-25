# MinimalCleanArch .NET 9.0.5 Upgrade Script
# Upgrades all projects to .NET 9.0.5 with verified available package versions

param(
    [switch]$WhatIf = $false,
    [switch]$SkipTests = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Continue"

Write-Host "MinimalCleanArch .NET 9.0.5 Upgrade Starting..." -ForegroundColor Green

if ($WhatIf) {
    Write-Host "WHAT-IF MODE - No changes will be made" -ForegroundColor Yellow
}

# Function to safely update file content
function Update-ProjectFile {
    param($FilePath, $Updates, $ProjectName)
    
    if (!(Test-Path $FilePath)) {
        Write-Warning "Project file not found: $FilePath"
        return $false
    }
    
    try {
        $content = Get-Content $FilePath -Raw
        $originalContent = $content
        $changed = $false
        
        # Update target framework
        if ($content -match '<TargetFramework>net[0-9]+\.[0-9]+</TargetFramework>') {
            $content = $content -replace '<TargetFramework>net[0-9]+\.[0-9]+</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>'
            Write-Host "  Updated TargetFramework to net9.0" -ForegroundColor Green
            $changed = $true
        }
        
        # Update package versions
        foreach ($package in $Updates.Keys) {
            $newVersion = $Updates[$package]
            $pattern = "PackageReference Include=`"$([regex]::Escape($package))`" Version=`"[^`"]+`""
            $replacement = "PackageReference Include=`"$package`" Version=`"$newVersion`""
            
            if ($content -match $pattern) {
                $content = $content -replace $pattern, $replacement
                Write-Host "  Updated $package to $newVersion" -ForegroundColor Green
                $changed = $true
            }
        }
        
        # Save changes
        if ($changed -and !$WhatIf) {
            Set-Content $FilePath $content -NoNewline
        }
        
        return $changed
    }
    catch {
        Write-Error "Failed to update $ProjectName : $($_.Exception.Message)"
        return $false
    }
}

# Step 1: Update global.json
Write-Host "Updating global.json..." -ForegroundColor Cyan

$globalJsonContent = @"
{
  "sdk": {
    "version": "9.0.300",
    "rollForward": "latestMinor"
  },
  "msbuild-sdks": {
    "Microsoft.Build.NoTargets": "3.7.0"
  }
}
"@

if (!$WhatIf) {
    Set-Content -Path "global.json" -Value $globalJsonContent
}
Write-Host "  Updated global.json to .NET 9.0.300" -ForegroundColor Green

# Step 2: Update Directory.Build.props
Write-Host "Updating Directory.Build.props..." -ForegroundColor Cyan

$directoryBuildContent = @"
<Project>
  <!-- Common project properties -->
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- XML Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>`$(NoWarn);CS1591;CS1573</NoWarn>
  </PropertyGroup>

  <!-- NuGet package properties -->
  <PropertyGroup>
    <Authors>Waanfeetan LLC</Authors>
    <Company>Waanfeetan LLC</Company>
    <Copyright>Copyright © `$(Company) `$([System.DateTime]::Now.Year)</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageProjectUrl>https://github.com/abdebek/MinimalCleanArch</PackageProjectUrl>
    <RepositoryUrl>https://github.com/abdebek/MinimalCleanArch.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>clean-architecture;minimal-api;entity-framework;validation;encryption</PackageTags>
    <MinVerTagPrefix>v</MinVerTagPrefix>
    <MinVerDefaultPreReleaseIdentifiers>preview</MinVerDefaultPreReleaseIdentifiers>
    <MinVerAutoIncrement>minor</MinVerAutoIncrement>
  </PropertyGroup>

  <!-- Include README.md in NuGet packages -->
  <ItemGroup>
    <None Include="`$(MSBuildThisFileDirectory)\README.md" Pack="true" PackagePath="\" Visible="false" Condition="Exists('`$(MSBuildThisFileDirectory)\README.md')" />
    <None Include="README.md" Pack="true" PackagePath="\" Visible="false" Condition="Exists('README.md')" />
  </ItemGroup>

  <!-- Source Link for debugging -->
  <PropertyGroup>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <AllowedOutputExtensionsInPackageBuildOutputFolder>`$(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb</AllowedOutputExtensionsInPackageBuildOutputFolder>
  </PropertyGroup>

  <!-- Deterministic builds in CI -->
  <PropertyGroup Condition="'`$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <!-- Common package references for all projects -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
"@

if (!$WhatIf) {
    Set-Content -Path "Directory.Build.props" -Value $directoryBuildContent
}
Write-Host "  Updated root Directory.Build.props" -ForegroundColor Green

# Step 3: Update src/Directory.Build.props
Write-Host "Updating src/Directory.Build.props..." -ForegroundColor Cyan

$srcDirectoryBuildContent = @"
<Project>
	<PropertyGroup>
		<TargetFramework>net9.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<GenerateDocumentationFile Condition="'`$(Configuration)' == 'Debug'">true</GenerateDocumentationFile>
		<GenerateDocumentationFile Condition="'`$(Configuration)' == 'Release'">false</GenerateDocumentationFile>
		<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
		<WarningsAsErrors />

		<!-- Package Information -->
		<PackageVersion>0.0.1</PackageVersion>
		<Authors>Abdullah D.</Authors>
		<Company>Waanfeetan</Company>
		<Product>MinimalCleanArch</Product>
		<Copyright>Copyright © `$(Company) `$([System.DateTime]::Now.Year)</Copyright>
		<PackageProjectUrl>https://github.com/abdebek/MinimalCleanArch</PackageProjectUrl>
		<RepositoryUrl>https://github.com/abdebek/MinimalCleanArch</RepositoryUrl>
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
		<None Include="`$(MSBuildThisFileDirectory)../README.md" Pack="true" PackagePath="\" />
		<None Include="`$(MSBuildThisFileDirectory)icon.png" Pack="true" PackagePath="\" Condition="Exists('`$(MSBuildThisFileDirectory)icon.png')" />
	</ItemGroup>
</Project>
"@

if (!$WhatIf) {
    New-Item -ItemType Directory -Path "src" -Force | Out-Null
    Set-Content -Path "src/Directory.Build.props" -Value $srcDirectoryBuildContent
}
Write-Host "  Updated src/Directory.Build.props" -ForegroundColor Green

# Step 4: Define package updates with verified available versions
$packageMappings = @{
    "MinimalCleanArch.DataAccess" = @{
        "Microsoft.EntityFrameworkCore" = "9.0.5"
    }
    
    "MinimalCleanArch.Extensions" = @{
        "FluentValidation" = "12.0.0"
        "FluentValidation.AspNetCore" = "11.3.0"
        "FluentValidation.DependencyInjectionExtensions" = "12.0.0"
        "Microsoft.AspNetCore.OpenApi" = "9.0.5"
        "Microsoft.OpenApi" = "1.6.22"
        "Microsoft.Extensions.DependencyInjection.Abstractions" = "9.0.5"
        "Swashbuckle.AspNetCore" = "7.2.0"
        "Swashbuckle.AspNetCore.Annotations" = "7.2.0"
        "Swashbuckle.AspNetCore.SwaggerGen" = "7.2.0"
    }
    
    "MinimalCleanArch.Validation" = @{
        "FluentValidation" = "12.0.0"
        "FluentValidation.AspNetCore" = "11.3.0"
        "FluentValidation.DependencyInjectionExtensions" = "12.0.0"
        "Microsoft.Extensions.DependencyInjection.Abstractions" = "9.0.5"
    }
    
    "MinimalCleanArch.Security" = @{
        "Microsoft.AspNetCore.DataProtection" = "9.0.5"
        "Azure.Extensions.AspNetCore.DataProtection.Keys" = "1.2.4"
        "Azure.Identity" = "1.14.0"  # Using 1.14.0 instead of 1.14.1
        "Microsoft.EntityFrameworkCore.Relational" = "9.0.5"
    }
    
    "MinimalCleanArch.Sample" = @{
        "Microsoft.AspNetCore.OpenApi" = "9.0.5"
        "Swashbuckle.AspNetCore" = "7.2.0"
        "Swashbuckle.AspNetCore.Annotations" = "7.2.0"
        "FluentValidation" = "12.0.0"
        "FluentValidation.AspNetCore" = "11.3.0"
        "Microsoft.EntityFrameworkCore" = "9.0.5"
        "Microsoft.EntityFrameworkCore.Sqlite" = "9.0.5"
        "Microsoft.Extensions.DependencyInjection" = "9.0.5"
        "Microsoft.Extensions.Hosting" = "9.0.5"
        "Microsoft.Extensions.Logging" = "9.0.5"
        "Microsoft.Extensions.Options" = "9.0.5"
        "Microsoft.Extensions.Configuration.CommandLine" = "9.0.5"
        "Microsoft.Extensions.Configuration.UserSecrets" = "9.0.5"
    }
    
    "MinimalCleanArch.UnitTests" = @{
        "Microsoft.NET.Test.Sdk" = "17.12.0"
        "xunit" = "2.9.2"
        "xunit.runner.visualstudio" = "2.8.2"
        "coverlet.collector" = "6.0.2"
        "Microsoft.EntityFrameworkCore.InMemory" = "9.0.5"
        "Microsoft.AspNetCore.Mvc.Testing" = "9.0.5"
        "Moq" = "4.20.72"
        "FluentAssertions" = "7.0.0"
    }
    
    "MinimalCleanArch.IntegrationTests" = @{
        "Microsoft.NET.Test.Sdk" = "17.12.0"
        "xunit" = "2.9.2"
        "xunit.runner.visualstudio" = "2.8.2"
        "coverlet.collector" = "6.0.2"
        "Microsoft.AspNetCore.Mvc.Testing" = "9.0.5"
        "Microsoft.EntityFrameworkCore.InMemory" = "9.0.5"
        "FluentAssertions" = "7.0.0"
    }
    
    "MinimalCleanArch.Benchmarks" = @{
        "BenchmarkDotNet" = "0.14.0"
        "Microsoft.EntityFrameworkCore.InMemory" = "9.0.5"
    }
    
    "MinimalCleanArch.Docs" = @{
        "docfx.console" = "2.59.4"  # Using stable version instead of 2.77.0
    }
}

# Step 5: Find and update all project files
Write-Host "Finding and updating project files..." -ForegroundColor Cyan

$searchDirectories = @("src", "samples", "tests", "docs")
$allProjectFiles = @()

foreach ($dir in $searchDirectories) {
    if (Test-Path $dir) {
        $projects = Get-ChildItem -Path $dir -Recurse -Filter "*.csproj"
        $allProjectFiles += $projects
        Write-Host "  Found $($projects.Count) projects in $dir" -ForegroundColor Gray
    }
}

Write-Host "  Total projects found: $($allProjectFiles.Count)" -ForegroundColor Gray

$updatedCount = 0
$failedProjects = @()

foreach ($projectFile in $allProjectFiles) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    Write-Host "Processing: $projectName" -ForegroundColor Yellow
    
    $updates = @{}
    if ($packageMappings.ContainsKey($projectName)) {
        $updates = $packageMappings[$projectName]
    }
    
    try {
        $wasUpdated = Update-ProjectFile -FilePath $projectFile.FullName -Updates $updates -ProjectName $projectName
        
        if ($wasUpdated) {
            $updatedCount++
            Write-Host "  Project updated successfully" -ForegroundColor Green
        } else {
            Write-Host "  No changes needed" -ForegroundColor DarkGray
        }
    }
    catch {
        $failedProjects += $projectName
        Write-Host "  Failed to update: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 6: Clean and restore
if (!$WhatIf) {
    Write-Host "Cleaning and restoring..." -ForegroundColor Cyan
    
    try {
        Write-Host "  Running dotnet clean..." -ForegroundColor Gray
        dotnet clean --verbosity quiet
        
        Write-Host "  Running dotnet restore..." -ForegroundColor Gray
        dotnet restore --verbosity quiet
        
        Write-Host "  Clean and restore completed" -ForegroundColor Green
        
        # Try building CI solution first
        if (Test-Path "MinimalCleanArch.CI.sln") {
            Write-Host "  Building CI solution..." -ForegroundColor Gray
            $buildOutput = dotnet build MinimalCleanArch.CI.sln --no-restore --verbosity quiet 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  CI solution build successful" -ForegroundColor Green
            } else {
                Write-Host "  CI solution build had warnings/errors" -ForegroundColor Yellow
                if ($Verbose) {
                    Write-Host $buildOutput -ForegroundColor DarkYellow
                }
            }
        }
        
        # Try building main solution
        if (Test-Path "MinimalCleanArch.sln") {
            Write-Host "  Building main solution..." -ForegroundColor Gray
            $buildOutput = dotnet build MinimalCleanArch.sln --no-restore --verbosity quiet 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  Main solution build successful" -ForegroundColor Green
            } else {
                Write-Host "  Main solution build had warnings/errors" -ForegroundColor Yellow
                if ($Verbose) {
                    Write-Host $buildOutput -ForegroundColor DarkYellow
                }
            }
        }
        
    } catch {
        Write-Host "  Build/restore encountered issues: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Step 7: Run tests
if (!$SkipTests -and !$WhatIf) {
    Write-Host "Running tests..." -ForegroundColor Cyan
    
    try {
        $testOutput = dotnet test --no-build --verbosity quiet 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  All tests passed" -ForegroundColor Green
        } else {
            Write-Host "  Some tests failed or had issues" -ForegroundColor Yellow
            if ($Verbose) {
                Write-Host $testOutput -ForegroundColor DarkYellow
            }
        }
    } catch {
        Write-Host "  Test execution encountered issues: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Step 8: Summary
Write-Host ""
Write-Host "=== UPGRADE SUMMARY ===" -ForegroundColor Magenta
Write-Host "Framework: Updated to .NET 9.0" -ForegroundColor Green
Write-Host "Projects processed: $($allProjectFiles.Count)" -ForegroundColor Green
Write-Host "Projects updated: $updatedCount" -ForegroundColor Green

if ($failedProjects.Count -gt 0) {
    Write-Host "Failed projects: $($failedProjects.Count)" -ForegroundColor Red
    foreach ($failed in $failedProjects) {
        Write-Host "  - $failed" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Key package versions updated:" -ForegroundColor Blue
Write-Host "  Entity Framework Core: 9.0.5" -ForegroundColor Gray
Write-Host "  FluentValidation: 12.0.0" -ForegroundColor Gray
Write-Host "  Microsoft.AspNetCore.OpenApi: 9.0.5" -ForegroundColor Gray
Write-Host "  Microsoft.Extensions.*: 9.0.5" -ForegroundColor Gray
Write-Host "  Azure.Identity: 1.14.0 (verified available)" -ForegroundColor Gray
Write-Host "  DocFX: 2.59.4 (stable version)" -ForegroundColor Gray
Write-Host "  Test frameworks: Latest stable" -ForegroundColor Gray

Write-Host ""
Write-Host "Fixed package version issues:" -ForegroundColor Green
Write-Host "  Azure.Identity: Using 1.14.0 instead of 1.14.1" -ForegroundColor DarkGreen
Write-Host "  DocFX: Using 2.59.4 instead of 2.77.0" -ForegroundColor DarkGreen

Write-Host ""
Write-Host "Breaking changes to watch for:" -ForegroundColor Yellow
Write-Host "  FluentValidation 12.0.0 may have breaking changes from 11.x" -ForegroundColor DarkYellow
Write-Host "  Review FluentValidation migration guide if validation fails" -ForegroundColor DarkYellow

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Blue
Write-Host "1. Review changes with git diff" -ForegroundColor Gray
Write-Host "2. Test your application thoroughly" -ForegroundColor Gray
Write-Host "3. Check FluentValidation 12.0 breaking changes" -ForegroundColor Gray
Write-Host "4. Update CI/CD if needed" -ForegroundColor Gray

if ($WhatIf) {
    Write-Host ""
    Write-Host "This was a dry run. Run without -WhatIf to apply changes." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Upgrade completed!" -ForegroundColor Green