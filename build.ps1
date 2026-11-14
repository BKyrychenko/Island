# build.ps1 - builds, tests and publishes Isle into dist\
#   dist\Isle\                framework-dependent single file (needs .NET 10 Desktop Runtime)
#   dist\Isle-selfcontained\  self-contained single file (no runtime needed)
# Usage: .\build.ps1 [-SkipTests]
param([switch]$SkipTests)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Step($name, [scriptblock]$body) {
    Write-Host "== $name" -ForegroundColor Cyan
    & $body
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit $LASTEXITCODE)" }
}

Step "build" { dotnet build Isle.sln -c Release -nologo -v q }
if (-not $SkipTests) { Step "test" { dotnet test Isle.sln -c Release -nologo --no-build -v q } }

$proj = "src\Isle\Isle.csproj"
Step "publish (framework-dependent)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained false -o dist\Isle -nologo -v q
}
Step "publish (self-contained)" {
    dotnet publish $proj -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -o dist\Isle-selfcontained -nologo -v q
}

if (Test-Path "src\Isle.Hook\Isle.Hook.csproj") {
    Step "publish hook helper" {
        dotnet publish "src\Isle.Hook\Isle.Hook.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o dist\Isle -nologo -v q
    }
    Copy-Item dist\Isle\Isle.Hook.* dist\Isle-selfcontained\ -Force
}

Write-Host "Done:" -ForegroundColor Green
Get-ChildItem dist -Recurse -Filter *.exe | ForEach-Object { "  {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB) }
