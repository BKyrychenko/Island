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
- [~] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M9 parts 1–2: `Modules/Tools` + `Views/Tools` — frozen-capture selection overlay (one window per monitor, physical px
via stretched Viewbox; drag / click window / Enter = monitor), OCR (Windows.Media.Ocr, language chips), QR (ZXing.Net),
colour picker with loupe + history, screenshot markup window (pen, highlighter, arrow, rect, ellipse, text, pixelate,
undo/redo, colours, widths; copy / save to Pictures\Isle / copy path), Tools tab, hotkeys Win+Alt+S/Q/C (R is taken by
Xbox Game Bar on this PC — registration fails gracefully). Verified live: OCR exact, picker exact, markup UI. 354 tests.

## Next step
M9 part 3: screen recording (spec 4.11 last bullet). Plan: try NuGet ScreenRecorderLib (MF-based, WGC/DXGI capture,
HW H.264, system audio + mic mixing, region) — if it loads on .NET 10 x64 use it and justify in DECISIONS; else
Windows.Graphics.Capture + MediaStreamSource/MediaTranscoder. 30 fps, 1080p cap, save to Videos\Isle, REC pill with
elapsed time (PillPriority.Recording), Tools button "Record"/"Stop", hotkey Win+Alt+R (taken here → document).
Then docs (DECISIONS/VERIFICATION) and commit M9; measure peak RAM during OCR and recording.
Live scripts: ..\scratch\live-m9.ps1 (+ tools-target.ps1) aborts if its test window is missing — keep it that way.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
