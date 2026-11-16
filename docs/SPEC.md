# Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)

## 0. How to work on this (read first)

You are building this autonomously over many hours. The owner will not be available to answer questions. Do not stop to ask; make the most reasonable decision, write it in `DECISIONS.md` (one line each: decision + why) and keep going.

**You can be interrupted at any moment** (usage limits, restarts, a different model taking over in the next session — sometimes Fable, sometimes Opus). Therefore all state lives in files, not in your head:
- `PROGRESS.md` — milestone checklist (`[ ]`, `[~]`, `[x]`), "last action", "next step", known blockers. Update it after every meaningful step and always before a commit. Every new session starts by reading `SPEC.md`, `PROGRESS.md`, `DECISIONS.md`, `ARCHITECTURE.md` and `graphify-out/GRAPH_REPORT.md` (if present), then continues exactly where `PROGRESS.md` says.
- Commit small and often (at least once per feature, always at the end of a milestone). Never leave the repo in a non-building state at a commit.
- Create `DONE.flag` (containing a short summary) **only** when the Definition of Done (section 9) is verified. An external script loops until this file exists, so creating it early ends the job prematurely, and never creating it wastes time.

**Rules:**
- Work milestone by milestone (section 8). After each: build, run its checks, fix failures, commit.
- You must be running on the owner's Windows 10/11 machine. First check `dotnet --version` (need .NET 10 SDK). If missing, write that in `PROGRESS.md` and stop; do not fake anything.
- Be your own tester: add a debug CLI flag `--snapshot <state> <out.png>` that renders every UI state to PNG via `RenderTargetBitmap`, then actually look at the PNGs and fix visual problems (clipping, misalignment, ugly spacing) before moving on. Use fakes/unit tests for hardware-dependent features.
- Keep code simple. Plain WPF + small classes. No DI frameworks, no MVVM toolkits. Add NuGet packages only when clearly worth it, and list each in `DECISIONS.md` with the reason.
- `VERIFICATION.md`: table of every feature → how it was verified (`auto-test`, `snapshot`, `manual-needed`) and result. Be honest: anything you could not truly exercise (e.g. OBS, Steam, VPN clients, Bluetooth headphones, microphone) is `manual-needed` with exact steps for the owner to check in 2 minutes.

### Code navigation with graphify (mandatory)
The owner has the **graphify** skill (knowledge graph of the codebase). Use it so you and future sessions navigate by graph instead of grepping everything.
1. Check availability: `graphify --version`. If the CLI is missing but Python 3.10+ exists: `pip install graphifyy && graphify install` (the PyPI package is `graphifyy`, the CLI is `graphify`). If it cannot be installed, continue without it and note that in `DECISIONS.md`; do not block.
2. After M0 (when there is code), run the graphify skill on the repo root (`/graphify .`), then run `graphify claude install` once in the project so CLAUDE.md rules and the search hook make future sessions read `graphify-out/GRAPH_REPORT.md` first.
3. Rebuild/update the graph at the end of every milestone. Exclude `bin/`, `obj/`, `dist/`, `logs/` from indexing, and add `graphify-out/cache` (or the large generated files) to `.gitignore`; keep `GRAPH_REPORT.md` readable.
4. The graph is for reading/understanding, not for writing. Also keep a hand-written `ARCHITECTURE.md` (module map: what each module owns, its public interface, how it is started/stopped, data files) up to date — short, one paragraph per module.

## 1. What we are building

A small, fast, always-on-top pill at the top-center of the screen, inspired by Apple's Dynamic Island. Collapsed, it shows live activity (music, timers, downloads, recording, etc.). When the mouse moves onto it, it expands into a panel with tabs and tools. It must offer the **complete feature set** listed in section 4 — everything unlocked, no licensing, no "Pro", no trial, no telemetry, no ads.

Inspired by the *idea* of existing apps, but it must have its **own original name, look, icons, wording, mascots and code**. Do not copy any assets, texts or code from any existing product.

## 2. Hard requirements (non-negotiable)

