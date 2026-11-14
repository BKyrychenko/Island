# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [~] M1 Expand/collapse & shell (+ perf measurement) — not started yet
- [ ] M2 Music + lyrics
- [ ] M3 Timers & reminders
- [ ] M4 Notes
- [ ] M5 Control Center
- [ ] M6 Live events + pill priority queue (+ perf measurement)
- [ ] M7 Clipboard + File shelf
- [ ] M8 Command bar
- [ ] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M0 code: solution (`Isle.sln`, `src/Isle`, `tests/Isle.Tests`), Core (Paths, Logger, JsonStore, Settings, EventBus,
ModuleHost, Strings EN/UK, Screens, Autostart, Memory), `IslandWindow` (fixed transparent topmost no-activate window,
black 120×32 pill, top-centre, PerMonitorV2), tray module (Shell_NotifyIcon + native menu), single instance,
`build.ps1`, docs stubs. Build clean, 12 tests green, app launched and pill verified by screenshot.
graphify 0.9.108 reinstalled through uv (Python 3.12), graph built (`graphify update .`), `graphify claude install` done.
M0 complete and committed.

## Next step
Start M1: hover logic (30 ms cursor timer, 120 ms intent, 250 ms leave), spring expand/collapse on a fixed window,
tab bar with sliding indicator, Styles/Theme.xaml (dark/light/system), live UI scale, auto-hide to edge, click-through mode,
fullscreen auto-hide, excluded apps, hotkey infrastructure (RegisterHotKey), --snapshot <state> <out.png> tool, perf measurement.
At the end of each milestone run `graphify update .` (code graph, no LLM) and commit.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
