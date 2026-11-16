# run-autonomous.ps1
# Runs Claude Code on docs\SPEC.md in rounds until DONE.flag appears.
# Default model: Opus (medium effort). If a round fails without making progress
# (e.g. usage limit reached), the script waits and retries, so the work resumes
# after the 5-hour limit resets.
#
# Usage (PowerShell, anywhere in the checkout):
#   .\scripts\run-autonomous.ps1
# Optional: .\scripts\run-autonomous.ps1 -Models opus -SleepMinutes 30
#
# NOTE: uses --permission-mode bypassPermissions so nothing waits for approval.
# Only run this in the dedicated project folder.

param(
    [string[]]$Models = @("opus"),
    [hashtable]$Efforts = @{ opus = "medium"; fable = "low" },
    [int]$SleepMinutes = 20,
    [int]$MaxRounds = 80
)

$ErrorActionPreference = "Continue"
Set-Location (Split-Path $PSScriptRoot -Parent)   # repo root
New-Item -ItemType Directory -Force -Path logs | Out-Null

if (-not (Test-Path docs\SPEC.md)) { Write-Host "docs\SPEC.md not found. Run this from the repo checkout."; exit 1 }

# Locate the Claude Code CLI. If it is not on PATH, look in the usual install folders.
$claude = (Get-Command claude -ErrorAction SilentlyContinue).Source
if (-not $claude) {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\claude\claude.exe",
        "$env:USERPROFILE\.local\bin\claude.exe",
        "$env:APPDATA\npm\claude.cmd",
        "$env:ProgramFiles\Claude\claude.exe"
    )
    $claude = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $claude) {
    Write-Host "Claude Code CLI not found. Install it, open a new PowerShell window, then run this script again."
    exit 1
}
Write-Host "Using Claude Code at: $claude"
if (-not (Test-Path .git)) { git init | Out-Null }

$prompt = @"
Read docs/SPEC.md first, then docs/PROGRESS.md, docs/DECISIONS.md, docs/ARCHITECTURE.md and graphify-out/GRAPH_REPORT.md if they exist.
Continue the work exactly where docs/PROGRESS.md says you stopped (if it does not exist yet, start at M0).
Follow section 0 of docs/SPEC.md strictly: no questions, small commits, keep docs/PROGRESS.md current.
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

        & $claude -p $prompt --model $m --effort $Efforts[$m] --permission-mode bypassPermissions *> $log
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

if (Test-Path DONE.flag) { Write-Host "DONE:"; Get-Content DONE.flag } else { Write-Host "Stopped after $MaxRounds rounds without DONE.flag. See docs\PROGRESS.md and logs\." }