**Performance** (the most important quality):
- Idle (collapsed, nothing active, all modules enabled): CPU < 1%, RAM < 80 MB working set. Active heavy features (OCR, recording, voice-to-text, AI chat) may use more only while in use and must release memory afterwards.
- **Every module is lazy**: it starts only when enabled and needed, and can be stopped. Each module can be switched off in settings. Heavy resources (Whisper model, OCR engine, capture sessions, HTTP clients) are created on first use and disposed after idle (e.g. 2 minutes).
- Smooth 60 fps expand/collapse (GPU-composited). Never resize the window per frame. Use one fixed-size transparent topmost window large enough for the largest state and animate the inner shape/clip with `RenderTransform`/clip geometry. Outside the visible shape, mouse input must pass through (hit-test only the shape, or toggle `WS_EX_TRANSPARENT`).
- Start-up to visible pill < 1 s (defer module startup until after the first frame).
- Event-driven wherever possible. The only allowed fast timer is the hover/cursor timer (~30 ms), paused while hidden in fullscreen. All other polling must be ≥ 1 s and only while its feature is visible/needed.

**Window behavior:**
- Borderless, transparent, `Topmost`, hidden from taskbar/Alt+Tab (`WS_EX_TOOLWINDOW`), never steals focus (`WS_EX_NOACTIVATE`). Exception: text boxes inside the island (notes, command bar, AI chat, timer input) may take focus on click and give it back when the island collapses.
- Per-monitor DPI aware (PerMonitorV2). Top-center of the selected monitor (primary by default). Re-position on display change/sleep-wake/DPI change.
- Auto-hide in fullscreen apps/games (`SHQueryUserNotificationState`), auto-hide to the screen edge (only a thin sliver stays; returns when the cursor touches the top edge), optional **click-through mode** (island is purely informational), and an **excluded-apps list** (island hidden when a listed process is foreground).
- Single instance (named mutex).

**Visual design:**
- Fully rounded pill; expanded corner radius ~28–32 px; soft subtle shadow; no window chrome.
- Themes: **Dark** (pure black `#000000` pill, white text), **Light** (near-white pill, dark text), **System** (follows Windows app theme live).
- UI scale 80–200% (live preview).
- Spring-like expand/collapse (~350–450 ms, slight overshoot, content fades/scales a bit after the shape starts moving, collapse faster than expand). Pick the smoothest technique (storyboards with `BackEase`/`ElasticEase` or a hand-written spring on `CompositionTarget.Rendering`) and note it in `DECISIONS.md`.
- Typography: Segoe UI Variable (fallback Segoe UI). Secondary text 60% opacity. One accent colour (from album art when music plays, else soft blue). 8-px spacing grid. No default WPF control chrome — write one small flat style set (button, toggle, slider, text box, list item, tab) in `Styles/Theme.xaml` and reuse it.
- Languages: UI in **English and Ukrainian** (switchable in settings). All UI strings through one `Strings` class with two dictionaries.

## 3. Tech stack and layout

- C# 14 / **.NET 10**, **WPF**, TFM `net10.0-windows10.0.19041.0` (WinRT APIs).
- Layout (flat, one project + tests):
  ```
  Isle/
    Isle.sln
    src/Isle/
      App.xaml(.cs)  IslandWindow.xaml(.cs)  Native.cs
      Core/        Settings, ModuleHost (IModule: Start/Stop/IsEnabled), EventBus, Logger, Strings, Paths
      Modules/     Media, Lyrics, Timers, Reminders, Notes, ControlCenter, LiveEvents,
                   Clipboard, Shelf, CommandBar, ScreenTools, Weather, AiChat, Voice,
                   AgentBridge, Characters, Hotkeys, Tray
      Views/       one UserControl per tab/panel
      Styles/Theme.xaml
    src/Isle.Hook/   tiny console exe used by agent hooks (named-pipe client)
    tests/Isle.Tests/   xUnit
    build.ps1  run-autonomous.ps1 (owned by the owner — do not edit)
    SPEC.md PROGRESS.md DECISIONS.md ARCHITECTURE.md VERIFICATION.md README.md
  ```
