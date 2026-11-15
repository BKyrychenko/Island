# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [x] M3 Timers & reminders
- [x] M4 Notes
- [x] M5 Control Center
- [x] M6 Live events + pill priority queue (+ perf measurement)
- [x] M7 Clipboard + File shelf
- [~] M8 Command bar
- [ ] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M8 part 1: pure logic in `Modules/Commands` — Calculator (own parser, %, "x% of y", functions, thousands/decimal
comma, units length/mass/temp/data incl. UK names), Fuzzy matcher, CurrencyParser (EN/UK aliases), NBU rates source +
CurrencyService (12 h cache, offline fallback), `Core/IdleHttp`. 341 tests green.

## Next step
M8 part 2: providers (apps/recent/folders index in background with cache, calculator, currency, timers/reminders via
TimersModule.Controller, system commands with confirm-by-second-Enter, web/URL/`g`, `?` → AI placeholder until M11),
CommandBarView as an overlay over the whole panel (focused input, 80 ms debounce, arrows/Enter/Esc), CommandModule
registering hotkey "commandBar" Alt+Space; snapshots; live check; docs; commit M8.
Live helpers (outside the repo, `..\scratch`): live-m1..m7.ps1, live-m7-drag.ps1 + drag-source.ps1, fake-session.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
