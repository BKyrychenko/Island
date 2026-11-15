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
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M10 complete: Weather module (Open-Meteo forecast + geocoding, optional geojs.io IP location, 30-min refresh only once
a place is set, weather.json offline cache, temperature in idle pill, once-per-event rain banner, original Wx.* icons).
Snapshots weather-* checked in dark/light, en/uk; 384 tests green; live API probe OK.

## Next step
Start M11 (AI chat + text actions, spec 4.13): BYOK Anthropic / OpenAI-compatible, DPAPI keys, SSE streaming, multiple
chats, markdown rendering, attachments (images, PDF, docx/xlsx via Open XML, text), text-actions hotkey Win+Alt+A,
`?` prefix in the command bar. Read the current Anthropic Messages API docs before writing the client. Measure perf (§6).
Live scripts must guard focus and never act on the owner's real windows (see memory + ..\scratch\live-m9.ps1 abort).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
