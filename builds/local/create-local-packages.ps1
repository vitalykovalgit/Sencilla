<#
.SYNOPSIS
    Builds and publishes Sencilla NuGet packages to a local directory for development/testing.

.DESCRIPTION
    Packs all Sencilla library projects with a local/preview version suffix and copies
    them to a local NuGet feed directory. Configure your projects to consume packages
    from this directory for fast iteration without pushing to NuGet.org.

.PARAMETER Version
    The base version to use (default: reads from Directory.Build.props or "10.0.0").
    A "-local.<timestamp>" suffix is appended automatically unless -ExactVersion is set.

.PARAMETER Suffix
    Pre-release suffix to append (default: "local"). Results in versions like "10.0.0-local.1".
    Common values: "local", "dev", "preview".

.PARAMETER LocalNugetDir
    Path to the local NuGet feed directory (default: $HOME/local-nuget).
    This directory will be created if it does not exist.

.PARAMETER Configuration
    Build configuration (default: "Release").

.PARAMETER ExactVersion
    If specified, use the Version parameter as-is without appending a suffix.

.PARAMETER Clean
    If specified, removes existing packages for the same base version from the local feed.

.EXAMPLE
    # Use defaults — auto-version, ~/local-nuget
    .\create-local-packages.ps1

.EXAMPLE
    # Specific version with dev suffix
    .\create-local-packages.ps1 -Version "10.0.3" -Suffix "dev"
    # Produces: 10.0.3-dev.1

.EXAMPLE
    # Exact version, custom directory
    .\create-local-packages.ps1 -Version "10.0.3-preview.1" -ExactVersion -LocalNugetDir "C:\nuget-local"

.EXAMPLE
    # Clean old packages before building
    .\create-local-packages.ps1 -Clean
#>

