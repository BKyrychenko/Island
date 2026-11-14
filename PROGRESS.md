# PROGRESS

Legend: `[ ]` todo · `[~]` in progress · `[x]` done (built, checked, committed)

## Milestones
- [x] M0 Skeleton
- [x] M1 Expand/collapse & shell (+ perf measurement)
- [x] M2 Music + lyrics
- [x] M3 Timers & reminders
- [x] M4 Notes
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
M4 complete: Notes module (store with folders/pins/search, rich-text editor with B/I/U, bullets, numbers, checkbox items,
2 heading sizes, autosave, delete + Undo toast, quick capture Win+Alt+N). Fixed text-box focus for the non-activating
island (drop WS_EX_NOACTIVATE while editing; focus after the panel is visible) — verified live with typing + UIA.
173 tests green. Snapshots in logs/shots/m4.

## Next step
Start M5 (Control Center): `Modules/ControlCenter` — tiles: Wi-Fi + Bluetooth radios (Windows.Devices.Radios, degrade if
denied), dark theme toggle (HKCU Personalize AppsUseLightTheme + SystemUsesLightTheme, broadcast WM_SETTINGCHANGE
"ImmersiveColorSet"), master volume + mute (Core Audio IAudioEndpointVolume via COM interop, reflect external changes via
IAudioEndpointVolumeCallback), brightness (WMI WmiMonitorBrightness/Methods, hide if unsupported); Bluetooth paired devices
list + connect/disconnect audio (try AudioPlaybackConnection / BluetoothDevice; record what works in DECISIONS.md); VPN list
(Get-VpnConnection / rasdial) + client detection (WireGuard, OpenVPN, Tailscale, Proton, WARP, Nord, Mullvad) with CLI
connect/disconnect where available; refresh on open and on radio/network/audio events. Tab "control" Order 60.
Live helpers (outside the repo, `..\scratch`): live-m1..m4.ps1, fake-session.ps1, measure-play.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
