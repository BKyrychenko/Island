# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [~] M0 Skeleton — code done and committed; graphify setup still to do (see Next step)
- [ ] M1 Expand/collapse & shell (+ perf measurement)
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

## Next step
1. graphify setup (SPEC §0): the `graphify` CLI on PATH is broken ("uv trampoline failed"); try
   `python -m pip install graphifyy` then `graphify install`, run it on the repo, `graphify claude install`.
   If it cannot be made to work in ~10 minutes, note it in DECISIONS.md and move on.
2. Mark M0 `[x]`, commit, start M1 (hover logic, spring animation, tab bar, themes, scale, auto-hide, click-through,
   fullscreen hide, excluded apps, hotkeys infrastructure, `--snapshot` tool, perf measurement).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
