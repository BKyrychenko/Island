# VERIFICATION

## Initial WPF audit repairs

| Check | Result |
|---|---|
| Release suite | 556 passed on Windows 11 with .NET 10. |
| Escape and pointer intent | An isolated desktop profile showed the old WPF version reserved Escape while expanded. The repaired version leaves Escape free and stays collapsed during a held-button hover. |
| Keyboard navigation | Win+Alt+Space focused the expanded island, Tab reached a radio button, focused Escape collapsed it, and Ctrl+Alt+Space focused the command field. |
| Published package | The .NET 10 x64 package and single-file hook helper were built. The package rendered a Ukrainian light-theme snapshot and opened its visible desktop window. |
| Fullscreen timer banner | A borderless foreground window hid both versions before the timer fired. On expiry the repaired banner appeared over fullscreen. The original stayed hidden. |
| Tier 2 idle budget | 60 one-second samples after startup on the i5-9300H with default modules. Average 19.0 MiB working set, 28.8 MiB private, 0.36% of one logical CPU and 0.045% of machine CPU. Peak working set 23.4 MiB. |
| Agent approval | Authenticated pipe round trips passed and a long Ukrainian approval snapshot showed scrolling and a disabled Allow action. Existing agents need the hook helper upgraded through Isle before they connect to the new endpoint. |
| Contrast and layouts | Palette matrix tested both themes. Exported Ukrainian control center, settings, reminder, overflow, command and agent approval snapshots. |
| Clipboard | In-memory tests covered exact HTML/RTF/custom formats and the three exclusions. No real Excel or elevated target was tested. Windows offers no reliable confirmation that an external app has finished reading after Ctrl+V. The sequence guard protects newer copies, but a target reading later than the 1.5-second restore window can get the restored content. External apps can also send their own Ctrl+C results to Windows history before Isle receives them. |
| Media and monitors | Automated suite passed. Restarting a real media player under the same AUMID and mixed-DPI docking were not exercised. |
| Anthropic | Fake HTTP/SSE covered attachments, errors, refusal and stream completion. No provider request using real credentials was sent. |

## Compact WPF controls at 80%

Measured the arranged WPF controls at 80% in isolated English and Ukrainian snapshot profiles, in both themes. The original 28-DIP tab target measured 22.4 effective DIPs and its 12-DIP label measured 9.6. The expanded capsule remained 416 × 176 effective DIPs.

| Control | Result at 80% |
|---|---|
| Navigation | Icon-only tabs measured 27.2 × 24 DIPs. More and Settings measured 24 × 24. The selected tab, overflow list and Settings navigation used 12-DIP text. The active Ukrainian Hotkeys label fit in full. |
| Common actions | The gallery and Settings close buttons, Notes formatting and pin actions, and all six Tools fixture swatches measured 24 × 24 DIPs. The swatches expose localized copy actions to the keyboard and UI Automation. |
| Settings | Each hotkey recorder measured 104 × 24 DIPs. The module picker measured 130.4 × 24 in English and 166.4 × 24 in Ukrainian. |
| Sliders | The Settings scale and Music seek controls have 24-DIP hit areas at 80%. The visible thumb and rail retain their original sizes. Routed drag and keyboard checks passed. |
| Notes | All eleven toolbar actions fit in one row at 80% and 100%. At 80%, they retain 24 × 24 DIP hit areas and pass corner hit tests. The sidebar is 8 base DIPs narrower to recover the editor's height. |
| Display scale | At simulated 125% and 150% display DPI, the smallest button in the overflow, Notes, Tools and Hotkeys fixtures measured 24.3 and 24.0 effective DIPs respectively. Physical monitor switching remains a manual check. |

Every feature → how it was verified and the result.
Methods: `auto-test` (xUnit), `snapshot` (PNG looked at), `live` (ran the real app and observed/measured),
`manual-needed` (could not be truly exercised here — steps for the owner), `unavailable` (with reason).

