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
M11 part 1: AI core in `Modules/AiChat` — `AiModels` (Conversation/ChatMessage/ChatAttachment, `AiConfig` from settings
with DPAPI keys, `AiException` readable error keys), `AnthropicProvider` (official SDK, beta endpoint + refusal fallbacks),
`OpenAiProvider` (raw SSE), `ChatStore` (chats.json). `ai.err.*` strings EN/UK. 412 tests green (incl. SDK stream via fake HTTP).

## Next step
M11 part 2: `AiModule` + `Views/AiChat/AiChatView` (chat list, new/delete chat, per-chat system prompt, streaming
bubbles with Stop + Copy, simple markdown renderer: code blocks, lists, bold; provider settings panel with key fields +
"Test connection"; attachments with size limits — PDF text via a PDF library for OpenAI, docx/xlsx via Open XML).
Then part 3: text actions (Win+Alt+A, Ctrl+C capture/restore, popup with Summarise/Translate/Fix/Explain/Reply, Copy /
Replace) and the command bar `?` prefix (stream a short answer inline). Snapshots, perf measurement (§6), docs.
Never run real API calls (owner's money) — use fakes; real call = manual-needed in VERIFICATION.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
