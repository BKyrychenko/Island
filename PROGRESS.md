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
- [ ] M15 Polish & release

## Last action
M14 done: `Modules/Characters` (CreatureBrain, CreatureDesign Pebb/Sprig/Kubo, CharactersModule, CharacterSnapshots),
`Views/Characters` (CreatureView, CharacterGallery), tab-bar mascot slot (`TabBar.Mascot`), `MusicPlaying` event from
media, strings EN/UK, `CharacterTests`. Perf: idle 0.26 % of one core (default), 1.2 % with the pill mascot on.
(M13 committed before: agents integration, pipe cancel race fixed.)

## Next step
M15 Polish & release (§8.15): settings panel inside the island (gear → overlay: theme, scale, language, monitor,
auto-hide, click-through, excluded apps, per-module toggles, module options incl. weather/AI/voice/agents/characters
gallery, hook install/remove link, hotkey table with rebinding), Ukrainian strings review, snapshots of every state in
both themes/languages, full perf measurement, 10-min soak test, README.md, VERIFICATION.md, ARCHITECTURE.md, build.ps1
run + launch the published exe. Then DONE.flag (+ final message in Ukrainian).
Notes: publish ships Whisper `runtimes\win-arm64` and `win-x86` too (~3.5 MB) — trim if easy; check the self-contained
build loads whisper.dll. `IslandWindow.SettingsRequested` has no handler yet (tray "Settings" too?).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
