<#
.SYNOPSIS
    Static check: does everything the built mod references in sts2.dll / BaseLib.dll exist, with the same signature,
    in the OTHER game install? Lists the call sites that would throw MissingMethodException there, including
    `override`s whose base method does not exist on that version (compiles fine, is just never called).

.DESCRIPTION
    Tests only cover the code paths they happen to run. This covers every reference in the dll, so run it after
    every game update and after adding code that touches new game API.

    The mod is compiled once against beta (0.113.1). Checking it against beta must always come back clean (that
    proves the checker has no false positives); checking it against public (0.111.1) must come back clean too, apart
    from the overrides the mod bridges with a Harmony shim (listed in $shimmed below, implemented in
    ActsFromThePastCode/Compatibility/).

.EXAMPLE
    ./build/check-compat.ps1                  # builds, then checks public (the older install)
    ./build/check-compat.ps1 -Branch beta     # sanity check against what it was compiled against
    ./build/check-compat.ps1 -NoBuild
#>
param(
    [ValidateSet("public", "beta")][string]$Branch = "public",
    [switch]$NoBuild
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
. (Join-Path $PSScriptRoot "common.ps1")
$props = Get-LocalProps

# Overrides that only exist on 0.113.0 and are routed by a shim on 0.111.1 (Compatibility/GameApiCompat.cs, AfterDeathCompat.cs).
$shimmed = "AfterDeath,ModifyDamageMultiplicative,AfterThisWasTransformedFrom"

if (-not $NoBuild) {
    Write-Host "=== Building (ExportDebug, against beta) ===" -ForegroundColor Cyan
    dotnet build ActsFromThePast.csproj -c ExportDebug --nologo -v q "-p:Sts2Branch=beta" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
}
dotnet build tools/CompatCheck -c Release --nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw "CompatCheck build failed" }

$game = Get-Sts2Path $props $Branch
$sts2 = Join-Path $game "data_sts2_windows_x86_64/sts2.dll"
$baselib = Join-Path (Get-Sts2ModsDir $game) "BaseLib/BaseLib.dll"
foreach ($f in $sts2, $baselib) { if (-not (Test-Path $f)) { throw "Not found: $f" } }

Write-Host "=== Checking the mod against the $Branch install ===" -ForegroundColor Cyan
dotnet tools/CompatCheck/bin/Release/net9.0/CompatCheck.dll ".godot/mono/temp/bin/ExportDebug/ActsFromThePast.dll" $sts2 $baselib "--shimmed=$shimmed"
exit $LASTEXITCODE
