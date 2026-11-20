# PROGRESS

## WPF audit repairs

- 2026-11-20: Follow-up repairs cover Notes recovery and editor selection, export and storage failures, hold-to-talk release, command and geocoder ordering, reminders, lyrics retry and chat drafts. The interface retains keyboard focus in More and Settings, keeps approval actions visible, enlarges the scale and seek targets, restores one-row Notes formatting, and reflects when Send is available.
- 2026-11-20: Codex streams through the local signed-in CLI. A live request produced 225 text updates, with its first fragment arriving 3.1 seconds before completion. The 80% layout passed 635 Release tests and 436 snapshot renders across English, Ukrainian, Dark and Light.
- 2026-11-20: The remaining audit pass covered drafts and chat history, hook ownership and approval review, clipboard capture and paging, timer persistence and file index updates, Settings controls and focus, Bluetooth and VPN, screen selection, voice cleanup, player seek, Steam activity, and weather error recovery. The built-in Codex flow shows a waiting message while the CLI handles a request. All 628 Release tests passed. The self-contained package was installed at the existing `dist/Isle` path and opened its window with all 16 modules registered. The signed-in Claude check was skipped by request.
- 2026-11-20: Follow-up fixes preserve the clicked command while results arrive and correct calculator argument and percentage parsing. Snooze preserves recurring schedules, keyboard users can navigate Settings and the Shelf, transport stays visible with lyrics open, and disabling IP location cancels stale weather requests. All 587 Release tests passed.
- 2026-11-20: The first priority audit batch routes Alt+F4 and tray Quit through recording finalization, reports save failures, gates AI Replace on a complete response for the full selection, and bundles a standalone agent hook in the self-contained package.
- 2026-11-20: WPF AI now uses the installed Codex or Claude Code CLI with the user's local sign-in. Removed direct API providers and key entry. All 558 Release tests passed. Codex answered a live test using chat context. Verified the signed Claude CLI's signed-out path and the full chat flow with a test executable.
- 2026-11-19: AI chat can use the local Codex CLI's ChatGPT sign-in. Office attachment parsing has size and worksheet bounds, and custom AI endpoints require HTTPS outside loopback. The excluded-app setting can pick an open app and optionally check whether it overlaps the capsule.
- 2026-11-19: Agent cards can focus the session's console or Windows Terminal tab, show reported subagent completion and activity, and preview the latest reported file edit. Verified selection between identically titled tabs, packaged hook delivery, and the live WPF release with the updated Codex hooks.
- Tier 1–3 repairs passed 556 Release tests. Windows checks verified keyboard ownership, drag hover, fullscreen timer banners, and a 60-second idle sample.
- Outstanding live checks include media-player restart, very slow clipboard targets, physical mixed-DPI docking, and a signed-in Claude Code session.

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
- [x] M14 Characters
- [x] M15 Polish & release

## Last action
M15 done: settings panel in the island (gear + tray), Ukrainian review, all 104 snapshot states × themes × languages
inspected, `build.ps1` (tests green, both variants in `dist\`, Whisper works from both after keeping natives loose),
published exes launched (pill shown, 365–527 ms start-up), idle perf 0.29 % of one core / 25 MB, 10-min soak clean
(77 cycles, no errors, memory steady; trim after collapse added), README / ARCHITECTURE / VERIFICATION / DECISIONS
updated, graph updated. Definition of Done verified → DONE.flag created.

## Next step
Nothing left in SPEC.md. Owner: see VERIFICATION.md `manual-needed` rows (2-minute checks).

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
