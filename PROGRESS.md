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
- [x] M8 Command bar
- [x] M9 Screen tools
- [x] M10 Weather
- [x] M11 AI chat + text actions (+ perf measurement)
- [x] M12 Voice to text
- [x] M13 Agents integration
- [x] M14 Characters
- [~] M15 Polish & release

## Last action
M15 step 1 done: settings panel inside the island (`Views/SettingsPanel/SettingsView` + snapshots + tests), opened by the
gear and the tray "Settings" (`App.OpenSettings`). Sections: General (theme, language, size slider with live preview,
monitor, clock, fullscreen/edge/click-through, autostart, excluded apps), Modules (16 switches → `Host.Sync`),
Options (lyrics, weather, live sources, OBS port/password, downloads folder, clipboard limit/excluded apps, shelf,
command-bar folders, recording audio, AI link, voice model/language, agents approvals/timeout/hooks link, character
gallery link), Hotkeys (click + press keys, reset, failures shown; global hotkeys suspended while recording), About.
Live-checked with UI Automation (`..\scratch\live-m15.ps1`).

## Next step
M15 remaining: Ukrainian strings review; every-state snapshots in both themes/languages (`--snapshot all`) and look
at them; full perf measurement (idle 60 s, all modules); 10-min soak test (script: music fake session, timers, notes,
clipboard copies, control toggles, tab switching; watch log + working set); README.md, VERIFICATION.md (add settings
panel rows), ARCHITECTURE.md (add Live events / Clipboard / Shelf / Command bar / Screen tools / Settings panel
paragraphs — missing now); build.ps1 run + launch dist exe (both variants). Then DONE.flag + final message in Ukrainian.
Notes: publish ships Whisper `runtimes\win-arm64` and `win-x86` too (~3.5 MB) — trim if easy; check the self-contained
build loads whisper.dll.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
