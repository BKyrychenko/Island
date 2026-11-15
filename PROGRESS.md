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
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M11 done. Part 3: text actions (Win+Alt+A → `ClipboardOps.CopySelectionAsync` → `AiPopupView` with Summarise /
→ EN·UK·NL / Fix / Explain / Reply, Copy / Replace selection) and command bar `? question` (streamed short answer,
Open in chat). Clipboard paste code shared via `Core/ClipboardOps` (+ quiet window for the history). Live-tested with a
local fake SSE server (`..\scratch\live-m11.ps1`, `live-m11b.ps1`): selection → stream → replace, clipboard restored,
history untouched; `?` → popup → Open in chat → follow-up with history. Perf measured (DECISIONS). 442 tests green.

## Next step
M12 Voice to text (§4.16): hold-to-talk hotkey `voice` (Win+Alt+V, `HotkeyService.Register(..., released)`), NAudio
WASAPI capture 16 kHz mono with a level/waveform pill, Whisper.net (+ runtime) loaded on demand and unloaded after 2 min
idle, model download (ggml `base` multilingual from the official Hugging Face whisper.cpp repo) into
`%AppData%\Isle\models\` with progress + SHA check if published, paste via `ClipboardOps.PasteTextAsync` into the
previous window. Clear messages for no mic / no model. Tests with fakes; record peak RAM while transcribing.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
