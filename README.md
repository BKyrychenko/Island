# Isle

A small always-on-top "island" pill for Windows 10/11 (personal project). Work in progress — see `PROGRESS.md`.

## Build and run
Requires the .NET 10 SDK.

```powershell
.\build.ps1                 # build + test + publish into dist\
dist\Isle\Isle.exe          # needs the .NET 10 Desktop Runtime
dist\Isle-selfcontained\Isle.exe   # no runtime needed
```

Quit and "Start with Windows" are in the tray icon menu. Data lives in `%AppData%\Isle\`.
