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
- [ ] M8 Command bar
- [ ] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M7 complete: clipboard history (DPAPI store, privacy formats, excluded apps, paste into previous window + restore) and
File shelf (drag in → expands to Shelf tab, drag out via DoDragDrop, shelf.json, missing marks, open/copy path/remove/
clear, remove-after-drop option). Verified live: capture, private skip, paste, drag-in. 247 tests green. Snapshots in
logs/shots/m7.

## Next step
Start M8 (Command bar, spec 4.10, Alt+Space): read section 4.10 first. Likely pieces: `Modules/Commands` — a fuzzy
matcher (tested), calculator parser (tested), currency parsing/conversion with a fake rates source (tested), app/file
index (Start-menu shortcuts + configured folders), system commands that confirm first; a command-bar view inside the
island that takes focus (AcquireFocus) and returns it on collapse; hotkey "commandBar" Alt+Space via island.Hotkeys.
Live helpers (outside the repo, `..\scratch`): live-m1..m7.ps1, live-m7-drag.ps1 + drag-source.ps1, fake-session.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