- Data: `%AppData%\Isle\` (settings.json, notes.json, clipboard store, shelf.json, reminders.json, chats, log.txt). Secrets (AI keys, clipboard text history) encrypted with DPAPI (`ProtectedData`, CurrentUser). Atomic writes (temp + replace). Corrupt file → backup `*.bak-<timestamp>` and start fresh, never crash.
- Publish: `dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained false`; `build.ps1` also builds a self-contained variant into `dist/`.

## 4. Features (all of them)

### 4.1 Collapsed pill — live activity
A priority queue decides what the pill shows (highest first): finished timer/reminder → agent waiting for approval → active recording → mic/camera in use → timer running → download/Steam progress → music → idle. Where room allows, two items share the pill (left/right).
- **Idle:** small plain pill (~120×32 at 100%); optional clock text (setting, off by default); optional small mascot (4.15).
- **Music:** thumbnail left, animated equalizer right (animates only while playing); track change shows title for ~2 s.
- **Timer:** remaining time + thin progress ring/bar. **Finished:** pulse ×3, system sound, "Done" with Dismiss / +1 min.
- **Downloads:** progress of an active download (see 4.8). **Steam:** game download progress.
- **Microphone/camera in use:** small coloured dot + icon (orange mic / green camera) — privacy indicator.
- **OBS recording:** red dot + elapsed time.
- **Windows notifications:** toast-like banner with app name + text (see 4.8 for API limits).
- **VPN status, keyboard layout switch, Caps Lock:** brief transient indicators (VPN connect/disconnect, layout code like `EN`/`UK`, Caps Lock on/off icon).
- **Weather:** optional temperature next to idle pill; rain alert banner.
- **Reminders:** banner with Done / Snooze.
- **Agent status (4.14):** dot (working = animated, waiting = attention colour, done = check).

### 4.2 Expansion
- Opens when the cursor enters the pill (~120 ms hover-intent), closes ~250 ms after the cursor leaves the expanded area. `Esc` collapses. Global hotkey toggles. Also expands while dragging files over it (shelf) and when the user clicks a banner.
- Expanded ≈ 520×220 px at 100% scale; a tab bar with sliding indicator: **Music · Timers & Reminders · Notes · Clipboard · Shelf · Control · Tools · Weather · AI · Agents** (tabs for disabled modules are hidden; overflow goes into a "more" menu). Remember last tab; open Music first after a track change while music plays. Crossfade between tabs, no size jumps.

### 4.3 Music tab (+ synced lyrics)
- Windows **Global System Media Transport Controls** (`GlobalSystemMediaTransportControlsSessionManager`): Spotify, browsers, VLC, etc. Show art, title, artist, source app, progress with elapsed/total, prev/play-pause/next, seek by clicking/dragging (`TryChangePlaybackPositionAsync`). Event-driven (`CurrentSessionChanged`, `MediaPropertiesChanged`, `PlaybackInfoChanged`, `TimelinePropertiesChanged`); progress extrapolated locally, UI refresh ≈ 4×/s only while expanded and playing.
- Handle: no session, multiple sessions (prefer playing, else most recent), missing thumbnail (gradient from title hash), long titles (marquee on hover). Dominant colour from art (downsample ~16×16, average most saturated pixels) with smooth transition.
- **Lyrics:** fetch synced lyrics from the free LRCLIB API (`https://lrclib.net/api/get` with `artist_name`, `track_name`, `album_name`, `duration`; fall back to `/api/search`), no key needed. Parse LRC, highlight the current line in time with playback (use extrapolated position), optionally show the current line in the pill. Cache on disk; handle not-found/instrumental; send a proper `User-Agent`; only fetch when the lyrics view is open or the setting "always fetch" is on.

### 4.4 Timers & Reminders tab
- Presets 5/10/15/25/45 min; free input accepting `90`, `1:30`, `1h30m`, `45s`, `2m 30s`; multiple timers; optional label; pause/resume/cancel; stopwatch. Pill shows the soonest. Survive collapse and restart (store absolute end timestamps; if already ended while closed, show as finished).
- **Reminders** from natural-language phrases (English **and Ukrainian**): "remind me in 2 hours to call mom", "tomorrow 9:00 submit report", "every monday 08:30 gym", "нагадай через 2 години подзвонити", "щодня о 22:00 пити воду". Support: relative times, absolute times, weekdays, "tomorrow", dates, repeat rules (daily/weekly on days/monthly/every N days-hours). Write the parser yourself (small, table-driven, unit-tested with ≥ 40 phrases). A reminder fires as a banner with Done / Snooze (5 min, 1 h, tomorrow) and a system sound; missed reminders fire at next start. List/edit/delete reminders in the tab.
- Pure logic with an injectable clock (`Func<DateTimeOffset>`), xUnit-tested.

