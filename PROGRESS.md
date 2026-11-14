# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
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
M1 complete: hover logic, spring expand/collapse, tab bar (+more overlay, gear), toast, Styles/Theme.xaml + Icons.xaml,
Dark/Light/System themes, UI scale, edge auto-hide, click-through, fullscreen auto-hide, excluded apps, HotkeyService
(Win+Alt+Space toggle), pill priority queue, `--snapshot` tool (all states × themes × languages looked at in logs/shots/m1).
31 tests green. Live: hover expand/collapse verified by script screenshots; idle 38.7 MB WS, 0.009 % CPU; start 457 ms.

## Next step
Start M2 (Music + lyrics): `Modules/Media` — GSMTC session manager (event-driven), pill item (art + equalizer, title for 2 s
on track change), Music tab view (art, title/artist/app, progress + seek, transport), dominant colour → `Theme.SetAccent`,
missing-art gradient, marquee. `Modules/Lyrics` — LRCLIB client (User-Agent, get → search fallback), LRC parser (tests),
disk cache, lyrics view with current-line highlight. Register tab "music" (Order 10) and snapshot states for it.
Live helper used for M1 checks: `..\scratch\live-m1.ps1` (outside the repo; launches the exe, moves the cursor, screenshots, measures CPU/RAM).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
