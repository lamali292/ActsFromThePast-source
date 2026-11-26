<#
.SYNOPSIS
    Builds the mod against the beta game (0.113.1) and deploys dll + pck + manifest to the mods folder of BOTH game
    installs (beta and public), so the one build can be tried on either.

.EXAMPLE
    ./build/build.ps1                 # dll only (fast), mirrored to both installs
    ./build/build.ps1 -Pck            # also export the Godot .pck (needed after changing localization, images or scenes)
    ./build/build.ps1 -Configuration Release -Pck

.NOTES
    The mod is compiled against ONE game version and has to run unchanged on the other; the differences between
    0.111.1 and 0.113.x are bridged at runtime in ActsFromThePastCode/Compatibility/. Compiling against the older
    game is not supported (it lacks the newer overrides the code is written against).
#>
param(
    [string]$Configuration = "ExportDebug",
    [switch]$Pck
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
. (Join-Path $PSScriptRoot "common.ps1")
$null = Get-LocalProps -NeedGodot:$Pck

$verb = if ($Pck) { "publish" } else { "build" }
Write-Host "=== dotnet $verb ActsFromThePast ($Configuration, against beta) ===" -ForegroundColor Cyan
dotnet $verb ActsFromThePast.csproj -c $Configuration --nologo -v q "-p:Sts2Branch=beta" | Out-Host
if ($LASTEXITCODE -ne 0) {
    # The Publicizer cache is cold on the first build after switching branches and fails once.
    Write-Host "Retrying (cold publicizer cache)..." -ForegroundColor Yellow
    dotnet $verb ActsFromThePast.csproj -c $Configuration --nologo -v q "-p:Sts2Branch=beta" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
}

$props = Get-LocalProps
foreach ($branch in "beta", "public") {
    $mods = Join-Path (Get-Sts2ModsDir (Get-Sts2Path $props $branch)) "ActsFromThePast"
    $files = Get-ChildItem $mods -ErrorAction SilentlyContinue | ForEach-Object { "$($_.Name) ($($_.LastWriteTime.ToString('HH:mm:ss')))" }
    Write-Host ("  {0,-7} {1}" -f $branch, ($files -join ", "))
}
Write-Host "BUILT" -ForegroundColor Green
