# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [x] M3 Timers & reminders
- [x] M4 Notes
- [x] M5 Control Center
- [~] M6 Live events + pill priority queue (+ perf measurement)
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
M6 part 1: `Modules/LiveEvents` — sources for downloads, mic/camera (RegNotifyChangeKeyValue), OBS v5, Steam, VPN flash,
keyboard layout, Caps Lock (WH_KEYBOARD_LL), Windows notifications (UserNotificationListener attempt); `LiveEventsModule`
("live", per-source options `live.<name>`), `PillSink` (flash items), `Core/Secret` (DPAPI). 223 tests green.

## Next step
M6 part 2: run live — check the notifications source result in log.txt (expected unavailable: no package identity) and
record it in VERIFICATION.md; drive a fake download into Downloads, Caps Lock, layout switch; snapshots of the live pills
(add LiveSnapshots like ControlSnapshots); then the 60 s idle perf measurement with all modules → DECISIONS.md; commit M6.
Live helpers (outside the repo, `..\scratch`): live-m1..m5.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