| Feature | Method | Result |
|---|---|---|
| Solution builds (Release, warnings as errors) | live | ok |
| Settings: defaults, roundtrip, corruption backup, migration, normalisation | auto-test | ok (5 tests) |
| Strings EN/UK key parity | auto-test | ok |
| ModuleHost: lazy start, enable/disable, failure isolation | auto-test | ok |
| EventBus | auto-test | ok |
| Single instance | live | ok — second process exits immediately |
| Pill visible top-centre, black, 120×32 @100% | live (screenshot at 125% DPI) | ok |
| Tray icon + menu | manual-needed | Right-click the Isle icon in the notification area: Show/Hide, Settings (opens the settings panel in the island), Start with Windows, Quit should work |
| Idle budget (M0) | live | 8 MB WS / 31 MB private / 0% CPU |
| Hover expand (120 ms intent) / collapse on leave (250 ms) | live (cursor driven by script, screenshots) | ok |
| Spring expand/collapse (~400 ms perceived, slight overshoot, collapse faster) | auto-test + live | ok |
| Tab bar with sliding indicator, label on selected tab, "more" overflow list, gear | snapshot (dark/light × en/uk, 150% scale) | ok |
| Toast inside the panel (text + action) | snapshot | ok |
| Themes Dark / Light / System (follows Windows live via WM_SETTINGCHANGE) | snapshot (dark/light); System: manual-needed | Switch Windows Settings → Personalisation → Colours → app mode; the island recolours within a second |
| UI scale 80–200% | snapshot at 150% + manual-needed for the live preview | ok; Settings → General → Size: drag the slider, the island resizes as you go |
| Auto-hide to screen edge (sliver, returns on top-edge touch) | snapshot (edge-hidden) | manual-needed for the live slide: Settings → General → "Tuck away at the screen edge", move cursor away 1.5 s → island slides up; touch the top edge → it returns |
| Click-through mode (WS_EX_TRANSPARENT) | manual-needed | Settings → General → Click-through → close the panel; clicks on the pill reach the window below. Switch it off again via tray → Settings (the panel itself is clickable while open) |
| Fullscreen auto-hide | manual-needed | Play a video fullscreen / press F11 in a browser → island disappears; exit → it returns |
| Excluded apps | auto-test (name matching) + manual-needed | Settings → General → "Hide while these apps are in front" → type `notepad`, Enter → island hides while Notepad is in front |
| Global hotkeys infrastructure (RegisterHotKey, failures non-fatal, shown in Settings → Hotkeys) | auto-test (gesture parsing) + manual-needed | Press Win+Alt+Space → panel opens/closes |
| Esc collapses | manual-needed | Expand, press Esc |
| Pill priority queue (selection, sharing, banners) | auto-test | ok |
| --snapshot tool | live | `Isle.exe --snapshot all <dir>` renders every state × theme × language |
| Start-up to visible pill | live | 457 ms to input-idle |
| Idle budget (M1) | live | 38.7 MB WS / 41.3 MB private / 0.009 % CPU over 60 s |
| Music: GSMTC session follow, pill (art + equalizer), banner on track start, tab (title/artist/source, progress extrapolation) | live (real SMTC session from a helper process, screenshots) + snapshot | ok |
| Music: prev / play-pause / next / seek by click or drag | manual-needed | Play a song in Spotify or a browser, open the Music tab, press the buttons and click/drag the progress bar |
| Music: multiple sessions (prefer playing, else current/recent) | auto-test (`PlaybackMath.Choose`) | ok |
| Music: missing art gradient, dominant colour, accent transition | auto-test + snapshot (`music-tab-noart`) + live | ok |
| Music: long title marquee on hover | manual-needed | Play a track with a long title, hover the title in the Music tab — it scrolls |
| Lyrics: LRC parser, LRCLIB get→search fallback, caching (incl. not-found expiry), title cleaning | auto-test (17 tests) | ok |
| Lyrics: live fetch + current line in pill | live (LRCLIB, real track metadata) | ok — correct line shown in time |
| Lyrics view in the Music tab (synced, plain, not found, instrumental, error, off) | snapshot (`music-lyrics`, `music-lyrics-notfound`) | ok |
| Timers: duration input formats, presets, labels, pause/resume/cancel, +1 min, stopwatch, survive restart | auto-test (TimerBook, DurationParser) + snapshot (`timers-tab`) | ok |
| Timer in pill (ring + remaining), shared with music in the side slot | snapshot (`timer-pill`, `timer-shared`) + live | ok |
| Timer finished: banner, sound, pulse ×3, Dismiss / +1 min | live (seeded timer, screenshot) + snapshot | banner ok; sound/pulse: manual-needed — start a 10 s timer (type `10s`), wait: a short system sound plays and the island bounces 3 times |
| Reminder NL parser EN + UK (≥ 40 phrases) | auto-test (62 phrases + negatives) | ok |
| Repeat rules (daily, weekly days, weekdays, monthly with clamping, every N days/hours/minutes) | auto-test | ok |
| Reminder banner Done / Snooze 5 min / 1 h / Tomorrow; clickable without expanding | live (seeded reminder, real click on Done) | ok |
| Missed reminders/timers fire at next start | auto-test + live (seeded files) | ok |
| Reminders list / edit (loads the original phrase) / delete | snapshot + manual-needed | Open Timers tab, press the pencil on a reminder, change the time, Enter |
| Notes: CRUD, search (all words, case-insensitive), folders, pins, atomic save, corrupt file backup | auto-test (8 tests) | ok |
| Notes: formatting round trip (bold/underline/heading/numbered list) | auto-test (STA) | ok |
| Notes tab: list, editor toolbar, checkbox glyphs, empty state, delete toast with Undo | snapshot (dark/light × en/uk) | ok |
| Quick capture Win+Alt+N: opens a new note with the caret in it, typing autosaves, Esc collapses and returns focus | live (simulated hotkey + typing, UIA focus check, notes.json check) | ok |
| Notes: Ctrl+B/I/U, bullets/numbers, click on ☐ toggles, Undo after delete | manual-needed | In the Notes tab: select a word, press Ctrl+B; press the ☐ button, click the box; delete the note and press Undo within 5 s |
| Control: Wi-Fi / Bluetooth radio state, dark-mode state, volume + mute state, brightness (WMI) | live (real machine, screenshot) + snapshot | ok — reads match the system (brightness 43 %) |
| Control: toggling Wi-Fi / Bluetooth / dark mode, moving volume and brightness sliders | manual-needed | Open the Control tab, click each tile and drag both sliders; Windows' own quick settings should follow |
| Control: volume follows external changes | manual-needed | With the Control tab open, press the keyboard volume keys — the slider moves |
| Bluetooth devices list + connect/disconnect audio (KS one-shot reconnect) | snapshot + manual-needed | Click the chevron on the Bluetooth tile; with paired headphones nearby press Connect, then Disconnect |
| VPN: profile/client detection | auto-test (4 tests) + live (none installed on this machine) | ok |
| VPN connect/disconnect (rasdial / tailscale / warp-cli / mullvad / nordvpn) | manual-needed | Open the VPN tile, press Connect on a listed VPN; if admin rights are needed a message says so |
| Fullscreen auto-hide vs maximized browser | live | island stays visible over a maximized Chrome window (was hidden before the fix) |
| Live: Downloads (partial file detection, final names, size/speed, finished flash) | auto-test (detector + source with fake sink) + live (screenshots m6/1, m6/2) | ok |
| Live: Chrome partial-download detector | auto-test | ok |
| Live: Steam (libraryfolders.vdf, appmanifest parser, progress pill) | auto-test + snapshot | ok; manual-needed live: start a game download in Steam → pill shows name + % + speed |
| Live: mic/camera in use (consent store, NonPackaged, event-driven) | auto-test (parser) + live (temporary registry entry → pill, removed → pill gone) | ok |
| Live: OBS recording (WebSocket v5, auth) | auto-test (auth) + snapshot | manual-needed: OBS → Tools → WebSocket Server Settings → enable (port 4455); in Isle Settings → Options → Live activity enter the port/password (stored DPAPI-encrypted) → start recording → red dot + elapsed time |
| Live: Windows notifications | unavailable | `UserNotificationListener` needs package identity; for this unpackaged app `RequestAccessAsync` fails with 0x80070490. Sparse MSIX identity would need a machine-trusted signing certificate (admin). Option `live.notifications` stays off by default |
| Live: VPN connect/disconnect flash | snapshot | manual-needed: connect a VPN (e.g. `tailscale up`) → "VPN connected" flash for 2.5 s |
| Live: keyboard layout flash | auto-test (codes) + snapshot | manual-needed: press Win+Space to switch EN↔UK → "UK"/"EN" flash |
| Live: Caps Lock flash | live (key sent by script, screenshot m6/3) + snapshot | ok |
| Pill priority order (finished > agent > rec > mic/cam > timer > download > music) | auto-test | ok |
| Idle budget (M6, all modules) | live | 55.5 MB WS / 0.29 % of one core over 60 s |
| Clipboard store: dedupe, pins, limit, clear keeps pinned, search, DPAPI on disk, images, corruption | auto-test (14 tests) | ok |
| Clipboard capture (text + image) via AddClipboardFormatListener | live (script copies text/image → tab screenshot m7/1) | ok |
| Clipboard privacy: ExcludeClipboardContentFromMonitorProcessing skipped | live (private item never stored) | ok; CanIncludeInClipboardHistory=0 path: manual-needed (copy a password from a password manager → not in the list) |
| Clipboard: click entry = paste into the previous window, previous clipboard restored | live (test window received the text; image was back on the clipboard) | ok |
| Clipboard excluded apps | auto-test (matching) | ok |
| Shelf store: add/dedupe/normalise/persist/missing/remove/clear/corruption | auto-test (6 tests) | ok |
| Shelf: drag files onto the island → expands to Shelf, drop hint, item saved | live (OLE drag from a helper window, screenshots m7/drag-over, m7/dropped) | ok |
| Shelf: drag an item out into another window (FileDrop), remove after drop option | manual-needed | Open the Shelf tab, drag a tile onto the desktop or an Explorer window → the file is copied/linked there; with "Remove after drag-out" on, the tile disappears |
| Shelf: missing file mark, open, copy path, clear | snapshot (shelf-tab) + manual-needed for open/copy | Select a tile → toolbar buttons |
| Command bar: Alt+Space opens with focused input, results as you type | live (keys sent by script, screenshots m8/b1, b2) | ok |
| Command bar: apps incl. Store apps (Notepad found via shell:AppsFolder), shell icons | live | ok |
| Command bar: calculator (35 cases), units (15), Enter copies | auto-test + live (2^10 + 15% of 80 -> 1036 on the clipboard) | ok |
| Command bar: currency parsing EN/UK, NBU rates, 12 h cache, offline fallback | auto-test (fake source) + live (100 USD = 4,542.74 UAH, rate date shown) | ok |
| Command bar: fuzzy matcher ranking | auto-test | ok |
| Command bar: system commands confirm with a second Enter; best result selected while results stream in | auto-test (CommandBarTests) | ok; manual-needed for the real actions: type "lock", Enter, Enter -> PC locks |
| Command bar: timers/reminders ("timer 10m", "remind me in 5 min to stretch", "нагадай ...") | manual-needed (parsers auto-tested in M3) | Alt+Space -> "timer 10m" -> Enter -> timer pill appears |
| Command bar: URL / g search / ? AI | manual-needed | "github.com" Enter opens the browser; "g wpf spring" searches; "? ..." goes to AI chat after M11 |
| Command bar: configurable folders | manual-needed | Settings → Options → Command bar → "Extra folders to search": e.g. `C:\Users\you\Documents`, Enter → file names appear after the background index |
| Screen selection overlay (dim, frame, size label, click-window, Enter = monitor, Esc) | live (screenshot m9/1-overlay) | ok; mixed-DPI multi-monitor: manual-needed (only one monitor here) — connect a second monitor with another scale and select on each |
| Screenshot markup (tools, colours, widths, undo/redo, copy/save/copy path) | live (editor opened on the selection, m9/5-markup) + auto-test (arrow geometry) | ok; drawing/saving by hand: manual-needed — Win+Alt+S, drag, draw, Save → file in Pictures\Isle |
| OCR (Windows.Media.Ocr, language chips, clipboard + toast) | live (test window → "Island OCR check 4721" exact) + snapshot | ok |
| QR / barcode reading (ZXing.Net), Open only for web links | auto-test (roundtrip incl. UTF-8, inverted, none, URL filter) + snapshot | ok; live with a real code: manual-needed — show a QR on screen, Win+Alt+Q, drag over it |
| Colour picker (loupe, HEX/RGB, click copies, history) | live (#129AE3 picked exactly) + snapshot | ok |
| Screen recording (WGC + MediaTranscoder MP4, system audio, REC pill, stop, save to Videos\Isle) | live (9 s MP4 with AAC audio track, pill, saved, memory released) | ok; microphone mixing and region recording: manual-needed — enable "Microphone" in the Tools tab, press Record, drag a region, speak, stop |
| Hotkey Win+Alt+R | unavailable on this PC | Xbox Game Bar owns it (Settings → Hotkeys shows it red); rebind there (click the box, press e.g. Ctrl+Win+Alt+R) or use the Record button |
| Weather tab: current, 24-h strip, 7-day forecast, °C/°F (Open-Meteo) | auto-test (parser, WMO codes, formatting, cache/offline with fake source) + snapshot (dark/light, en/uk) + live API probe (request accepted, shape matches parser) | ok |
| Weather city search (geocoding) and approximate IP location | auto-test (parsers) + live API probe + snapshot | ok; in-app: manual-needed — Weather tab → pencil → type "Kyiv" → Enter → pick a result; the forecast appears within a few seconds |
| Temperature in idle pill | snapshot | ok |
| Rain alert banner (once per event) | auto-test (detection, rounding, hourly fallback, once-per-event) + snapshot | ok; real rain: manual-needed — wait for a rainy day with the module on |
| Weather offline cache | auto-test (restart reads cache, failed refresh keeps it, corrupt cache ignored) + snapshot | ok |
| AI providers: Anthropic (official SDK, streaming, refusal fallbacks) and OpenAI-compatible SSE; readable 401/429/5xx/network errors | auto-test (request building, SSE parsing, SDK stream via fake HTTP, error mapping) | ok; real Anthropic/OpenAI call: manual-needed — AI tab → key icon → paste your key → Save → "Test connection" shows "Connection works." |
| AI chat tab: multiple chats, context kept, streaming, Stop, Copy, per-chat system prompt, Markdown (code, lists, bold) | auto-test (Markdown parser, history building) + snapshot (dark/light, en/uk, 11 states) + live against a local fake OpenAI-compatible SSE server (question → "Open in chat" → follow-up sent with the full history) | ok |
| AI keys DPAPI-encrypted, never logged | auto-test (Secret roundtrip) + log inspected after live runs (only "ai request (OpenAi)") | ok |
| AI attachments: images (vision), PDF (document / extracted text), .docx, .xlsx, text/code; size limits with messages | auto-test (docx/xlsx/PDF extraction, type detection, limits, unreadable/missing files) + snapshot | ok; real upload: manual-needed — AI tab → paperclip → pick a PDF → ask "summarise" |
| AI text actions (Win+Alt+A): copy selection (clipboard saved/restored, not added to history), Summarise / → EN·UK·NL / Fix / Explain / Reply, Copy / Replace selection | auto-test (prompts) + snapshot + live (WinForms text box: selection captured, streamed from fake server, Replace pasted the result, clipboard restored to its previous text, history unchanged) | ok |
| Command bar "? question" → short streamed answer, Copy / Open in chat | snapshot + live (Alt+Space, typed "? how many km in a mile", Enter → popup streamed; Open in chat created a stored chat) | ok |
| Perf M11: idle with all modules, 60 s | live | 0.39 % of one core, 22.7 MB working set, 35.9 MB private; start-up 442 ms |
| Peak RAM while AI streaming | live (fake server) | text action: 72 → 92 MB peak, 9 MB 6 s after (trim); chat tab: 114 → 116 MB peak, 23 MB 8 s after |
| Voice: model download (official whisper.cpp repo, size + SHA-256 check, `.part` + move, progress pill) | auto-test (fake HTTP: ok / bad checksum / truncated / HTTP error, `.part` cleaned) + live (real 148 MB `ggml-base.bin` downloaded in 5 s and verified) + snapshot | ok |
| Voice: Whisper transcription offline (Whisper.net, auto language with EN/UK/NL preferred) | auto-test (language choice, transcript cleanup, resampling, level/silence) + live `--transcribe` on a 6 s TTS English WAV (exact text; also from the single-file publish) | ok; Ukrainian/Dutch speech: manual-needed (no UK/NL TTS voices on this PC) — hold Win+Alt+V in Notepad, say a Ukrainian sentence, release |
| Voice: hold Win+Alt+V → recording pill with level waveform + elapsed → release → transcribe → paste into the previous window (clipboard restored) | snapshot (recording / transcribing / download / no-mic, dark/light, en/uk) + live (scripted hold in a WinForms target: pill shown, real mic captured — quiet room peaked −51 dBFS → "Didn't hear anything", clipboard and foreground unchanged) | ok; speaking into the mic: manual-needed — open Notepad, hold Win+Alt+V, say "testing one two three", release; the text appears in Notepad within ~2 s (first use downloads the model first) |
| Voice: engine unloaded after 2 min idle | live (`--transcribe --unload`) | ok — unloaded after 2 min; working set 400 MB peak → 9 MB |
| Agents: hook JSON (PermissionRequest allow/deny, AskUserQuestion answers via updatedInput, no-decision cases) | auto-test | ok |
| Agents: pipe protocol — real client vs real server (allow/deny, delayed answer, question, status no-wait, fail open without island, hook timeout cancels the server wait) | auto-test | ok |
| Agents: Isle.Hook.exe ↔ running Isle (status event, waiting pill, timeout → no output, card removed; Allow pressed in the tab → `{"behavior":"allow"}` on stdout in ~35 ms) | live (UI Automation) | ok |
| Agents: tab cards, question chips, empty/setup, hooks panel, install confirm, pills (waiting/working/done) | snapshot (dark/light, en/uk) | ok |
| Agents: install/remove hooks (backup, merge, only Isle entries removed, invalid JSON never overwritten, missing file created) | auto-test (temp configs) | ok |
| Agents: real Claude Code / Codex session through the installed hooks | manual-needed | Agents tab → plug icon → Claude Code → Install → Confirm; start a new `claude` session, ask it to run `dir`; the island shows "needs you" with Allow/Deny — press Allow and the command runs. Remove restores the file (a backup `settings.json.isle-backup-*` is kept). Same for Codex. |
| Characters: three original creatures (Pebb, Sprig, Kubo), every mood (awake, blink, dance, happy, asleep) | snapshot (`chars-sheet`, dark/light) | ok |
| Characters: mood rules (happy > asleep > dancing > awake), eye offset, blink, motion ranges, ≤ 30 fps | auto-test | ok |
| Characters: gallery (pick, show in idle pill, nap delay) + panel mascot + idle-pill mascot | snapshot (en/uk, dark/light) + live (pill mascot drawn and animating) | ok |
| Characters: idle cost | live (60 s): 0.26 % of one core by default, 1.2 % of one core with the pill mascot | ok |
| Characters: dance to real music, fall asleep after N min, hop when a timer ends | auto-test (logic) | manual-needed — open the panel, click the character, turn on "Show in idle pill"; play music → it bounces; start a 1-min timer ("1m") → it hops when it ends; leave the PC for the nap time → it sleeps, move the mouse → it wakes |
| Settings panel inside the island (gear + tray "Settings"): General / Modules / Options / Hotkeys / About | snapshot (6 states × dark/light × en/uk) + live (UI Automation: gear → sections → Light theme → Українська; settings.json saved, applied live) + auto-test (every section builds in both languages, labels exist) | ok |
| Settings: module switches start/stop modules; options that a module reads at start restart that module | auto-test (ModuleHost) + code path | ok; manual-needed — Settings → Modules → switch Weather off → its tab disappears; on → it returns |
| Settings: hotkey rebinding (click the box, press keys; Backspace = off; reset; conflicts shown red) | auto-test (key press → gesture text) + snapshot | manual-needed — Settings → Hotkeys → click "Win+Alt+R" → press Ctrl+Win+Alt+R → the red note disappears; press it → recording starts |
| Settings: UI scale slider live preview, monitor choice (only with 2+ monitors), click-through switch reachable while on | snapshot + manual-needed | Drag Size to 150 %; with a second monitor pick "2" → the island moves there |
| Ukrainian translation review (all 650+ strings; consistent "папка", typographic apostrophes) | auto-test (key parity) + snapshot sheets (104 states) | ok |
| All UI states in both themes and both languages | snapshot (`--snapshot all`: 104 states × Dark/Light × EN/UK = 416 PNGs, inspected as contact sheets) | ok (switches that start "on" render "off" in snapshots only — template animation; live they are correct) |
| `build.ps1` → `dist\Isle\Isle.exe` (framework-dependent, 69 MB) and `dist\Isle-selfcontained\Isle.exe` | live | both start and show the pill; Whisper `--transcribe` works from both (self-contained needed loose native DLLs — fixed) |
| Perf M15: published exe, idle, all 16 modules, 60 s | live | 0.29 % of one core (0.024 % of machine), 25 MB working set, 38 MB private; start-up 365–527 ms (both variants); the very first launch of a freshly built exe took 6 s (69 MB) / 29 s (318 MB self-contained) while Windows security scanned the new file — later launches are fast |
| 10-minute soak (music play/pause on a real SMTC session, timers + finished banners dismissed, new notes typed, clipboard copy every cycle, mute on/off, settings sections, switching all 10 tabs) | live (`scratch\soak.ps1`, 77 cycles in 603 s) | ok — no errors/unhandled exceptions in log.txt; private bytes steady 55–86 MB (no trend), managed heap 7 MB after each trim, working set 14.8 MB 20 s after the last collapse |
