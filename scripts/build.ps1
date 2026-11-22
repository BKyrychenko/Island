# build.ps1 - builds, tests and publishes Isle into dist\
#   dist\Isle\                framework-dependent single file (needs .NET 10 Desktop Runtime)
#   dist\Isle-selfcontained\  self-contained single file (no runtime needed)
#   dist\Isle-Dev\, dist\Isle-Dev-selfcontained\  the same plus the developer pack (Isle.Dev.dll)
# Usage: .\scripts\build.ps1 [-SkipTests] [-OutputDirectory dist]
# The small Rust launcher (Isle.Launcher.exe) is built when rustc is on PATH; without it Isle.exe is started directly.
param([switch]$SkipTests, [string]$OutputDirectory = "dist")

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)   # repo root
$rustc = Get-Command rustc.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$framework = Join-Path $output "Isle"
$bundled = Join-Path $output "Isle-selfcontained"

function Step($name, [scriptblock]$body) {
    Write-Host "== $name" -ForegroundColor Cyan
    & $body
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit $LASTEXITCODE)" }
}

Step "build" { dotnet build Isle.sln -c Release -nologo -v q }
if (-not $SkipTests) { Step "test" { dotnet test Isle.sln -c Release -nologo --no-build -v q } }

$launcher = $null
if ($rustc) {
    $launcherDir = New-Item -ItemType Directory -Path "target\isle-launcher" -Force
    $launcher = Join-Path $launcherDir.FullName "Isle.Launcher.exe"
    Step "build Rust launcher" {
        & $rustc --edition=2024 --target x86_64-pc-windows-msvc --crate-name isle_launcher -C opt-level=s -C strip=symbols src\Isle.Launcher\main.rs -o $launcher
    }
}
else {
    Write-Host "== rustc not found: skipping Isle.Launcher.exe (run Isle.exe directly)" -ForegroundColor Yellow
}

$proj = "src\Isle\Isle.csproj"
Step "publish (framework-dependent)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained false -o $framework -nologo -v q
}
Step "publish (self-contained)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained true -o $bundled -nologo -v q
}

if (Test-Path "src\Isle.Hook\Isle.Hook.csproj") {
    Step "publish hook helper (framework-dependent)" {
        dotnet publish "src\Isle.Hook\Isle.Hook.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o $framework -nologo -v q
    }
    Step "publish hook helper (self-contained)" {
        dotnet publish "src\Isle.Hook\Isle.Hook.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true --self-contained true -o $bundled -nologo -v q
    }
}

# Native DLLs stay loose next to the exe (Whisper looks for runtimes\win-x64\whisper.dll there; a bundled copy is
# extracted elsewhere and not found). Other architectures are never loaded on this machine.
foreach ($d in $framework, $bundled) {
    if ($launcher) { Copy-Item -LiteralPath $launcher -Destination $d -Force }
    foreach ($arch in "win-arm64", "win-x86") {
        $native = Join-Path $d "runtimes\$arch"
        if (Test-Path -LiteralPath $native) { Remove-Item -LiteralPath $native -Recurse -Force }
    }
}

# Developer pack: the community folders plus Isle.Dev.dll from the solution build.
# ponytail: the pack is copied as plain IL (no ReadyToRun); publish it with R2R if its start-up time ever shows.
$pack = "src\Isle.Dev\bin\Release\net10.0-windows10.0.19041.0\Isle.Dev.dll"
$devFramework = Join-Path $output "Isle-Dev"
$devBundled = Join-Path $output "Isle-Dev-selfcontained"
foreach ($pair in @(@($framework, $devFramework), @($bundled, $devBundled))) {
    if (Test-Path -LiteralPath $pair[1]) { Remove-Item -LiteralPath $pair[1] -Recurse -Force }
    Copy-Item -LiteralPath $pair[0] -Destination $pair[1] -Recurse
    Copy-Item -LiteralPath $pack -Destination $pair[1]
}

if (-not $SkipTests) {
    # --snapshot without arguments prints usage and exits with 2: proves the published app starts
    $entry = if ($launcher) { "Isle.Launcher.exe" } else { "Isle.exe" }
    $smoke = Start-Process -FilePath (Join-Path $framework $entry) -ArgumentList "--snapshot" -PassThru
    if (-not $smoke.WaitForExit(30000) -or $smoke.ExitCode -ne 2) {
        throw "$entry failed to return the snapshot usage exit code."
    }
    if (Test-Path (Join-Path $framework "Isle.Dev.dll")) { throw "the community build contains Isle.Dev.dll." }

    # A developer-pack state renders only when the single-file exe loaded Isle.Dev.dll.
    $png = Join-Path ([IO.Path]::GetTempPath()) "isle-dev-smoke-$PID.png"
    Remove-Item -LiteralPath $png -ErrorAction SilentlyContinue
    $smoke = Start-Process -FilePath (Join-Path $devFramework "Isle.exe") -ArgumentList "--snapshot", "sysmon-alert", "`"$png`"" -PassThru
    if (-not $smoke.WaitForExit(60000) -or $smoke.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $png)) {
        throw "Isle-Dev\Isle.exe did not load the developer pack (sysmon-alert snapshot failed)."
    }
    Remove-Item -LiteralPath $png
}

Write-Host "Done:" -ForegroundColor Green
Get-ChildItem -LiteralPath $framework, $bundled, $devFramework, $devBundled -Recurse -Filter *.exe | ForEach-Object { "  {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB) }
