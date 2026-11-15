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
- [x] M11 AI chat + text actions (+ perf measurement)
- [x] M12 Voice to text
- [x] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M13 done: `src/Isle.Hook` (pipe client, fail open), `Modules/Agents` (AgentPipeServer, AgentBook, AgentsModule, HookInstaller,
AgentText, AgentSnapshots), `Views/Agents` (AgentsView, AgentPill), strings EN/UK, `AgentTests` (46). Fixed: first pipe
instance created synchronously (flaky tests), hook-timeout cancel race that left stale waiting cards (found live).
Live: real Isle.Hook.exe ↔ Isle, Allow via UI Automation reached the hook; timeout fails open and clears the card.

## Next step
M14 Characters (§4.15): 3 original geometric creatures in XAML (invented names), idle breathing/blink, eyes follow
cursor, dance to music (playback state; loudness only if cheap), sleep after N min idle (`GetLastInputInfo`), happy on
`TimerFinished` (EventBus), small in idle pill (left slot) + larger on the panel, settings gallery, ≤30 fps, paused when hidden.
Note for M15: publish ships Whisper `runtimes\win-arm64` and `win-x86` too (~3.5 MB) — trim if easy; check the
self-contained build loads whisper.dll. Agents settings (timeout, approvals) go into the settings panel.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
