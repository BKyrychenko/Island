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
- [ ] M9 Screen tools
- [ ] M10 Weather
- [ ] M11 AI chat + text actions (+ perf measurement)
- [ ] M12 Voice to text
- [ ] M13 Agents integration
- [ ] M14 Characters
- [ ] M15 Polish & release

## Last action
M8 complete: command bar overlay (Alt+Space) with providers for apps (shell:AppsFolder + Start menu + recent +
folders, background index), calculator + units, currency (NBU, 12 h cache), timers/reminders, system commands with
confirm, web/URL/`?` AI hand-off. Fixed live: selection now follows the best result. 342 tests green.
Side effect during the buggy live run: a Chrome window with a Google search for "shutdown" was opened on the owner's
machine (left open; mention it in the final report).

## Next step
Start M9 (Screen tools, spec 4.11): read section 4.11 first. Tools tab + hotkeys Win+Alt+S screenshot (+ markup),
Win+Alt+R recording, Win+Alt+C colour picker, Win+Alt+Q QR/OCR. OCR via Windows.Media.Ocr (WinRT, no package);
QR decoding needs a decoder (ZXing.Net is the usual NuGet). Keep capture sessions lazy and disposed.
Live scripts must call Guard (Isle in front) before sending keys — see ..\scratch\live-m8b.ps1.

## Known blockers
- none

## How to check quickly
- Build: `dotnet build Isle.sln -c Release` · Test: `dotnet test Isle.sln -c Release`
- Run: `src\Isle\bin\Release\net10.0-windows10.0.19041.0\Isle.exe` (quit from the tray icon menu)
