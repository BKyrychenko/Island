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
