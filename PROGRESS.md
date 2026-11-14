# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [x] M3 Timers & reminders
- [x] M4 Notes
- [x] M5 Control Center
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
M5 complete: Control tab with Wi-Fi/Bluetooth radios, Bluetooth device list + KS reconnect/disconnect, Windows dark mode,
VPN detection (pbk + clients) with CLI toggles, master volume + mute (Core Audio COM, event-driven), WMI brightness.
Verified live on this laptop. Also fixed: fullscreen detection hid the island behind maximized browsers; remembered tab
restore. 177 tests green. Snapshots in logs/shots/m5.

## Next step
Start M6 (Live events + pill priority queue, then perf measurement): `Modules/LiveEvents` — one small class per source,
each with a fake for tests, started only when enabled: Downloads (FileSystemWatcher on Downloads for *.crdownload / *.part /
*.tmp, size growth + speed; Chrome partial-download detector tested), mic/camera in use (CapabilityAccessManager
ConsentStore registry, incl. NonPackaged; RegNotifyChangeKeyValue, not polling), Windows notifications
(UserNotificationListener — likely needs package identity: try, otherwise mark unavailable), OBS WebSocket v5 (recording
state + elapsed), Steam (libraryfolders.vdf + appmanifest_*.acf parser tested; watch manifests), VPN transient indicator
(ControlModule.NetworkChanged + VpnDetector), keyboard layout (EVENT_SYSTEM_FOREGROUND + 500 ms check while on),
Caps Lock (WH_KEYBOARD_LL, tiny callback). Pill items use PillPriority constants. Then measure idle CPU/RAM over 60 s with
all modules enabled and record in DECISIONS.md (idle WS was ~21–106 MB right after start before trim — check trim timing).
Live helpers (outside the repo, `..\scratch`): live-m1..m5.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
