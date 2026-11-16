# Isle UI/UX audit

Scope: `/mnt/user-data/uploads/island/src/Isle` (C# 14 / .NET 10 / WPF, Windows 11). Read-only code audit from Linux; nothing was built or run. Anything that depends on runtime behaviour is marked `UNVERIFIED - needs a run on Windows`. The `Strings` EN/UK dictionaries are not in the upload, so Ukrainian string lengths could not be measured; the localisation findings reason from fixed widths in code only.

## Summary

Isle's engineering is careful in the places where such apps usually fail: NOACTIVATE/`MA_NOACTIVATE` handling, UIPI-aware clipboard borrowing, privacy checks on capture, a fail-open hook helper, `IsWebUrl` guarding on QR links, and a backup-and-merge hook installer. The problems are in the seams between Isle and the rest of Windows, and in a visual system that assumes a dark pill with a blue-ish accent.

The three things that matter most:

1. **A bare `Esc` is registered as a global hotkey while the island is expanded** (`IslandWindow.Hover.cs:124`). While it is registered, every Esc press on the machine goes to Isle, not to the dialog, menu or editor in front of you. The hover logic also re-expands the island under a resting cursor and ignores mouse buttons, so this state is easy to enter by accident.
2. **White-on-accent text is hard-wired** (`Theme.cs:70`) while the accent is taken from album art and clamped only on lightness. Contrast is 2.46:1 for the default dark accent, 1.07:1 at worst, and `FgFaint` text fails everywhere (3.0:1 dark, 2.27:1 light).
3. **Clipboard borrow/paste-back is lossy and unchecked.** It drops every format except text/image/file-drop, does not exclude the temporary text from Win+V history, and sends Ctrl+V without confirming the target is still the foreground window.

Also worth knowing: the timer/reminder banner is invisible when a fullscreen app is in front, and Alt+Space (default command bar) takes the system window menu away from every app.

Counts: 3 Critical, 5 High, 6 Medium, 3 Low. Handled well: listed at the end.

---

## Critical

### C1. Global bare-Esc hotkey while expanded
- **Where:** `src/Isle/IslandWindow.Hover.cs:124` (`Native.RegisterHotKey(_hwnd, EscHotkeyId, 0, 0x1B)`), unregister at `:143`. Contrast the comment at `SettingsView.cs:556` ("a bare/shifted key would swallow normal typing"): the author already knows the consequence, and the app does it to itself.
- **Scenario:** Hover the pill (it expands after 120 ms) and move on, or let it open from a pill event. While `_expanded` is true, Esc closes the Windows Start menu, a file dialog, a Chrome find bar, a Vim insert mode, an IDE popup, a combo-box dropdown: none of them receive it. The island collapses instead, and only then does Esc work again. Because `Collapse` is gated by the 250 ms leave timer and `_sticky`, the window of stolen Esc can last as long as the cursor stays near the top edge, and for sticky/held-open views (AI input, file dialog at `AiChatView.cs:315` with `HoldOpen`) it lasts as long as the view is open.
- **Why it matters:** Esc is the key people hit reflexively. A global hotkey is consumed system-wide (`RegisterHotKey` posts WM_HOTKEY to the registering thread instead of delivering the key to the focused window; the behaviour is documented at https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey). The failure mode "Esc sometimes does nothing" is a classic daily irritation that is very hard to attribute. UNVERIFIED - needs a run on Windows: open the island by hover, then press Esc inside Notepad's Find dialog.
- **Fix:**
  1. Delete the hotkey registration. Handle Esc through normal `KeyDown` only when `_hasFocus` is true (the island already owns focus in that case via `AcquireFocus`).
  2. For the non-focused hover-expanded state, offer click-outside or leave-timer collapse only; do not capture keys you do not own.
  3. If you want a "dismiss from anywhere" key, make it a user-configurable chord in the Hotkeys settings (for example Win+Alt+Esc), never bare.

### C2. Hover logic re-expands under a resting cursor and ignores mouse buttons and drags
- **Where:** `IslandWindow.Hover.cs:59-86` (`OnHoverTick`), `:101-108` (`IsCursorInside`: margin 3 collapsed, 10 expanded, `Y >= -4`), `:15` constants. `_insideSince` is never reset on collapse. There is no `GetAsyncKeyState(VK_LBUTTON)` check anywhere in the file.
- **Scenarios:**
  - Press Esc/click the close affordance to collapse while the cursor still sits in the pill rectangle: 120 ms later the island expands again. You cannot dismiss it without moving the mouse away first.
  - Drag a window to the top of the screen to trigger Windows 11's snap bar (https://support.microsoft.com/en-us/windows/snap-your-windows-885a9b1e-a983-a3b1-16cd-c531795e6241): the cursor travels through top-centre, the pill treats it as hover intent and expands mid-drag, over the thing you are dragging onto. The same applies to dragging a browser tab out of the tab strip, rubber-band selecting in a canvas app, or moving the scrollbar of a maximised window.
  - Clicking a Chrome/VS Code tab or title-bar item at top-centre and lingering 120 ms expands the pill over it; `Y >= -4` also counts a cursor pushed against the screen edge.
- **Why it matters:** this is the single most-repeated interaction. False expansions are loud (shadow, spring, tab view appearing over content) and the user will perceive Isle as "twitchy".
- **Fix:**
  - On `Collapse`, set `_insideSince = null` and add `_needsLeave = true`; clear `_needsLeave` only after the cursor has been outside the rect for 150 ms. Require `_needsLeave == false` to start intent.
  - Skip intent while either mouse button is down (`GetAsyncKeyState(VK_LBUTTON/VK_RBUTTON) & 0x8000`) unless `_expanded` or a drop is in progress (Drop.cs path handles its own expansion).
  - Require actual cursor motion in the pill rect (>= 2 px between ticks within the intent window) so a cursor merely parked there does not count. Raise `HoverIntentMs` from 120 to 150 when the foreground window is maximised or fullscreen-like, and keep 120 otherwise.
  - Poll interval: 30 ms is fine. Consider stopping the timer entirely when `HideInFullscreen` has hidden the window (the code already stops it in `SetHidden`).
- UNVERIFIED - needs a run on Windows: drag a window by its title bar across the pill with the island collapsed.

### C3. Contrast system: white on accent, faint text, light-theme controls
- **Where:** `Core/Theme.cs:70` (`AccentFgBrush` is always white); `Theme.cs:53-66` (palette); `Modules/Media/ArtColors.cs:54-58` (`Readable`: saturation >= 0.45, lightness clamped 0.45-0.65); `Styles/Theme.xaml:75-79` (AccentButton), `:181-184` (checked Chip uses AccentFg), `:130` (Switch thumb `Fill="White"`). Contrast measured with the WCAG 2.2 relative-luminance formula (https://www.w3.org/TR/WCAG22/#contrast-minimum): script at `/home/claude/scratch/contrast.py`.
- **Numbers:**
  - White on the default dark accent: **2.46:1**; on the default light accent **3.96:1**. Needed: 4.5:1 for button text.
  - Across hue/saturation/lightness combinations that `ArtColors.Readable` allows, white-on-accent reaches 4.5:1 in only **22%** and 3:1 in only **48%**. Yellow at l=0.65: 1.35:1; green l=0.65: 1.53:1; cyan l=0.65: 1.56:1; orange l=0.45: 3.43:1. Blues/purples at l=0.45 are fine (7.5:1).
  - The same accent used as text on the light theme background (accent-on-light >= 4.5:1) holds in only **19%** of combinations; on black it holds in 80%.
  - `FgFaint` (alpha 0.35): **3.0:1** on dark, **2.27:1** on light. It is used for placeholders (`CommandBarView.cs:54`) and clipboard timestamps (`ClipboardView.cs`). `FgDim` (0.60) is acceptable on black; recheck it on the light-theme surface tint (not computed precisely).
  - Light-theme `Warn` (`Theme.cs`) **2.91:1** on #F7F7F8; the light Switch thumb is white on a #DEDEDF off-track: **1.34:1**, and the off-track against the background is **1.26:1**, so an "off" switch is nearly invisible. The light pill itself (#F7F7F8) on a white page is **1.07:1**; the stroke outline gives only 1.35:1.
- **Why it matters:** album-art accents change per track, so the unreadable-button case is not rare: it happens whenever the cover art is yellow, green or cyan. This is the most visible visual defect in the app.
- **Fix:**
  1. `AccentFgBrush` should be computed in `SetAccent` (`Theme.cs:77-96`): pick `White` or `#111113` by whichever gives the higher contrast against the accent. With that rule, only 26 of 648 sampled combinations still fall under 4.5:1 (all are mid-tone, pick the darker lightness clamp for those).
  2. Split `Accent` into `AccentFill` (what ArtColors yields) and `AccentText` (derived by moving HSL lightness until contrast with the actual background reaches 4.5:1: lighten on dark, darken on light). Use `AccentText` for text/icons on the pill, `AccentFill` for fills.
  3. Raise `FgFaint` alpha: **0.46** in dark (4.5:1 on black), **0.59** in light. Keep the visual hierarchy with size/weight instead of lowering contrast.
  4. Light theme state colours: Warn #A85A00 (4.75:1), Ok #167A41 (5.04:1), Danger #C42B2B (5.26:1).
  5. Switch: outline the off-track with `Fg` at alpha 0.45 (1 px) and give the thumb a 1 px border in light theme; or use `Fg` at 0.60 for the light thumb.
  6. Give the light pill a 1 px outline at `Fg` alpha >= 0.30 (the 1.35:1 stroke is too weak), or a stronger shadow, so it separates from white pages.

---

## High

### H1. Clipboard borrow/paste-back loses data and pastes blindly
- **Where:** `Core/ClipboardOps.cs:22-34` (`Snapshot`: text, image, file-drop only), `:37-42` (`Restore`), `:98-119` (`CopySelectionAsync`), `:123-136` (`PasteTextAsync`: `SetForegroundWindow` then one `Task.Delay(90)`, no post-check, restore after a fixed `Task.Delay(600)` at `:133`); `Modules/Clipboard/ClipboardService.cs:156-168` (`Paste`, 600 ms restore at `:166`); `VoiceModule.cs:46-47,116`; `AiPopupView.cs` (`Replace` -> `PasteTextAsync`).
- **Scenarios:**
  - You have an Excel range, a Word RTF selection or HTML from a browser on the clipboard, run voice dictation or "Replace selection", and afterwards the clipboard holds plain text only (or nothing for custom formats). Silent data loss.
  - The temporary text is set with `Clipboard.SetText` without the privacy formats, so it appears in Win+V clipboard history and, if cloud clipboard sync is on, can upload. Dictated text and selections copied for AI replacement are exactly what you may not want there. (Capture of other apps' content already honours the exclude formats, `ClipboardService.cs:81-111`; this is the reverse direction.)
  - Target window closes or loses foreground during recognition (`_target` is captured at record start, `VoiceModule.cs:46-47`): `SetForegroundWindow` can be refused (foreground lock) and Ctrl+V goes into whatever is active, possibly a terminal or chat box. Nothing checks `IsWindow(target)` or compares `GetForegroundWindow()` after the switch.
  - Slow targets (Electron, remote sessions, browser-based editors) can take more than 600 ms to read the clipboard: restore then races the paste and you paste the OLD content.
  - Target is elevated (manifest is `asInvoker`, `uiAccess=false`): UIPI drops `SendInput` silently; the user sees nothing happen.
- **Fix:**
  - Snapshot with `OleGetClipboard`/`IDataObject` copy, or enumerate `EnumClipboardFormats` and duplicate every HGLOBAL-backed format; skip GDI-handle formats only if duplication fails. Restore by re-setting all of them in a single `OpenClipboard` session.
  - When setting the temporary text, add the registered formats `ExcludeClipboardContentFromMonitorProcessing`, `CanIncludeInClipboardHistory = 0` and `CanUploadToCloudClipboard = 0` (https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats).
  - Before Ctrl+V: `if (!IsWindow(target)) abort`; call `SetForegroundWindow`, poll up to 300 ms until `GetForegroundWindow() == target`; if it never matches, leave the text on the clipboard and show a pill message "Copied - press Ctrl+V" instead of sending keys.
  - Restore on a clipboard-sequence-number change or after 1500 ms, whichever comes first; do not use a fixed 600 ms.
  - Detect elevated target (`OpenProcess`/token elevation check) and fall back to the same "copied" message.
- UNVERIFIED - needs a run on Windows: dictate into an elevated terminal, and into Excel with a range on the clipboard.

### H2. Agent permission prompt: the command you approve can be truncated
- **Where:** `Views/Agents/AgentsView.cs:156` (permission detail `MaxHeight=34`, ellipsis), `Views/Agents/AgentText.cs:11,49` (`MaxDetail = 400`); `Modules/Agents/AgentsModule.cs:151-169` (`BuildPill`); `Views/Agents/AgentPill.cs` (no Allow/Deny in the pill).
- **Scenario:** Claude Code asks permission for a multi-line shell command. The row shows about two lines at ~34 px; a long `&&` chain, a heredoc, or a command whose dangerous part (`rm -rf`, `curl | sh`) sits after the visible part is cut. Anything beyond 400 characters is dropped entirely. You click Allow on a command you have not read.
- **Why it matters:** this is the one place in the app where the UI is a security control. The hook helper fails open (correct for availability), so the island is the only checkpoint.
- **Fix:**
  - Show the first 6 lines in a scrollable block (`MaxHeight` ~ 110 DIP at scale 1.0) for `Bash`-type tools; use a monospace font; wrap, never ellipsize.
  - If the command exceeds the visible area or 400 chars, show "+N more chars" and require expanding (or a 1 s delay on Allow) before Allow becomes enabled.
  - Raise `MaxDetail` to 2000 and still show the truncation count.
  - Do not make Allow the focused/Enter-activated button; Deny (or no default) should be the safe default.

### H3. Timer/reminder is invisible when a fullscreen app is in front
- **Where:** `IslandWindow.Hover.cs:281` (`SetHidden("fullscreen", s.HideInFullscreen && IsFullscreen(fg, Screen))`), `:244-259` (hides the window completely), `Core/ForegroundWatcher.cs:69-80`, `Settings.cs:22` (`HideInFullscreen` default **true**); `Modules/Timers/TimersModule.cs:122-160` (banners persist, `Announce` = `MessageBeep` + `Pulse(3)` only).
- **Scenarios:**
  - A timer or reminder fires while you watch a video, present, or play a game: you get a system beep and nothing else. The banner persists but the window is hidden, so you only see it after leaving fullscreen, possibly much later.
  - `SHQueryUserNotificationState` (https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shqueryusernotificationstate) is machine-wide: a game or F11 video on monitor 2 returns "busy" and hides the island on monitor 1 too (`:75`, before the per-screen rectangle check).
  - The default beep (`MB_ICONASTERISK/EXCLAMATION`) follows the Windows sound scheme and can be silent.
- **Fix:**
  - A `Timers`/reminder "done" pill should override fullscreen hiding: add a `reason`-aware bypass (`SetHidden("fullscreen", false)` while a banner is active), or at least raise a Windows toast.
  - Scope the fullscreen test to the island's monitor: only treat `SHQueryUserNotificationState` as decisive if the foreground window is on the same monitor (`MonitorFromWindow`).
  - Add a distinct sound option for timers (own wav) instead of the sound scheme beep.
- UNVERIFIED - needs a run on Windows: start a 10 s timer, switch a browser video to F11/fullscreen.

### H4. Hotkey defaults collide and failures are silent
- **Where:** `Modules/Commands/CommandModule.cs:16` (default `Alt+Space`); `App.xaml.cs:87` (toggle `Win+Alt+Space`); `Modules/Hotkeys/HotkeyService.cs` (`Bind` logs a warning on failure); UI hint only in Settings (`SettingsView.cs:254`).
- **Facts:** Alt+Space is the system window menu (Restore/Move/Size/Close) in every desktop app; registering it globally removes that menu everywhere. It is also the default activation key for PowerToys Run (https://learn.microsoft.com/en-us/windows/powertoys/powertoys-run) and for several launchers. For an EN/UK user, Win+Space and Alt+Shift/Ctrl+Shift switch keyboard layouts (https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec); Win+Alt+Space is not a documented Windows binding but sits next to Win+Space and is easy to mistype into the layout switch. Game Bar owns Win+Alt+R, Win+Alt+G etc.
- **Scenario:** Another tool already holds Alt+Space (or starts first after reboot); `RegisterHotKey` fails; Isle logs a warning nobody reads and the command bar never opens. You find out only by opening Settings.
- **Fix:**
  - Change the command bar default to a chord nothing else owns (for example `Ctrl+Alt+Space`; verify it is free on your machine) and leave Alt+Space as an opt-in.
  - On `Bind` failure, show a one-time pill message ("Alt+Space is taken - open Settings > Hotkeys") and a red badge on the Settings tab until fixed.
  - Settings should flag conflicts with a short list of known system/launcher chords before the user saves.
- UNVERIFIED - needs a run on Windows: with PowerToys running, launch Isle and test both chords.

### H5. Keyboard and assistive access: effectively mouse-only
- **Where:** `Styles/Theme.xaml:44` (Buttons `Focusable=False`), `:30-34` (`FocusVisualNone`), `:88,418` (tooltip text reused as automation name), `Views/AiChat/AiChatView.cs:87` (`AutomationProperties.Name="ai-input"`); no occurrences of `HighContrast`, `ClientAreaAnimation`, `AnimationsEnabled` or `UISettings` anywhere in the source (grep-verified).
- **Problems:**
  - Because every button is non-focusable and has no focus visual, Tab never reaches anything but text boxes. The tab bar, Allow/Deny, Clear, close, and timer controls cannot be operated by keyboard. Arrow-key tab switching and Enter/Space activation do not exist.
  - `ai-input` is a developer identifier, not a name a screen reader can use; localise it ("Message to AI").
  - High contrast: all brushes are hard-coded (`Theme.cs:53-66`), so Windows contrast themes are ignored; the pill stays dark/light by Isle's own setting.
  - Reduce motion: the spring animation (`IslandWindow.Motion.cs`, K=170/C=20.5 expand, K=290/C=30.6 collapse) and `Pulse(3)` ignore Windows' "Animation effects" setting.
  - Colour-only meaning exists in places (Danger/Warn/Ok dots; the agent state has text labels, which is good).
- **Why it matters for a solo dev:** keyboard control is also a productivity feature when the mouse is on another monitor. The accessibility gaps are secondary for you personally, but the keyboard gap is not.
- **Fix:**
  - Make buttons focusable (`Focusable=True`) but keep mouse clicks from stealing focus (the NOACTIVATE window already handles that); add a visible focus ring: a 2 px `AccentText` rounded outline at 2 px offset, shown only for keyboard focus (`FocusVisualStyle` set in `Theme.xaml:30-34` instead of none).
  - Add `KeyboardNavigation.TabNavigation="Cycle"` on the expanded root, Left/Right to switch tabs, Esc to collapse (handled in-window, see C1).
  - Replace `ai-input` with a localised `AutomationProperties.Name`.
  - Check `SystemParameters.HighContrast` and map brushes to `SystemColors.*` when true; check `SystemParameters.ClientAreaAnimation` and snap (no spring, no pulse) when false.
- UNVERIFIED - needs a run on Windows: inspect with Accessibility Insights whether the tooltip-based names actually appear as UIA `Name`.

---

## Medium

### M1. Fixed 640x300 stage, scale up to 2.0, and top-centre collisions
- **Where:** `IslandWindow.xaml.cs:18-20` (BaseWidth 640, BaseHeight 300, expanded 520x220, TopGap 6), `:113-127` (`Reposition`), `Core/Settings.cs` (`Scale` clamp 0.8-2.0).
- **Problems:**
  - At scale 2.0 the stage window is 1280x600 DIP (more physical pixels with display scaling) of transparent layered window parked at top-centre. UNVERIFIED - needs a run on Windows whether fully transparent areas pass clicks to windows underneath (WPF `AllowsTransparency` windows are click-through only where alpha is 0 and background is null, and `WS_EX_TRANSPARENT` is not set permanently). Check by clicking a Chrome tab at top-centre with the island collapsed.
  - Top-centre is occupied in many apps: VS Code's Command Center is on by default in the title bar centre since v1.82 (https://code.visualstudio.com/updates/v1_82), Chrome/Edge tab strips, and the Windows 11 snap bar when dragging to the top edge. A collapsed pill sits on top of those. There is no horizontal offset setting.
  - Software rendering of a large layered window is expensive; the 30 ms cursor poll, the spring on `CompositionTarget.Rendering` and the shadow rings (alpha 8/16/26 over 3-8 px) all run per frame while animating. `Core/Memory.cs` does a full compacting GC + `EmptyWorkingSet` after every collapse, which causes page-in stutter on the next hover.
- **Fix:** add `OffsetX` (-50%..+50% of screen width) and "dodge" option (shift pill 120-200 px right when the foreground window title/class is in a configurable list); shrink the stage window to the current shape bounds plus 24 px shadow margin during idle (resize in `ApplyShape` when settled); drop the forced `EmptyWorkingSet` or run it only after 60 s of idle.

### M2. Monitor selection by index
- **Where:** `Core/Screens.cs` (monitors sorted primary first, then left to right), `Settings.cs` (`Monitor` index, -1 = primary), `IslandWindow.xaml.cs:129-155` (WM_DISPLAYCHANGE handled).
- **Scenario:** Index 1 means "the second monitor in sorted order". Docking/undocking a laptop, a monitor waking after the others, or changing which display is primary re-sorts the list and the pill jumps to a different monitor or to a monitor with a different DPI (PerMonitorV2 is declared, so layout is recomputed on `WM_DPICHANGED`, but the user's choice is lost).
- **Fix:** store the device name (`\\.\DISPLAY2`, from `GetMonitorInfo` `MONITORINFOEX.szDevice`) plus a fallback index; resolve by name first, fall back to primary, and re-resolve on `WM_DISPLAYCHANGE`.
- UNVERIFIED - needs a run on Windows with two monitors and a dock.

### M3. Ukrainian text and fixed widths
- **Where:** `Modules/Voice/VoiceModule.cs:179` (message pill width `Math.Clamp(60 + text.Length * 6.6, 200, 420)`), `Views/Live/IndicatorPill.cs:14,29` (`_sub` Right-docked, no trimming), `Modules/Tools/ToolsModule.cs:172` (rec-saved pill width 250 with `Isle yyyy-MM-dd HH.mm.ss.mp4`), `Views/Timers/TimerPill.cs` (`TimerDoneBanner` 320x52, `ReminderBanner` 380x76), `Views/TabBar.cs:15` (TabWidth 34, LabelRoom 118).
- **Problems:** the estimate 6.6 px/character is a Latin average; Cyrillic capitals and many lowercase letters are wider, and Ukrainian strings are 20-40% longer than English per the brief, so 420 px (the cap) will clip before the text finishes. `_sub` docked right with no `TextTrimming` overlaps the main label in longer languages. 250 px for a 31-character mono-ish filename plus icon is borderline in either language. Fixed 320x52 / 380x76 banners will clip a localised reminder title. `Strings` EN/UK files are not in the upload, so I cannot confirm actual lengths.
- **Fix:** measure with `FormattedText` (or `Measure`) instead of `length * 6.6`; set `TextTrimming="CharacterEllipsis"` and `MaxWidth` on every label that sits next to another element; allow the banner height to grow (wrap up to 2 lines) instead of fixed 52/76; test with a pseudo-localised EN (wrap each string in "[" + text + "~~~~]", +40%) at scale 0.8, 1.0, 1.5 and 2.0.
- UNVERIFIED - needs a run on Windows with UK strings.

### M4. Tab overflow hides the two tabs you use most
- **Where:** `Views/TabBar.cs:15,119-135` (overflow into a "more" menu), `Modules/Characters/CharactersModule.cs:54` (mascot tab, enabled by default).
- **Scenario:** With the mascot tab present, n=8 and the layout (34 px tabs, 118 px label room) pushes AI and Agents into the overflow menu. The selected hidden tab swaps in, so the layout reshuffles after each selection and muscle memory about tab positions breaks. The "more" menu uses the default WPF `ContextMenu`/`MenuItem` (there are no styles for them in `Theme.xaml`), so it renders in light Windows chrome inside a dark pill.
- **Fix:** order tabs by user preference and put the mascot last / disable by default; make the overflow stable (never swap the selected tab in); style `ContextMenu`/`MenuItem`/`ComboBox` in `Theme.xaml` so they use `Surface`/`Fg` brushes; add Ctrl+1..9 or Alt+1..9 tab shortcuts while expanded.

### M5. Command bar: Enter during debounce and two-Enter confirm on destructive commands
- **Where:** `Views/Commands/CommandBarView.cs:244-248` (Enter during debounce re-queries only), `:258-268` (`Execute`/`_armed`), `Modules/Commands/Providers.cs:116-132` (shutdown `/s /t 0`, restart `/r /t 0`, `SHEmptyRecycleBin` with flags 0x7 = no confirmation, no progress UI, no sound).
- **Scenarios:**
  - Typing "empty" Enter Enter fast (a double-tap habit) arms and executes in under 200 ms: the Recycle Bin is permanently emptied with no Windows confirmation, and there is no preview of what is in it. Same for shutdown/restart with a zero delay: apps get a close request immediately.
  - Enter during the query debounce only re-queries (`:244-248`), so the first Enter can be swallowed and the result list can change under a pending arm. `_armed` is cleared on arrow-key moves (`:237,:241`) and at `:100,:114`; verify it is also cleared when a late query replaces `_results`, otherwise the second Enter could confirm a different item. UNVERIFIED - needs a run on Windows.
- **Fix:** ignore the confirming Enter within 400 ms of arming; auto-disarm after 3 s; render the armed state in `Danger`, not `Warn`, with the specific consequence ("Permanently empty Recycle Bin - 213 items, 1.4 GB"); use `/t 10` for shutdown/restart with an "abort" command available (`shutdown /a`); drop `SHERB_NOCONFIRMATION` from the flags so Windows' own prompt appears, since you already have one confirmation layer you do not need to disable the system one.

### M6. Destructive buttons without confirm or undo; shelf drag-out effects
- **Where:** `Views/Clipboard/ClipboardView.cs:33` (Clear, no confirm or undo), `Views/Shelf/ShelfView.cs:51` (Clear, no confirm), `ShelfView.cs:154` (`DoDragDrop(... Copy | Move | Link)`).
- **Scenarios:** One mis-click on Clear wipes the whole clipboard history (including pinned items, if pins live in the same list; I did not verify). Dragging a shelf file into Explorer with a modifier or into a folder on the same drive defaults to Move: the original may leave its source folder, and the shelf item then points at a path that no longer exists. UNVERIFIED - needs a run on Windows whether the shelf copies or references files.
- **Fix:** replace confirm dialogs with a 6-second "Cleared - Undo" toast (keep the deleted list in memory until it expires); restrict `AllowedEffects` to `Copy | Link` unless the shelf owns a private copy of the file; keep pinned items out of Clear.

---

## Low / polish

### L1. Pill message and toast timing
`IslandWindow.Content.cs` toast lasts 5 s regardless of length; a 420 px message that wraps needs more reading time. Scale duration with text length (about 60 ms/char, min 3 s, max 10 s) and pause the countdown while hovered.

### L2. Spring and shadow constants
`IslandWindow.Motion.cs`: expand K=170/C=20.5 is roughly critically damped (zeta ~0.79: slight overshoot), collapse K=290/C=30.6 is zeta ~0.90; fine. The shadow is drawn as nested paths with alpha 8/16/26 over a 3-8 px ring, which can band on a light background; consider a single `DropShadowEffect` only for the expanded state if software rendering allows (measure). UNVERIFIED - needs a run on Windows with light theme.

### L3. Recording and REC pill
`Modules/Tools/ToolsModule.cs:186-190` makes the REC pill itself the stop button (`Click = StopRecording`), and `StopRecording` has no try/catch: an exception during stop leaves the pill stuck in recording state. Wrap in try/catch/finally that always resets the pill state and shows a message with the error. Also name the recording by `Isle yyyy-MM-dd HH.mm.ss` consistently and add "Show in folder" to the saved pill.

---

## Handled well (one line each)
- NOACTIVATE/`MA_NOACTIVATE` window with focus acquired only for text input (`AcquireFocus`, `OnDeactivated`).
- Clipboard capture from other apps respects password-manager/exclude formats (`ClipboardService.cs:81-111`).
- QR scanning only opens `http(s)` URLs (`IsWebUrl`).
- Hook installer: backs up the settings file, merges instead of overwriting, supports `Strip`, and uses the exec-form `args` handler as documented in the Claude Code hooks reference (https://docs.claude.com/en/docs/claude-code/hooks); the helper fails open.
- Agent states have text labels beside the dots (not colour-only).
- Handles `TaskbarCreated`, `WM_DISPLAYCHANGE`, `WM_DPICHANGED`, `WM_SETTINGCHANGE`.
- Per-monitor V2 DPI declared in the manifest; `asInvoker`, no needless elevation.

## If you only fix five things
1. **Remove the global bare-Esc hotkey** (`IslandWindow.Hover.cs:124,143`); handle Esc in-window only when the island has focus.
2. **Re-arm hover properly:** reset `_insideSince` on collapse, require leave-and-re-enter, ignore hover while a mouse button is down, require real cursor motion (`Hover.cs:59-108`).
3. **Fix accent contrast:** compute `AccentFg` per accent (white or #111113), derive a separate `AccentText`, raise `FgFaint` to alpha 0.46 dark / 0.59 light, fix the light Switch and Warn (`Theme.cs:53-96`, `Theme.xaml:75-181`).
4. **Make paste-back safe:** snapshot all formats, add the three clipboard-history exclude formats to the temporary text, verify the target is foreground before Ctrl+V and fall back to "copied" if not (`ClipboardOps.cs:22-136`).
5. **Show the full command before Allow** in agent permission prompts and let timers/reminders break through fullscreen hiding (`AgentsView.cs:156`, `AgentText.cs:11`, `IslandWindow.Hover.cs:281`).
