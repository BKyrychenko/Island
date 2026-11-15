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
- [~] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M11 part 2: `AiModule` (registered as "ai") + `Views/AiChat/AiChatView` (chat list overlay, new/delete chat, per-chat
system prompt, streaming bubbles with Stop + Copy, Markdown renderer with code cards, provider settings overlay with
DPAPI key + Test connection, attachments: picker + Ctrl+V image, PDF text via PdfPig, docx/xlsx via Open XML zip).
11 `ai-*` snapshot states checked (dark/light, en/uk). 436 tests green. Module starts live (13 modules).

## Next step
M11 part 3: text actions (hotkey `textActions` Win+Alt+A: save clipboard, SendInput Ctrl+C, read selection, restore
clipboard; popup overlay in the island with Summarise / Translate EN·UK·NL / Fix grammar / Explain / Reply; streamed
result via `AiModule.StreamInto`; Copy / Replace selection = clipboard + Ctrl+V into `PasteTarget`, restore clipboard —
reuse the clipboard module's paste helper if it is reusable) and the command bar `?` prefix (subscribe `AskAi`, stream a
short answer inline in the command bar, "Open in chat" button). Then snapshots, perf measurement (§6, incl. peak RAM
while streaming with a fake local SSE server), VERIFICATION rows, docs, commit, mark M11 done.
Never run real API calls (owner's money) — use fakes; real call = manual-needed in VERIFICATION.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
