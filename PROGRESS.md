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
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M12 done: `Modules/Voice` (VoiceModule hold-to-talk Win+Alt+V, MicCapture NAudio WASAPI → 16 kHz, WhisperEngine
Whisper.net 1.9.1 with 2-min idle unload, ModelDownloader with SHA-256, LanguagePick, TranscriptText), `Views/Voice/VoicePill`,
snapshots `voice-*`, strings EN/UK, `VoiceTests`. Debug CLI `--transcribe <wav> <out.txt> [--lang] [--model] [--unload]`.
Live: real model download + verified, exact English transcription (cold 2.1 s / warm 1–2 s, peak ~400 MB), scripted hold
in a WinForms target (pill, real mic, silence message). Published single-file exe transcribes too. Idle unload verified (400 → 9 MB).

## Next step
M13 Agents integration (§4.14): read https://code.claude.com/docs/en/hooks first (exact field names), `src/Isle.Hook`
named-pipe client (fail open, 60 s timeout), pipe server + Agents tab cards with Allow/Deny and option buttons, pill dot,
install/remove hooks with backup + merge (`~/.claude/settings.json`), Codex `notify` (read its docs). Tests for pipe
protocol and hook JSON with fakes.
Note for M15: publish ships Whisper `runtimes\win-arm64` and `win-x86` too (~3.5 MB) — trim if easy; check the
self-contained build loads whisper.dll.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
