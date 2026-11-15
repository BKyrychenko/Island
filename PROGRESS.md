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
M6 complete: live module with downloads, mic/camera, OBS, Steam, VPN, layout, Caps Lock sources (notifications
unavailable: no package identity, 0x80070490), PillSink flashes, DPAPI helper, live snapshots. Verified live: download
pill + finished flash, mic indicator via a temporary consent entry, Caps Lock. Idle with everything on: 55.5 MB WS,
0.29 % of one core. Snapshots in logs/shots/m6.

## Next step
Start M7 (Clipboard + File shelf): `Modules/Clipboard` — AddClipboardFormatListener on a message-only HWND, store
(last 200, pins, dedupe, images as PNG in %AppData%\Isle\clip, text DPAPI via `Core/Secret.ProtectBytes`), privacy
formats (ExcludeClipboardContentFromMonitorProcessing, CanIncludeInClipboardHistory=0) + excluded apps, paste into the
previous window (`ForegroundWatcher.LastExternal`, SendInput Ctrl+V, restore clipboard); tests for the store. Then
`Modules/Shelf` (drag in → expand on drag-over, DoDragDrop out, shelf.json, missing files, copy path/open, clear).
Live helpers (outside the repo, `..\scratch`): live-m1..m6.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
