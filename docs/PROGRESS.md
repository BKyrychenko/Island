# PROGRESS

## WPF audit repairs

- Latest: Tier 2 code integrated: hover intent, background polling, durable saves, media rebinding, fullscreen timer alerts, clipboard borrowing, and system tool paths. All 549 tests passed on Windows. The desktop probe verified Escape and held-button hover.
- Next: Verify fullscreen alerts and idle usage on Windows. Tier 3 covers contrast, keyboard access, command confirmation, layout, and dependency weight.

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
- [x] M15 Polish & release

## Last action
M15 done: settings panel in the island (gear + tray), Ukrainian review, all 104 snapshot states × themes × languages
inspected, `build.ps1` (tests green, both variants in `dist\`, Whisper works from both after keeping natives loose),
published exes launched (pill shown, 365–527 ms start-up), idle perf 0.29 % of one core / 25 MB, 10-min soak clean
(77 cycles, no errors, memory steady; trim after collapse added), README / ARCHITECTURE / VERIFICATION / DECISIONS
updated, graph updated. Definition of Done verified → DONE.flag created.

## Next step
Nothing left in SPEC.md. Owner: see VERIFICATION.md `manual-needed` rows (2-minute checks).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
