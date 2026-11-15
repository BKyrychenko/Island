# VERIFICATION

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
| Tray icon + menu | manual-needed | Right-click the Isle icon in the notification area: Show/Hide, Settings, Start with Windows, Quit should work |
| Idle budget (M0) | live | 8 MB WS / 31 MB private / 0% CPU |
| Hover expand (120 ms intent) / collapse on leave (250 ms) | live (cursor driven by script, screenshots) | ok |
| Spring expand/collapse (~400 ms perceived, slight overshoot, collapse faster) | auto-test + live | ok |
| Tab bar with sliding indicator, label on selected tab, "more" overflow list, gear | snapshot (dark/light × en/uk, 150% scale) | ok |
| Toast inside the panel (text + action) | snapshot | ok |
| Themes Dark / Light / System (follows Windows live via WM_SETTINGCHANGE) | snapshot (dark/light); System: manual-needed | Switch Windows Settings → Personalisation → Colours → app mode; the island recolours within a second |
| UI scale 80–200% | snapshot at 150% | ok (settings UI arrives in M15; live preview there) |
| Auto-hide to screen edge (sliver, returns on top-edge touch) | snapshot (edge-hidden) | manual-needed for the live slide: enable `AutoHideEdge` in settings.json, move cursor away 1.5 s → island slides up; touch the top edge → it returns |
| Click-through mode (WS_EX_TRANSPARENT) | manual-needed | Set `"ClickThrough": true` in settings.json → clicks on the pill reach the window below |
| Fullscreen auto-hide | manual-needed | Play a video fullscreen / press F11 in a browser → island disappears; exit → it returns |
| Excluded apps | auto-test (name matching) + manual-needed | Add `"notepad"` to `ExcludedApps` → island hides while Notepad is in front |
| Global hotkeys infrastructure (RegisterHotKey, failures non-fatal) | auto-test (gesture parsing) + manual-needed | Press Win+Alt+Space → panel opens/closes |
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
| Live: OBS recording (WebSocket v5, auth) | auto-test (auth) + snapshot | manual-needed: OBS → Tools → WebSocket Server Settings → enable (port 4455; password goes into option `live.obsPassword`, DPAPI) → start recording → red dot + elapsed time |
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
| Command bar: configurable folders | manual-needed | Set option `cmd.folders` to e.g. `C:\Users\you\Documents` -> file names appear after the background index |