### 4.5 Notes tab
- Formatted notes: `RichTextBox`-based editor with bold/italic/underline (Ctrl+B/I/U), bullet and numbered lists, checkbox items, headings (2 sizes). Stored as XAML FlowDocument text inside JSON plus a plain-text shadow field for search. Note list with search; simple one-level **folders** (dropdown filter, move to folder). Autosave (debounced 500 ms). Delete with 5-s undo toast. Quick-capture hotkey opens a new empty note with the caret focused. Pinned notes on top. Tested: CRUD, search, atomic save, corruption recovery.

### 4.6 Control Center tab
- Tiles: **Wi-Fi** and **Bluetooth** (Windows.Devices.Radios: `RequestAccessAsync`, `GetRadiosAsync`, `SetStateAsync`; degrade gracefully if denied/missing), **Dark theme** (`AppsUseLightTheme` + `SystemUsesLightTheme` in `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`, then broadcast `WM_SETTINGCHANGE` "ImmersiveColorSet"), **master volume + mute** (Core Audio `IAudioEndpointVolume`, reflect external changes), **screen brightness** on laptops (WMI `WmiMonitorBrightnessMethods`; hide if unsupported).
- **Bluetooth devices:** list paired devices; one press connects/disconnects audio headphones (best-effort: use `Windows.Devices.Enumeration` + `BluetoothDevice`/`AudioPlaybackConnection`; document in DECISIONS.md which approach actually worked on this machine, show state "connecting…/connected").
- **VPN:** list Windows built-in VPN profiles (`Get-VpnConnection`/`rasdial` connect and `/disconnect`) and detect common clients (WireGuard, OpenVPN, Tailscale, Proton, Cloudflare WARP, NordVPN, Mullvad) by installed service/process/adapter; connect/disconnect through the client's own CLI when it has one (e.g. `tailscale up/down`, `warp-cli connect/disconnect`, `rasdial`); otherwise just show status and a "open client" button. Never require admin silently; show a clear message when elevation is needed.
- Refresh state on open and on radio/network/audio events.

