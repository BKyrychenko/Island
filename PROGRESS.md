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
- [~] M7 Clipboard + File shelf
- [ ] M8 Command bar
- [ ] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M7 part 1 (clipboard): `Modules/Clipboard` — ClipStore (dedupe, pins, limit, DPAPI text, PNG images, 14 tests),
ClipboardService (message-only HWND listener, privacy formats + excluded apps, paste via SendInput + clipboard restore),
ClipboardView tab. Verified live: capture text+image, private item skipped, paste into a test window, restore.

## Next step
M7 part 2: `Modules/Shelf` — drag files onto the island (expand on drag-over; IslandWindow needs AllowDrop on the pill
shape + DragEnter → Expand("shelf")), items with shell icon/thumbnail + name, drag out with DoDragDrop(FileDrop),
option "remove after drop", shelf.json, missing-file mark, open/copy path/remove/clear; ShelfStore tests; snapshots for
clipboard + shelf (both themes/langs); commit M7.
Live helpers (outside the repo, `..\scratch`): live-m1..m7.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
