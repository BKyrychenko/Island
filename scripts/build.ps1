# build.ps1 - builds, tests and publishes Skerry into dist\
#   dist\Skerry\                framework-dependent single file (needs .NET 10 Desktop Runtime)
#   dist\Skerry-selfcontained\  self-contained single file (no runtime needed)
# Usage: .\scripts\build.ps1 [-SkipTests] [-OutputDirectory dist]
# The small Rust launcher (Skerry.Launcher.exe) is built when rustc is on PATH; without it Skerry.exe is started directly.
param([switch]$SkipTests, [string]$OutputDirectory = "dist")

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)   # repo root
$rustc = Get-Command rustc.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$framework = Join-Path $output "Skerry"
$bundled = Join-Path $output "Skerry-selfcontained"

function Step($name, [scriptblock]$body) {
    Write-Host "== $name" -ForegroundColor Cyan
    & $body
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit $LASTEXITCODE)" }
}

Step "build" { dotnet build Skerry.sln -c Release -nologo -v q }
if (-not $SkipTests) { Step "test" { dotnet test Skerry.sln -c Release -nologo --no-build -v q } }

$launcher = $null
if ($rustc) {
    $launcherDir = New-Item -ItemType Directory -Path "target\skerry-launcher" -Force
    $launcher = Join-Path $launcherDir.FullName "Skerry.Launcher.exe"
    Step "build Rust launcher" {
        & $rustc --edition=2024 --target x86_64-pc-windows-msvc --crate-name skerry_launcher -C opt-level=s -C strip=symbols src\Skerry.Launcher\main.rs -o $launcher
    }
}
else {
    Write-Host "== rustc not found: skipping Skerry.Launcher.exe (run Skerry.exe directly)" -ForegroundColor Yellow
}

$proj = "src\Skerry\Skerry.csproj"
Step "publish (framework-dependent)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained false -o $framework -nologo -v q
}
Step "publish (self-contained)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained true -o $bundled -nologo -v q
}

if (Test-Path "src\Skerry.Hook\Skerry.Hook.csproj") {
    Step "publish hook helper (framework-dependent)" {
        dotnet publish "src\Skerry.Hook\Skerry.Hook.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o $framework -nologo -v q
    }
    Step "publish hook helper (self-contained)" {
        dotnet publish "src\Skerry.Hook\Skerry.Hook.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true --self-contained true -o $bundled -nologo -v q
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

if (-not $SkipTests) {
    # --snapshot without arguments prints usage and exits with 2: proves the published app starts
    $entry = if ($launcher) { "Skerry.Launcher.exe" } else { "Skerry.exe" }
    $smoke = Start-Process -FilePath (Join-Path $framework $entry) -ArgumentList "--snapshot" -PassThru
    if (-not $smoke.WaitForExit(30000) -or $smoke.ExitCode -ne 2) {
        throw "$entry failed to return the snapshot usage exit code."
    }
}

Write-Host "Done:" -ForegroundColor Green
Get-ChildItem -LiteralPath $framework, $bundled -Recurse -Filter *.exe | ForEach-Object { "  {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB) }
