# Isle — fix plan from the two audits

Two read-only audits were run against this repository and are in `docs/`:
- `SOFTWARE-AUDIT.md` — correctness, resource use, security, efficiency (26 findings, with file:line, failure scenario and an implementable fix for each)
- `UI-AUDIT.md` — interface and interaction (17 findings, same format; contrast numbers computed against WCAG 2.2)

Read both in full before changing anything. They contain the reasoning; this file only sets the order and the rules.

## How to work on this

Same regime as `SPEC.md` §0, with one change: **this is a repair pass, not a feature pass. Add no features.**

- Work tier by tier, in the order below. Within a tier, order is yours.
- After each fix: build, run the relevant tests, update `PROGRESS.md` ("last action" / "next step"), commit with the finding id in the message (e.g. `fix(SW-C2): random pipe name + FIRST_PIPE_INSTANCE`).
- Ask nothing. Decisions go in `DECISIONS.md`, one line each.
- Where an audit marks a finding `UNVERIFIED — needs a run on Windows`, run the stated check first. If it does not reproduce, write that in `VERIFICATION.md` and skip the fix; do not "fix" a problem you could not observe.
- Several findings are a behaviour change the owner will feel (hover timing, hotkey defaults, accent colours). Keep each behind a setting where the audit suggests one, and default to the safer value.
- Add a regression test wherever the fix is in testable logic (parsers, stores, pipe protocol, contrast computation). UI-only fixes get a `--snapshot` state instead.
- Re-run the full idle measurement from `SPEC.md` §2 after Tier 2 and record real numbers in `DECISIONS.md`. The previously recorded 0.29 % / 25 MB is not trustworthy: `SOFTWARE-AUDIT.md` shows it was taken with polls that stop only in some states, and with `EmptyWorkingSet` inflating the memory figure.
- Delete nothing from the audits. When a finding is done, append `— FIXED <commit sha>` to its heading in the audit file.

Stop and create `FIXES-DONE.flag` only when every Tier 1 and Tier 2 item is either fixed or explicitly dismissed in `VERIFICATION.md` with a reason.

## Tier 1 — do first (security and system-wide effects)

1. **SW-C2 Agent pipe is squattable.** Random pipe name in a user-only file, `FILE_FLAG_FIRST_PIPE_INSTANCE` on the server, server-process verification in the hook client, validate `o["v"]`. Until this is done, any same-user process can auto-approve the owner's Claude Code commands.
2. **SW-C3 / UI-H2 The approval card truncates the command being approved.** Both audits raised this independently. Show the full command (scrollable, wrapping, monospace), raise `MaxDetail`, stop making Allow the default/Enter button, and never summarise an unknown tool by "first non-empty property".
3. **SW-C1 `WH_KEYBOARD_LL` on the UI thread, on by default.** Move it to its own thread with its own message loop, or delete the Caps Lock source. The same thread also runs the blocking GC, PNG encoding and synchronous JSON writes — those must come off the hook path regardless.
4. **UI-C1 Bare `Esc` registered as a global hotkey while expanded.** Remove the registration; handle Esc in-window only when focused. While it is registered, Esc does not reach dialogs, menus or editors anywhere on the machine.

## Tier 2 — daily use (annoyance, data loss, battery)

5. **UI-C2 Hover re-expands under a resting cursor.** Reset `_insideSince` on collapse, require leave-and-re-enter, ignore hover while a mouse button is down, require real cursor motion.
6. **SW-H1/H2 Runaway polls.** Downloads sampler stop condition, OBS process enumeration (and its undisposed results, and backoff), 500 ms layout timer, 2 s fullscreen check re-enumerating monitors.
7. **SW-H3 `Memory.TrimSoon` on every collapse.** Delete it, or defer to ~60 s idle.
8. **SW `JsonStore.WriteAtomic` never flushes.** Power loss can produce an empty `notes.json`, which the recovery path then replaces with an empty store — silent total note loss. Flush before rename, and stop ignoring `File.Move` failure.
9. **SW `MediaSession.SameSession` compares by app id.** Restarting Spotify leaves the pill bound to a dead session; music display and transport controls freeze.
10. **UI-H3 Timer and reminder banners are invisible under fullscreen.** Let an active banner break through the fullscreen hide, and only trust the machine-wide notification state when the foreground window is on the island's monitor.
11. **UI-H1 Clipboard paste-back is lossy and unchecked.** Snapshot all formats, add the three history-exclude formats to the temporary payload, verify the target is still foreground before sending Ctrl+V, and fall back to "copied — press Ctrl+V".
12. **SW Two blocking `Dispatcher.Invoke` calls from the OBS socket loop**, and `rasdial.exe` / `shutdown.exe` launched by bare name (use full System32 paths).

## Tier 3 — quality, after Tier 1 and 2 are committed

13. **UI-C3 Contrast.** Per-accent foreground (white or near-black, whichever wins), separate accent fill from accent text, `FgFaint` alpha, light-theme Switch and Warn colours, light-theme pill outline. The audit gives exact values.
14. **UI-H4 Hotkey defaults.** Alt+Space takes the system window menu from every app and collides with PowerToys Run. Default to a free chord, make Alt+Space opt-in, and surface registration failures instead of only logging them.
15. **UI-H5 Keyboard and accessibility.** Focusable buttons with a keyboard-only focus ring, tab navigation, real automation names, honour high-contrast themes and the system reduce-motion setting.
16. **UI-M5 Command bar destructive commands.** "empty" + double Enter permanently empties the Recycle Bin in under 200 ms with no prompt; shutdown and restart run with zero delay.
17. **UI-M1/M2/M3/M4** stage size and top-centre collisions, monitor identified by index instead of device name, fixed widths versus Ukrainian strings, tab overflow hiding AI and Agents.
18. **SW Dependency weight.** Replace the 11 MB `Anthropic` package with ~150 lines over the existing SSE client. Reconsider `System.Management` (pulled in for one brightness slider). Set the publish options the csproj currently lacks.