[CmdletBinding()]
param(
    [string]$Version = "",
    [string]$Suffix = "local",
    [string]$LocalNugetDir = "$HOME/local-nuget",
    [string]$Configuration = "Release",
    [switch]$ExactVersion,
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Resolve paths ─────────────────────────────────────────────────────────────
$ScriptDir  = $PSScriptRoot
$RepoRoot   = Resolve-Path (Join-Path $ScriptDir "../..")
$SolutionFile = Join-Path $RepoRoot "Sencilla.sln"
$ArtifactsDir = Join-Path $RepoRoot "artifacts/nuget-local"

# ── Determine version ─────────────────────────────────────────────────────────
if (-not $Version) {
    # Try to read from Directory.Build.props
    $buildProps = Join-Path $RepoRoot "Directory.Build.props"
    if (Test-Path $buildProps) {
        $xml = [xml](Get-Content $buildProps)
        $Version = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    }
    if (-not $Version) { $Version = "10.0.0" }
}

if (-not $ExactVersion) {
    # Append suffix + build counter based on current minute to ensure uniqueness
    $Counter = [int](Get-Date -Format "yyyyMMddHHmm")
    $PackageVersion = "$Version-$Suffix.$Counter"
} else {
    $PackageVersion = $Version
}

# ── Expand local NuGet directory path ─────────────────────────────────────────
$LocalNugetDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($LocalNugetDir)

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Sencilla Local Package Builder" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Package version : $PackageVersion" -ForegroundColor White
Write-Host "  Configuration   : $Configuration" -ForegroundColor White
Write-Host "  Artifacts dir   : $ArtifactsDir" -ForegroundColor White
Write-Host "  Local feed dir  : $LocalNugetDir" -ForegroundColor White
Write-Host "  Solution        : $SolutionFile" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Clean artifacts dir ───────────────────────────────────────────────────────
if (Test-Path $ArtifactsDir) {
    Remove-Item $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null

# ── Create local feed dir ─────────────────────────────────────────────────────
if (-not (Test-Path $LocalNugetDir)) {
    Write-Host "Creating local NuGet feed directory: $LocalNugetDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $LocalNugetDir | Out-Null
}

# ── Clean old packages (same base version) ────────────────────────────────────
if ($Clean) {
    Write-Host "Cleaning old packages from $LocalNugetDir ..." -ForegroundColor Yellow
    $OldPackages = Get-ChildItem -Path $LocalNugetDir -Filter "Sencilla.*.$Version*.nupkg" -ErrorAction SilentlyContinue
    $OldPackages += Get-ChildItem -Path $LocalNugetDir -Filter "Microsoft.EntityFrameworkCore.*.$Version*.nupkg" -ErrorAction SilentlyContinue
    foreach ($pkg in $OldPackages) {
        Remove-Item $pkg.FullName -Force
        Write-Host "  Removed: $($pkg.Name)" -ForegroundColor Gray
    }
    Write-Host ""
}

# ── Also clear NuGet global cache for these packages ─────────────────────────
$NugetCache = Join-Path $env:USERPROFILE ".nuget\packages"
if (Test-Path $NugetCache) {
    Write-Host "Clearing NuGet global cache for Sencilla packages..." -ForegroundColor Yellow
    $CacheEntries = Get-ChildItem -Path $NugetCache -Directory -Filter "sencilla.*" -ErrorAction SilentlyContinue
    $CacheEntries += Get-ChildItem -Path $NugetCache -Directory -Filter "microsoft.entityframeworkcore.extension" -ErrorAction SilentlyContinue
    foreach ($entry in $CacheEntries) {
        # Remove only the version subdirectory to avoid breaking other versions
        $VersionDir = Join-Path $entry.FullName $PackageVersion
        if (Test-Path $VersionDir) {
            Remove-Item $VersionDir -Recurse -Force
            Write-Host "  Cache cleared: $($entry.Name)/$PackageVersion" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

# ── Restore ───────────────────────────────────────────────────────────────────
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore $SolutionFile
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

# ── Build ─────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Building solution ($Configuration, v$PackageVersion)..." -ForegroundColor Yellow
dotnet build $SolutionFile `
    --configuration $Configuration `
    --no-restore `
    /p:Version=$PackageVersion
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

# ── Pack ──────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Packing NuGet packages..." -ForegroundColor Yellow
dotnet pack $SolutionFile `
    --configuration $Configuration `
    --no-build `
    --output $ArtifactsDir `
    /p:Version=$PackageVersion
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

# ── Copy to local feed ────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Copying packages to local feed..." -ForegroundColor Yellow
$Packages = Get-ChildItem -Path $ArtifactsDir -Filter "*.nupkg"

if ($Packages.Count -eq 0) {
    Write-Host "No packages found in $ArtifactsDir" -ForegroundColor Red
    exit 1
}

foreach ($pkg in $Packages) {
    $Dest = Join-Path $LocalNugetDir $pkg.Name
    Copy-Item $pkg.FullName -Destination $Dest -Force
    Write-Host "  ✓ $($pkg.Name)" -ForegroundColor Green
}

# ── Ensure nuget.config has the local feed ────────────────────────────────────
$NugetConfigPath = Join-Path $RepoRoot "nuget.local.config"
if (-not (Test-Path $NugetConfigPath)) {
    Write-Host ""
    Write-Host "Creating nuget.local.config pointing to local feed..." -ForegroundColor Yellow
    $NugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Local Sencilla" value="$LocalNugetDir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
    Set-Content -Path $NugetConfigPath -Value $NugetConfigContent
    Write-Host "  Created: $NugetConfigPath" -ForegroundColor Green
    Write-Host "  Use with: dotnet restore --configfile nuget.local.config" -ForegroundColor Cyan
}

# ── Done ──────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Done! $($Packages.Count) package(s) published" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "To use in your project, add the local feed to your nuget.config:" -ForegroundColor White
Write-Host ""
Write-Host "  <packageSources>" -ForegroundColor Gray
Write-Host "    <add key=`"Local Sencilla`" value=`"$LocalNugetDir`" />" -ForegroundColor Gray
Write-Host "  </packageSources>" -ForegroundColor Gray
Write-Host ""
Write-Host "Then reference the package:" -ForegroundColor White
Write-Host ""
Write-Host "  dotnet add package Sencilla.Core --version $PackageVersion" -ForegroundColor Gray
Write-Host ""
