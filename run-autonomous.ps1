# run-autonomous.ps1
# Runs Claude Code on SPEC.md in rounds until DONE.flag appears.
# Model order: Fable (low effort) first; if it fails without making progress
# (e.g. usage limit reached), the same round falls back to Opus; if both fail,
# waits and retries (so the work resumes after the 5-hour limit resets).
#
# Usage (PowerShell, in the project folder that contains SPEC.md):
#   .\run-autonomous.ps1
# Optional: .\run-autonomous.ps1 -Models fable,opus -SleepMinutes 20
#
# NOTE: uses --permission-mode bypassPermissions so nothing waits for approval.
# Only run this in the dedicated project folder.

param(
    [string[]]$Models = @("fable", "opus"),
    [hashtable]$Efforts = @{ fable = "low"; opus = "medium" },
    [int]$SleepMinutes = 20,
    [int]$MaxRounds = 80
)

$ErrorActionPreference = "Continue"
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force -Path logs | Out-Null

if (-not (Test-Path SPEC.md)) { Write-Host "SPEC.md not found next to this script."; exit 1 }
if (-not (Test-Path .git)) { git init | Out-Null }

$prompt = @"
Read SPEC.md first, then PROGRESS.md, DECISIONS.md, ARCHITECTURE.md and graphify-out/GRAPH_REPORT.md if they exist.
Continue the work exactly where PROGRESS.md says you stopped (if PROGRESS.md does not exist yet, start at M0).
Follow section 0 of SPEC.md strictly: no questions, small commits, keep PROGRESS.md current.
Create DONE.flag only when the Definition of Done in section 9 is verified.
"@

function Get-Head { $h = git rev-parse HEAD 2>$null; if ($LASTEXITCODE -eq 0) { $h } else { "none" } }

for ($round = 1; $round -le $MaxRounds; $round++) {
    if (Test-Path DONE.flag) { break }
    $progressed = $false

    foreach ($m in $Models) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $log = "logs\round$round-$m-$stamp.log"
        $before = Get-Head
        Write-Host "[$(Get-Date -Format HH:mm:ss)] round $round, model $m, effort $($Efforts[$m])"

        & claude -p $prompt --model $m --effort $Efforts[$m] --permission-mode bypassPermissions *> $log
        $code = $LASTEXITCODE
        $after = Get-Head

        if (Test-Path DONE.flag) { break }
        if ($code -eq 0 -or $before -ne $after) {
            # The model did real work (or ended cleanly). Start the next round with the preferred model.
            $progressed = $true
            break
        }
        Write-Host "  failed with no progress (exit $code), see $log. Trying next model..."
    }

    if (Test-Path DONE.flag) { break }
    if (-not $progressed) {
        Write-Host "[$(Get-Date -Format HH:mm:ss)] all models failed (likely usage limit). Sleeping $SleepMinutes min..."
        Start-Sleep -Seconds ($SleepMinutes * 60)
    }
}

if (Test-Path DONE.flag) { Write-Host "DONE:"; Get-Content DONE.flag } else { Write-Host "Stopped after $MaxRounds rounds without DONE.flag. See PROGRESS.md and logs\." }
