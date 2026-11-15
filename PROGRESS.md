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
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M9 complete: selection overlay, screenshot markup, OCR, QR, colour picker, and screen recording (WGC → D3D11 →
MediaTranscoder MP4 + NAudio mixed audio; software-processing fallback on this PC; memory released after stop).
ScreenRecorderLib rejected (.NET Framework C++/CLI). Verified live; 354 tests green. Peak RAM recorded in DECISIONS.

## Next step
Start M10 (Weather, spec 4.12): read section 4.12. Likely Open-Meteo (free, no key) with cities from options, cache,
refresh only while needed (≥ 1 s polling rule; e.g. 30 min while enabled), optional temperature next to the idle pill,
rain alert banner. Network only when the weather module is enabled (`Core/IdleHttp`). Tests for the parser/cache with
a fake source.
Live scripts must guard focus and never act on the owner's real windows (see memory + ..\scratch\live-m9.ps1 abort).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
