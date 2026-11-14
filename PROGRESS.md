# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [x] M3 Timers & reminders
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
M3 complete: timers (input + presets + labels, pause/resume/cancel, +1 min, stopwatch, pill ring, finished banner with
sound + pulse) and reminders (EN/UK parser, repeats, banner with Done/Snooze, list/edit/delete, missed at start).
165 tests green. Live-verified with seeded timers.json/reminders.json: pill, done banner, reminder banner, real click on Done.
Snapshots in logs/shots/m3.

## Next step
Start M4 (Notes): `Modules/Notes` — NotesStore (notes.json: id, title, folder, pinned, XAML FlowDocument text + plain
shadow, created/updated; atomic save; corruption recovery; tests: CRUD, search, folders, pins), Notes tab: list with search
+ folder dropdown + pinned first, RichTextBox editor (Ctrl+B/I/U, bullets, numbered, checkbox items, 2 heading sizes),
autosave debounced 500 ms, delete with 5-s undo toast (`IslandWindow.ShowToast`), quick-capture hotkey Win+Alt+N (new
note, caret focused via `AcquireFocus`). Tab "notes" Order 30.
Live helpers (outside the repo, `..\scratch`): live-m1/m2/m3.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
