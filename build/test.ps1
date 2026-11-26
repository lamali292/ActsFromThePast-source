<#
.SYNOPSIS
    Builds the mod, launches Slay the Spire 2 with the in-game test runner armed, waits for it to finish and prints
    the results. The mod is compiled once against beta (0.113.1) and the same build outputs are run on BOTH game
    installs (beta, then public = 0.111.1) by default. Exit code 0 = everything passed on every install.

.EXAMPLE
    ./build/test.ps1                          # every test on beta and public
    ./build/test.ps1 -Filter Flight           # only tests whose "Type.Method" contains "Flight"
    ./build/test.ps1 -NoBuild -Branch public  # just the public install, reusing the last build
    ./build/test.ps1 -Pck                     # also re-export the .pck first (after localization/scene changes)

.NOTES
    Requires Steam running (the game exe is launched directly). Game folders come from local.props
    (Sts2BetaPath / Sts2PublicPath). The mod needs a .pck in the mods folder (localization lives in it): run
    ./build/build.ps1 -Pck once, or pass -Pck here.
#>
param(
    [string]$Filter = "",
    [switch]$NoBuild,
    [switch]$Pck,
    [ValidateSet("both", "beta", "public")][string]$Branch = "both",
    [int]$TimeoutMinutes = 5
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
. (Join-Path $PSScriptRoot "common.ps1")

# The game exe is launched directly but still needs the Steam client for its API; without it the game fails to
# start and no result file is written, so bail out before spending time on builds.
if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) {
    Write-Host "Steam is not running - start Steam and re-run the tests." -ForegroundColor Red
    exit 1
}

$props = Get-LocalProps

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot "build.ps1") -Pck:$Pck
}

function Invoke-BranchTests([string]$BranchName) {
    $gameDir = Get-Sts2Path $props $BranchName
    $exe = Get-Sts2ExePath $gameDir
    if (-not (Test-Path $exe)) { throw "Game executable not found: $exe" }
    if (-not (Test-Path (Join-Path (Get-Sts2ModsDir $gameDir) "ActsFromThePast/ActsFromThePast.pck"))) {
        throw "No ActsFromThePast.pck in the $BranchName mods folder. Run ./build/build.ps1 -Pck first."
    }

    $outFile = Join-Path (Get-TempDir) "aftp_tests_$BranchName.json"
    Remove-Item $outFile -ErrorAction SilentlyContinue

    $env:AFTP_RUN_TESTS = "1"
    $env:AFTP_TEST_FILTER = $Filter
    $env:AFTP_TEST_OUTPUT = $outFile
    try {
        Write-Host "=== [$BranchName] Launching game headlessly (filter: '$Filter') ===" -ForegroundColor Cyan
        $proc = Start-Process -FilePath $exe -ArgumentList "--headless", "--audio-driver", "Dummy" -WorkingDirectory (Split-Path $exe) -PassThru
        if (-not $proc.WaitForExit($TimeoutMinutes * 60 * 1000)) {
            $proc.Kill()
            throw "Timed out after $TimeoutMinutes minutes; killed the game ($BranchName)."
        }
    }
    finally {
        Remove-Item Env:AFTP_RUN_TESTS, Env:AFTP_TEST_FILTER, Env:AFTP_TEST_OUTPUT -ErrorAction SilentlyContinue
    }

    if (-not (Test-Path $outFile)) {
        throw "No result file was written for $BranchName (game exit code $($proc.ExitCode)). Check $(Get-Sts2LogPath)"
    }

    $result = Get-Content $outFile -Raw | ConvertFrom-Json
    Write-Host ""
    Write-Host ("[{0}] game {1}  |  seed {2}  |  {3:n1}s  |  {4} passed, {5} failed" -f `
        $BranchName, $result.GameVersion, $result.Seed, ([TimeSpan]$result.Duration).TotalSeconds, $result.Passed.Count, $result.Failed.Count) `
        -ForegroundColor $(if ($result.Failed.Count -eq 0) { "Green" } else { "Red" })
    foreach ($p in $result.Passed) { Write-Host "  PASS  $p" -ForegroundColor DarkGreen }
    foreach ($f in $result.Failed) {
        Write-Host "  FAIL  $($f.Name)" -ForegroundColor Red
        Write-Host "        $($f.Message)"
    }
    Write-Host ""
    Write-Host "Full JSON: $outFile"
    return ($result.Failed.Count -eq 0)
}

$branches = if ($Branch -eq "both") { @("beta", "public") } else { @($Branch) }
$summary = [ordered]@{}
foreach ($b in $branches) {
    try { $summary[$b] = if (Invoke-BranchTests $b) { "PASSED" } else { "FAILED" } }
    catch {
        Write-Host "[$b] ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $summary[$b] = "ERROR"
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
foreach ($b in $summary.Keys) {
    Write-Host ("  {0,-7} {1}" -f $b, $summary[$b]) -ForegroundColor $(if ($summary[$b] -eq "PASSED") { "Green" } else { "Red" })
}
exit $(if (@($summary.Values | Where-Object { $_ -ne "PASSED" }).Count -eq 0) { 0 } else { 1 })
