# build.ps1 - builds, tests and publishes Isle into dist\
#   dist\Isle\                framework-dependent single file (needs .NET 10 Desktop Runtime)
#   dist\Isle-selfcontained\  self-contained single file (no runtime needed)
# Usage: .\scripts\build.ps1 [-SkipTests] [-OutputDirectory dist]
param([switch]$SkipTests, [string]$OutputDirectory = "dist")

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)   # repo root
$rustc = (Get-Command rustc.exe -CommandType Application -ErrorAction Stop).Source
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

$launcherDir = New-Item -ItemType Directory -Path "target\isle-launcher" -Force
$launcher = Join-Path $launcherDir.FullName "Isle.Launcher.exe"
Step "build Rust launcher" {
    & $rustc --edition=2024 --target x86_64-pc-windows-msvc --crate-name isle_launcher -C opt-level=s -C strip=symbols src\Isle.Launcher\main.rs -o $launcher
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
    Copy-Item -LiteralPath $launcher -Destination $d -Force
    foreach ($arch in "win-arm64", "win-x86") {
        $native = Join-Path $d "runtimes\$arch"
        if (Test-Path -LiteralPath $native) { Remove-Item -LiteralPath $native -Recurse -Force }
    }
}

if (-not $SkipTests) {
    $smoke = Start-Process -FilePath (Join-Path $framework "Isle.Launcher.exe") -ArgumentList "--snapshot" -PassThru
    if (-not $smoke.WaitForExit(30000) -or $smoke.ExitCode -ne 2) {
        throw "Rust launcher failed to return the WPF snapshot usage exit code."
    }
}

Write-Host "Done:" -ForegroundColor Green
Get-ChildItem -LiteralPath $framework, $bundled -Recurse -Filter *.exe | ForEach-Object { "  {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB) }
