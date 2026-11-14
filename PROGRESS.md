# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [~] M3 Timers & reminders — logic done (DurationParser, TimerBook, ReminderParser EN+UK, RepeatRule, ReminderBook; 165 tests); UI next
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
M2 complete: Media module (GSMTC, pill art + equalizer, track banner, Music tab with transport/seek/progress, art accent,
missing-art gradient, marquee) and Lyrics module (LRCLIB client, LRC parser, disk cache, lyrics view, optional lyric line
in the pill). 69 tests green. Verified live with a real (silent) SMTC session: banner, pill, tab, synced lyric in pill.
Snapshots in logs/shots/m2 (all states × themes × languages).

## Next step
Start M3 (Timers & reminders): `Modules/Timers` — timer model with absolute end timestamps + injectable clock (tests),
duration parser (`90`, `1:30`, `1h30m`, `45s`, `2m 30s`), presets 5/10/15/25/45, labels, pause/resume/cancel, stopwatch,
persistence (timers.json), pill item (soonest timer + progress ring), finished state (pulse ×3, system sound, Done / +1 min).
`Modules/Reminders` — NL parser EN+UK (≥40 phrases tested, table-driven), repeat rules, reminders.json, banner with
Done / Snooze (5 min, 1 h, tomorrow), missed reminders fire at start, list/edit/delete in the tab. Tab "timers" Order 20.
Live helpers (outside the repo, in `..\scratch`): `live-m1.ps1` (hover + idle perf), `live-m2.ps1` + `fake-session.ps1`
(silent media session), `measure-play.ps1`.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