### 4.7 Clipboard tab
- `AddClipboardFormatListener` (event-driven). Keep text and images (last 200 entries, configurable; images stored as PNG files in `%AppData%\Isle\clip\`, text encrypted with DPAPI). Search, pin (pinned never expire), delete, clear all. **Click an entry = paste into the previously active window** (put it on the clipboard, then `SendInput` Ctrl+V; thanks to NOACTIVATE the target keeps focus; restore the previous clipboard afterwards).
- **Privacy:** ignore clipboard content marked `ExcludeClipboardContentFromMonitorProcessing` / `CanIncludeInClipboardHistory = 0` (password managers) and apps in the excluded list. Never log clipboard content.

### 4.8 Live events (sources for the pill)
Each source is its own small class with a fake for tests, started only when enabled; degrade silently if unavailable.
- **Downloads:** watch the user's Downloads folder (`FileSystemWatcher`) for browser partial files (`*.crdownload`, `*.part`, `*.tmp` of Chrome/Edge/Firefox) and show filename + growing size; progress % only if the total is known (Chrome writes it in the file name/headers — use size growth + speed otherwise). Click opens the folder.
- **Microphone/camera in use:** read `HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{microphone|webcam}\*` (`LastUsedTimeStart` > `LastUsedTimeStop` means in use; include `NonPackaged` apps); show which app.
- **Windows notifications:** use `UserNotificationListener` (requires package identity/capability for unpackaged apps). Try it; if it does not work for an unpackaged WPF app, implement packaging with a sparse MSIX package (identity) if feasible within reason; otherwise mark the feature `unavailable` in VERIFICATION.md with the exact reason, keep the settings toggle disabled with an explanation, and move on. Do not burn hours here.
- **OBS recording:** connect to OBS WebSocket v5 (`ws://localhost:4455`, optional password in settings) → show recording state and elapsed time; if OBS is not running, stay silent.
- **Steam downloads:** find Steam via `HKCU\Software\Valve\Steam` (`SteamPath`), read `libraryfolders.vdf` and `steamapps\appmanifest_*.acf` (`StateFlags`, `BytesDownloaded`, `BytesToDownload`, `name`); watch the manifest files; show game name + % and speed.
- **VPN status:** from network interface change events (`NetworkChange`) + the detection in 4.6.
- **Keyboard layout:** detect foreground layout changes (`GetKeyboardLayout` of the foreground thread, checked on `EVENT_SYSTEM_FOREGROUND` via `SetWinEventHook` plus a 500-ms check only while the setting is on) → show `EN`/`UK`/… briefly.
- **Caps Lock:** low-level keyboard hook (`WH_KEYBOARD_LL`, keep the callback tiny and never block) or `GetKeyState` after key events → brief on/off indicator.

### 4.9 File shelf
- Drag files/folders onto the island (it expands on drag-over) to park them; items show icon/thumbnail + name; **drag them out** into any window (`DoDragDrop` with `FileDrop`), optional "remove after drop". Persist references (`shelf.json`), mark missing files, copy-path and open actions, clear all.

### 4.10 Command bar (default hotkey Alt+Space)
- `RegisterHotKey`; opens the island with a focused input. Results list updates as you type (debounce 80 ms). Providers:
  - **Apps & files:** index Start Menu shortcuts (all users + current user) and recent files; fuzzy match (subsequence + acronym + prefix scoring); Enter launches; plus configurable folders to search by name (background, incremental, cached).
  - **Calculator:** own safe expression parser (+ − × ÷ ^ %, parentheses, functions sqrt/sin/cos/log/ln/abs/round, constants, thousands separators, `15% of 80`, unit conversion for length/mass/temperature/data size). Enter copies the result.
  - **Currency:** `100 usd to eur` / `100 євро в гривню`; rates from a free no-key API (e.g. Frankfurter/ECB) cached 12 h with offline fallback to the cache; show the rate's date.
  - **Timers/reminders:** `timer 10m`, `remind me …`, `нагадай …` (reuse 4.4).
  - **System commands:** lock, sleep, restart, shutdown (always ask for confirmation), open Settings pages, empty recycle bin.
  - **Web & AI:** `g <query>` web search, URLs open directly, `? <question>` sends to AI chat (4.13).
- Esc closes; arrow keys navigate; results never block the UI thread.

### 4.11 Screen tools (Tools tab + hotkeys)
- **Screenshot with markup:** full-screen dim overlay across all monitors, drag to select a region (or window/fullscreen), then a markup toolbar: pen, highlighter, arrow, rectangle/ellipse, text, blur/pixelate, undo/redo, colour + thickness. Copy to clipboard / save to `Pictures\Isle\` / copy path. Use `Windows.Graphics.Capture` or `CopyFromScreen`; handle mixed DPI monitors correctly.
- **Text capture (OCR):** select a region → `Windows.Media.Ocr.OcrEngine` (use installed languages; offer to pick the language) → text copied to clipboard + shown in a toast.
- **QR code reading:** select a region → decode (ZXing.Net or equivalent) → show the content with Copy / Open (open only after showing the URL) buttons.
- **Colour picker:** magnifier loupe following the cursor with live HEX/RGB readout; click copies HEX; keep a short history of picked colours.
- **Screen recording:** `Windows.Graphics.Capture` + hardware H.264 MP4 encoding (Media Foundation / `MediaTranscoder` with `MediaStreamSource`, or a small well-known library — choose and justify). Region or full screen, optional system audio and microphone (WASAPI via NAudio) mixed into the file, start/stop hotkey, elapsed time in the pill, save to `Videos\Isle\`. Recording must not add visible lag to the system (limit to 30 fps, 1080p default).

### 4.12 Weather tab
- Open-Meteo (no key): geocoding for the city typed in settings (also "use my approximate location" via IP geolocation toggle, off by default); current conditions, **24-hour** hourly strip and **7-day** forecast, temperature (°C/°F) optionally in the pill; **rain alerts** ("rain starts in ~20 min") as a banner once per event. Refresh every 30 min only when enabled; cache last result for offline display; weather icons drawn as simple original vector shapes.

### 4.13 AI chat tab (bring-your-own key)
- No keys are built in. Settings: provider = **Anthropic** (API key, model name field) or **OpenAI-compatible** (base URL, key, model); keys DPAPI-encrypted; "test connection" button.
- Chat: multiple conversations, context kept, **streaming** responses (SSE), stop button, copy answer, markdown rendering (code blocks, lists, bold), per-chat system prompt. **Attachments:** images (vision), PDF (send as document where supported, else extract text with a PDF library), Word `.docx` and Excel `.xlsx` (extract text/CSV via Open XML), plain text/code files; size limits with clear messages.
- **Text actions:** select text in any app → hotkey → mini popup in the island with Summarise / Translate (to English / Ukrainian / Dutch) / Fix grammar / Explain / Reply — works by simulating Ctrl+C (save and restore the clipboard) and shows the result with Copy / Replace selection (Ctrl+V back).
- Command-bar questions with the `?` prefix show a short streamed answer inline.
- Handle errors (401, 429, network down) with readable messages; never log keys or message bodies.

### 4.14 Agents tab — Claude Code and Codex integration
- Show each running agent session as a card: project folder name, state (**working / waiting for you / done**), last action text. Pill dot reflects the most urgent state.
- **Allow / Deny from the island:** when an agent waits for permission, the card shows the command/tool and buttons **Allow · Deny** (and numbered option buttons when the agent asks a multiple-choice question), so the user does not need to switch to the terminal.
- Mechanism: `Isle.Hook.exe` (tiny, fast, no dependencies) is registered as a **Claude Code hook** (events such as `UserPromptSubmit`, `PreToolUse`/permission request, `Notification`, `Stop`) and talks to the island over a **named pipe**. For permission requests it blocks waiting for the island's answer and prints the hook decision JSON on stdout. **Read the current official hooks documentation (https://code.claude.com/docs/en/hooks) and implement exactly what it says — do not guess field names.** The hook must **fail open**: if the island is not running, the pipe is missing, or the timeout (default 60 s, configurable) passes, exit 0 with no decision so the normal terminal prompt appears.
- For **Codex CLI**: read its current docs; wire status from its supported notification/hook mechanism (e.g. the `notify` setting in its config) and approvals only if a supported mechanism exists; otherwise status-only. Record the outcome in `DECISIONS.md`.
- "Install hooks" / "Remove hooks" buttons in settings: show exactly what will be changed, **back up** the target config (`~/.claude/settings.json`, Codex config) first, merge (never overwrite) existing hooks, and make removal restore only Isle's entries. Never modify these files without the explicit button press.
- Unit-test the pipe protocol and hook JSON building with fakes.

### 4.15 Characters ("tenants")
- Create **3 original animated characters** (your own simple geometric creatures drawn in XAML vector shapes — do not imitate any existing mascot or product character; give them invented names). Each supports: idle breathing/blinking, **eyes follow the cursor**, **dance to music** (bounce driven by playback state, and by real loudness if WASAPI loopback is cheap enough — otherwise a tempo-agnostic bounce), **fall asleep** after N minutes without input (`GetLastInputInfo`) and wake on movement, and a happy reaction when a timer finishes.
- Shown small in the idle pill (optional) and larger on the expanded panel. Selectable in settings (a gallery). Animations run at ≤ 30 fps and fully pause when not visible; use frozen/cached drawings so idle cost stays within budget.

### 4.16 Voice to text
- Hold a hotkey (default `Win+Alt+V`) to record from the default microphone (NAudio/WASAPI, 16 kHz mono); on release, transcribe **offline** with Whisper (Whisper.net / whisper.cpp binding) and paste the text into the previously active window (clipboard + Ctrl+V, then restore the clipboard). Languages: automatic detection with English/Ukrainian/Dutch preferred. The model (e.g. `base` or `small` multilingual `ggml`) is downloaded on first use from the official source into `%AppData%\Isle\models\` with a progress bar, checksum verified if one is published; the engine is loaded on demand and unloaded after 2 min idle. Show a recording waveform/level in the pill while holding the key. If no microphone or model is available, show a clear message.

### 4.17 Tray, settings, autostart, hotkeys
- Tray icon (generated, original) with Show/Hide, Settings, Start with Windows (`HKCU\...\Run`), Quit. Settings panel (inside the island or a small window): theme, scale, language, monitor, auto-hide, click-through, excluded apps, per-module toggles, per-module options (cities, API keys, OBS password, folders to index, clipboard limits), characters gallery, hook install/remove, a hotkey table.
- Default hotkeys (all configurable; registration failures shown in settings, not fatal): Alt+Space command bar · Win+Alt+Space toggle island · Win+Alt+N new note · Win+Alt+A text actions · Win+Alt+V hold-to-talk · Win+Alt+S screenshot · Win+Alt+R start/stop recording · Win+Alt+C colour picker · Win+Alt+Q QR/OCR capture.
- Global exception handler logs to `%AppData%\Isle\log.txt` (rotate at 1 MB) and keeps the app alive; a failing module shows a muted state and never takes the island down.

### 4.18 Explicitly out of scope
Auto-update/installer signing, licensing, payments, telemetry, any cloud account. (Updates = the owner re-runs `build.ps1`.)

## 5. Quality rules
- Nullable enabled; warnings as errors for new code where practical. No `Thread.Sleep`, no busy loops, no UI-thread blocking; async/await for WinRT/IO. Dispose timers/hooks/subscriptions; re-subscribe media sessions on change without leaks.
- All Win32/WinRT/network calls wrapped in try/catch at the module boundary.
- Network only for features the user enabled (lyrics, weather, currency, AI, model download). No analytics. Secrets never logged.
- Security: the command bar's system commands confirm first; QR/URL results are never opened automatically; hook helper fails open; config edits are backed up.

## 6. Performance verification
At M1, M6, M11 and M15 measure idle CPU/RAM (`Get-Process` over 60 s, all modules enabled, nothing playing) and record numbers in `DECISIONS.md`. If the budget in section 2 is violated, fix before continuing. Also record peak RAM during OCR, recording, voice-to-text, and AI streaming.

## 7. Testing
xUnit for: timer logic, reminder/NL parser (≥ 40 phrases EN+UK), calculator parser, currency parsing/conversion with a fake rates source, fuzzy matcher, notes store, clipboard store (dedupe, pins, limits, DPAPI roundtrip), settings migration, LRC parser, hook JSON + pipe protocol, Steam manifest parser, Chrome partial-download detector. UI via `--snapshot` PNGs of every state in both themes and both languages.

## 8. Milestones (in order; commit after each; update PROGRESS.md and the graph)

0. **M0 Skeleton:** solution, tray, single instance, fixed transparent topmost no-activate window, DPI, click-through outside the shape, black pill visible; `ModuleHost`, `Settings`, `Logger`, `Strings` (EN/UK), `build.ps1`, docs stubs, `git init` (if needed), `.gitignore`; then graphify setup (section 0).
1. **M1 Expand/collapse & shell:** hover logic, spring animation, tab bar, themes (dark/light/system), scale, auto-hide to edge, click-through mode, fullscreen auto-hide, excluded apps, hotkeys infrastructure, snapshot tool. Measure perf.
2. **M2 Music + lyrics.**
3. **M3 Timers & reminders (NL parser EN+UK).**
4. **M4 Notes (rich text, folders).**
5. **M5 Control Center** (Wi-Fi, Bluetooth + devices, VPN, dark theme, volume, brightness).
6. **M6 Live events** (downloads, mic/camera, notifications*, OBS, Steam, VPN, layout, Caps Lock) + pill priority queue. Measure perf.
7. **M7 Clipboard + File shelf.**
8. **M8 Command bar.**
9. **M9 Screen tools** (screenshot+markup, OCR, QR, colour picker, recording).
10. **M10 Weather.**
11. **M11 AI chat + text actions.** Measure perf.
12. **M12 Voice to text.**
13. **M13 Agents integration** (hook helper, pipe, Claude Code, Codex, install/remove).
14. **M14 Characters.**
15. **M15 Polish & release:** settings panel completeness, Ukrainian strings review, every state snapshot in both themes/languages, full perf measurement, soak test, `README.md`, `VERIFICATION.md`, `ARCHITECTURE.md`, `build.ps1` run and the published exe launched.

## 9. Definition of Done
- `dotnet build -c Release` has no errors; `dotnet test` is green.
- `build.ps1` produces a working exe in `dist/`, and it starts and shows the island.
- All features in section 4 are implemented, or explicitly marked `unavailable`/`manual-needed` in `VERIFICATION.md` with a concrete reason and steps.
- Performance budget met; measurements recorded.
- 10-minute soak test (music play/pause, timers, notes, clipboard copies, control toggles, tab switching) without unhandled exceptions or runaway memory.
- Docs written; git history has at least one commit per milestone; graph report generated (or the reason it could not be).
- Only then create `DONE.flag`, and write the final message to the owner **in Ukrainian**: what works, what needs a manual check (from `VERIFICATION.md`), measured performance, how to run and enable autostart.
