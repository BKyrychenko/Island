# DECISIONS

One line each: decision — why.

## M0
- Repo root is the solution root (`Isle.sln` next to `SPEC.md`) — the working folder already is the dedicated project folder.
- `Isle.sln` in classic `.sln` format (not `.slnx`) — matches the spec's file name and older tooling.
- Tray icon via raw `Shell_NotifyIcon` + native popup menu, not WinForms `NotifyIcon` — avoids loading WinForms (RAM budget).
- Tray icon pixels are generated in code (`TrayIcon.Render`) — original artwork, no asset files needed.
- Window is a WPF layered window (`AllowsTransparency`) — per-pixel alpha gives click-through outside the shape for free.
- **Software rendering by default** (`RenderOptions.ProcessRenderMode = SoftwareOnly`, option `render.hardware` to switch) —
  measured on this machine (i5-12450H, Intel Xe): hardware mode loads the Intel driver (`igd12dxva64` + `igc64`) and idles at
  157 MB working set / 109 MB private; software mode idles at 76 MB / 32 MB. The window is small (640×300 DIP) and layered
  windows need a GPU→CPU read-back anyway, so software rasterisation is the cheaper path. Smoothness to be measured in M1.
- `Memory.TrimSoon()` (full GC + `EmptyWorkingSet`) runs once after start-up and will run after heavy features finish —
  spec requires memory to be released after heavy use. Measured idle after trim: 8 MB working set, 31 MB private, 0 ms CPU / 30 s.
- GC: workstation, non-concurrent — no background GC thread, lower idle footprint.
- `TreatWarningsAsErrors` on for the app project — spec §5.
- Settings: typed core fields + a free-form `Options` string dictionary for per-module options — avoids touching the
  settings class and migration for every small module option.
- No NuGet packages in the app so far. Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk (standard test stack).
- `logs/` is git-ignored — it holds the owner's run logs and my verification screenshots (`logs/shots`).
- graphify: the preinstalled CLI shim was broken and pip could not build it on Python 3.15, so it was reinstalled with `uv tool install graphifyy --python 3.12`. Graph is rebuilt with `graphify update .` (AST only, no LLM, seconds); only `GRAPH_REPORT.md` is committed, the rest of `graphify-out/` is regenerated locally. `.graphifyignore` excludes bin/obj/dist/logs.

## M1
- Expand/collapse uses a hand-written damped spring (`Core/Spring`, semi-implicit Euler, 240 Hz sub-steps) on `CompositionTarget.Rendering`, subscribed only while moving — easier to retarget mid-flight (hover in/out quickly) than storyboards; expand k=170/c=20.5 (≈400 ms, ~2 % overshoot), collapse k=290/c=30.6 (≈260 ms).
- The shape is a `RectangleGeometry` clip on the content + three stacked translucent shadow paths (no `DropShadowEffect`, which would force an offscreen blur pass every frame in software rendering).
- Hover cursor timer (30 ms) runs only while the pointer is engaged or the panel is open; it stops itself when idle — idle CPU stays ~0.
- Fullscreen check: event-driven on foreground change (`SetWinEventHook`) plus a 2 s check only while "hide in fullscreen" is on (catches F11 without a foreground change; spec allows ≥1 s polling).
- Text boxes take focus by activating the window on click (`AcquireFocus`); focus is handed back to the previous window on collapse.
- Esc is a temporary global hotkey registered only while expanded (the island is not activated, so it gets no key events otherwise).
- `IslandWindow.Foreground` renamed to `Watcher` (hid `Control.Foreground`); `Views.Icon` is fully qualified inside the window (clashes with `Window.Icon`).
- Toggle hotkey (Win+Alt+Space) expands/collapses the panel and un-hides a user-hidden island; tray Show/Hide uses a "user" hide reason so it composes with fullscreen/excluded hiding.
- Snapshot renderer: off-screen `IslandWindow(snapshotMode: true)`, settings changed in memory only, rendered at 192 DPI on a grey backdrop. States are a registry (`SnapshotStates`) so later modules add theirs.

## M2
- GSMTC via the built-in WinRT projection of the `net10.0-windows10.0.19041.0` TFM — no NuGet package needed.
- Session choice: playing > system "current" > most recently active (`PlaybackMath.Choose`, tested); re-evaluated on every play-state change so starting another player switches over.
- Paused music keeps the pill for 2 minutes, then the pill returns to idle (the tab still shows the paused track) — a permanently paused player should not occupy the pill forever.
- Track banner (title + artist, 2.2 s) is shown the first time a track is seen *playing* (a track that appears paused gets it when it starts).
- Album art decoded at 192 px wide (enough for the 104-DIP art at 175 %), frozen; dominant colour = score-weighted average of the most saturated mid-light pixels of a 16×16 downsample; greyscale art → default blue.
- Accent brushes are replaced (not mutated) on every change and their colour animation is started before they enter `Application.Resources` — the dictionary freezes plain Freezables, and frozen brushes cannot animate (found live).
- Lyrics: LRCLIB `/api/get` (needs artist + duration) then `/api/search` with a cleaned title ("feat.", "Official Video", "Remastered" removed); search hits must be within 5 s of the track length; synced beats plain. "Not found" is cached for 7 days, network errors are never cached. User-Agent `Isle/1.0 (Windows desktop overlay; personal use)` (no personal data).
- Lyrics are fetched only when the lyrics view is open, or when option `lyrics.alwaysFetch` / `lyrics.inPill` is on. The HttpClient is disposed after 2 minutes idle.
- Lyrics are a separate module (`lyrics`) so the network part can be switched off; the Music tab says so when it is off.
- Equalizer animates at 20 fps (each frame repaints the whole layered window); measured cost while music plays collapsed: ~0.35 % of the machine.
- Live testing without sound: `scratch/fake-session.ps1` (outside the repo) creates a real SMTC session with a silent WAV at volume 0 and display properties set on a `MediaPlaybackItem`.

## M3
- Timers and reminders are one module (`timers`) with one tab — they share the input box ("10m" starts a timer, "tomorrow 9:00 …" sets a reminder), so separate modules would only duplicate plumbing.
- Timer input: a bare number means minutes ("90" = 1 h 30 min), "1:30" means m:ss, "1:02:03" h:mm:ss; trailing words become the label ("25m focus").
- No polling: one DispatcherTimer aimed at the next end/due moment (capped at 30 min, re-checked on resume from sleep and on system time change); a 1 s display tick only while a countdown is running.
- Absolute end/due timestamps in `timers.json` / `reminders.json`; anything that ended while Isle was closed is shown as finished/fired at start.
- Reminder parser: own table-driven regex rules (no NLP library), EN + UK, 60+ phrases tested at a fixed UTC clock. Defaults: date without a time → 09:00; "morning" 09:00, "afternoon" 14:00, "evening" 19:00, "tonight" 20:00, "night" 21:00; "at 5" without am/pm → the next of 05:00/17:00 (with a day given, hours ≤ 7 mean pm).
- Ukrainian "за" is "in" only when nothing else fixes the time ("за 10 хвилин …"); with an explicit time it is ordinary text ("звіт за тиждень").
- Finished-timer and reminder banners don't expand on hover (`PillItem.HoverExpands = false`) so their buttons can be clicked in the collapsed pill; clicking the banner background opens the tab.
- Attention: `MessageBeep` (asterisk for timers, exclamation for reminders) + `IslandWindow.Pulse(3)` (velocity kicks on the shape springs); `TimerFinished` is published on the EventBus for the characters (M14).
- Snooze moves the due time; "Done" on a repeating reminder advances it with `RepeatRule.Next` (wall-clock arithmetic in the local zone, DST-safe).

## M4
- Notes content is stored as WPF's own XAML clipboard format (`TextRange.Save(..., DataFormats.Xaml)`) inside notes.json plus a plain-text shadow; on load the editor's inherited colour/font/margins written by `TextRange.Save` are cleared so theme changes apply to old notes (heading sizes kept). Round trip tested (bold, underline, heading, numbered list).
- Checkbox items are text glyphs ☐/☒ at the start of a paragraph (click toggles, Enter continues the list, Enter on an empty item ends it) — survives XAML/plain-text round trips and search without embedded controls. ☑ is avoided because WPF falls back to the colour emoji font for it.
- Folder picker and "move to folder" are an in-panel overlay list (no WPF Popup/ComboBox windows, which would fight the non-activating island).
- Keyboard focus for text boxes: `AcquireFocus` removes `WS_EX_NOACTIVATE` while a text box is in use (restored on collapse/deactivation), then SetForegroundWindow + Activate + SetFocus; the editor is focused only once it is actually visible (the panel is collapsed during the first frames of the expand animation). Found live: with NOACTIVATE kept, the window came to the front but key presses never reached it.
- Autosave 500 ms after the last edit, also on note switch, tab hide and module stop; the last opened note is remembered (`notes.last`).

## M5
- NuGet `System.Management` (Microsoft) for WMI brightness (`WmiMonitorBrightness` / `WmiSetBrightness`) — the spec names WMI and there is no WinRT brightness API for desktop apps; loaded only when the Control tab opens; writes are coalesced during slider drags. Works on this laptop (read 43 %).
- Core Audio volume via hand-written `[ComImport]` interfaces (no NAudio for this part): `IAudioEndpointVolume` + `IAudioEndpointVolumeCallback` for external changes + `IMMNotificationClient` for default-device changes; our own changes use a context GUID so they don't echo.
- Bluetooth headphones: Windows has no public "connect" API for audio profiles (`AudioPlaybackConnection` is for the PC acting as a speaker). Implemented the approach the Settings app uses: find the audio endpoint whose KS filter id contains `BTH…` + the device address, then `IKsControl.KsProperty(KSPROPSETID_BtAudio, ONESHOT_RECONNECT / ONESHOT_DISCONNECT)`. Paired devices are listed with WinRT (`BluetoothDevice`). Connect/disconnect not exercised here (would hijack the owner's audio) → manual-needed.
- VPN: Windows profiles from `rasphone.pbk` (user + all users) with `rasdial` connect/disconnect and status from an up PPP adapter of the same name (no PowerShell `Get-VpnConnection`, which costs ~1 s per call). Clients detected by install path and adapter name/description; CLI toggles for Tailscale (`up/down`), Cloudflare WARP (`warp-cli connect/disconnect`), Mullvad (`mullvad connect/disconnect`), NordVPN (`-c/-d`); WireGuard, OpenVPN and Proton are status + "Open app". Elevation errors are detected (exit code 5 / 740 / "administrator" in output) and shown as a message, never retried elevated.
- Control services (audio COM objects, radios, WMI) are created on the first open of the tab, not at start-up.
- Fullscreen detection fixed (found live): a maximized Chrome/Edge window has no `WS_CAPTION`, so the old "covers the screen and has no caption" rule hid the island behind every maximized browser. Now: `SHQueryUserNotificationState` busy/D3D/presentation → hide; otherwise only a non-maximized (`IsZoomed` false) window covering the whole monitor counts.
- Tab restore: the remembered last tab wins over whichever tab registered first, until the user picks a tab in this session (found live: modules register tabs one by one).

## M6
- One module `live` hosts all live-event sources; each source has its own option `live.<name>` (downloads, miccam, notifications, obs, steam, vpn, layout, caps — all on by default except notifications) and is constructed only when enabled. Toggling an option starts/stops just that source.
- Sources talk to an `ILiveSink` (the pill queue + timed "flash" items) so they can be tested with a fake sink on a private STA dispatcher.
- Downloads: `FileSystemWatcher` on Downloads for `*.crdownload/*.part/*.partial/*.download`; a 1 s size sample runs only while a partial file exists; a leftover partial file from an old, abandoned download is shown only once it grows; rename to the final name → 4 s "Downloaded" flash (click selects the file in Explorer). Chrome's partial files carry no total size, so progress is size + speed (spec allows this).
- Mic/camera: consent store read on `RegNotifyChangeKeyValue` (whole subtree, thread-pool wait, 300 ms debounce) — no polling. Verified live with a temporary NonPackaged entry.
- Windows notifications: `UserNotificationListener.RequestAccessAsync` throws `0x80070490` (element not found) for this unpackaged app. A sparse MSIX package would need a signing certificate trusted in the machine store (admin) — not done; the source stays off by default and is marked unavailable.
- OBS: `ClientWebSocket` to obs-websocket v5, subscribed only to Outputs events; the 15 s probe first checks for an `obs64`/`obs` process so nothing connects while OBS is closed. Password stored DPAPI-encrypted (`Core/Secret`, NuGet `System.Security.Cryptography.ProtectedData` from Microsoft — also needed for the clipboard in M7).
- Steam: `SteamPath` from HKCU, libraries from `libraryfolders.vdf` (new and old format), `FileSystemWatcher` on `appmanifest_*.acf`; own tiny KeyValues parser. Downloading = StateFlags has UpdateRunning (1024) or Downloading (1<<20), or UpdateStarted with bytes left.
- Keyboard layout: checked on `EVENT_SYSTEM_FOREGROUND` (shared `ForegroundWatcher`) plus the 500 ms check the spec allows while the option is on (layout switches inside a window raise no cross-process event).
- Caps Lock: `WH_KEYBOARD_LL`; the callback only compares the vk code and posts to the UI thread.
- Progress bar in pills uses a `ScaleTransform` (found in snapshots: width computed from `ActualWidth` before layout was NaN).

## M7
- Clipboard listener: `AddClipboardFormatListener` on a message-only `HwndSource` (HWND_MESSAGE) — event-driven, no viewer chain. A locked clipboard (COMException) is retried up to 4× at 80 ms via a DispatcherTimer, never by sleeping.
- Privacy: skipped when the clipboard has `ExcludeClipboardContentFromMonitorProcessing` or `Clipboard Viewer Ignore`, or `CanIncludeInClipboardHistory` = 0 (if that value can't be read, the content is treated as private). Excluded apps = the island's ExcludedApps + option `clip.excludedApps`, matched against the clipboard owner's process (fallback: foreground process). Content is never logged.
- Storage: `clipboard.json` holds text only as DPAPI (CurrentUser, app entropy) ciphertext; plaintext lives in memory only. Text dedupe is done in memory (no content hash on disk). Images are PNGs in `clip\`, deduped by SHA-256 of the pixels; clipboard DIBs with all-zero alpha are made opaque. Text > 1 M chars and images > 40 MP are ignored.
- "Clear" removes unpinned entries only — pinned entries never expire, per spec. Limit option `clip.limit` (default 200, clamped 10–5000).
- Paste: put the entry on the clipboard, collapse, `SetForegroundWindow(PasteTarget)` if needed, 90 ms later `SendInput` Ctrl+V (physically held Shift/Alt/Win are released first), 600 ms later the previous clipboard (text/image/file list) is restored. Our own writes are ignored via `GetClipboardSequenceNumber`. Verified live into a WinForms text box.
- The list shows at most 60 rows (search reaches the rest) and is rebuilt only while the tab is visible.
- Shelf keeps references only (never copies files). Drag-in: the island window has `AllowDrop`; drag-over a FileDrop expands to the Shelf tab (sticky) and shows an opaque "Drop to shelve" hint; transparent areas still pass drags through to windows below (layered window). Drag-out: `DoDragDrop(FileDrop)` with Copy|Move|Link while `HoldOpen("shelf-drag")` keeps the panel open; drops onto the island itself are refused during our own drag. "Remove after drag-out" is option `shelf.removeAfterDrop`.
- Shelf tiles: selection + toolbar (open / copy path / remove) instead of per-tile hover buttons — fewer elements and works with the 76-px tiles; double-click opens; a missing file shows "missing" and Open goes to its folder if that still exists. Icons via `SHGetFileInfo`, images get a decoded 80-px thumbnail; cached per path.

## M8
- Command bar = an overlay over the whole expanded panel (`IslandWindow.ShowOverlay`), not a tab: it has no place in the tab bar and must cover it. Opening = `Expand(sticky)` + `AcquireFocus`; collapsing hides the overlay and gives focus back (that also returns the foreground to the previous window before a launched app appears).
- Providers run on the thread pool (`Task.Run`) and merge into the list as each finishes (fast local results first, currency later). The best result stays selected until the user presses Up/Down — found live: keeping the first-arriving result selected made Enter run the web-search fallback instead of the system command.
- System commands (lock, sleep, restart, shut down, empty Recycle Bin) need a second Enter ("Press Enter again to confirm"); covered by an xUnit test, deliberately not exercised live.
- Apps: `shell:AppsFolder` via `Shell.Application` on a short-lived STA thread (desktop *and* Store apps — Windows 11 Notepad/Calculator/Terminal have no Start-menu .lnk) + Start-menu shortcuts (all users + user) + 150 most recent files + folders from option `cmd.folders` (';'-separated, depth <= 6, <= 30 000 entries, skips hidden/system/node_modules/.git/bin/obj). Built in the background when the bar opens and the cache (`index.json`) is older than 10 min; the open bar re-queries when the build finishes. Not incremental per file — a full background rescan is cheap at this size.
- Fuzzy score: prefix 1000 > later word prefix 850 > acronym 800 > substring 600 > subsequence ~300, minus name length; apps +60, recent -40, files -120.
- Calculator: own recursive-descent parser (no eval): + - × ÷ ^ (right-assoc, -2^2 = -4), postfix % and !, implicit multiplication, functions sqrt/cbrt/sin/cos/tan/asin/acos/atan/log/ln/exp/abs/round/floor/ceil/min/max, pi/e/tau, "x% of y" (also "від"), thousands "1,000"/"1 000", decimal comma "3,5". Plain numbers / constants alone are not shown. Units: length, mass, temperature, data; KB/MB/GB are decimal (1000), KiB/MiB/GiB binary. Enter copies the plain invariant value.
- Currency: National Bank of Ukraine rates (`bank.gov.ua ... exchange?json`, free, no key) instead of Frankfurter/ECB because ECB has no UAH and the owner's main pair is UAH; all conversions go through UAH. Cached in `rates.json` for 12 h; offline -> cache of any age; the rate's date is shown. HTTP via `Core/IdleHttp` (lazy client, disposed after 2 min idle).
- `? question` publishes `AskAi` on the EventBus (the AI module subscribes in M11); until then the row says AI chat is not set up and does nothing.

## M9
- Selection: everything works on a frozen GDI capture (BitBlt + CAPTUREBLT, physical pixels; the process is PerMonitorV2). One borderless topmost window per monitor, placed with SetWindowPos in physical pixels; its content is a Canvas in that monitor's pixels inside a `Viewbox Stretch=Fill`, so 1 canvas unit = 1 physical pixel whatever WPF thinks the DPI is (mixed-DPI safe). A selection stays inside the monitor where it started. Drag = region, click = topmost window under the cursor (DWM extended frame bounds, cloaked windows skipped), Enter = whole monitor, Esc/right-click = cancel.
- The island is excluded from our own captures with `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` only while capturing/recording (so other tools can still screenshot it).
- Colour picker reads pixels from the frozen capture (no live GetPixel); loupe = 11×11 px magnified with nearest-neighbour; last 12 colours in option `tools.colors`.
- OCR: `Windows.Media.Ocr` with the language from option `ocr.lang` (chips for every installed recognizer language), else the user-profile languages; selections smaller than 300 px are upscaled ×2; engine created per use. Win+Alt+Q tries QR first and falls back to OCR; the Tools tab has separate Text and QR buttons.
- QR: ZXing.Net (managed, Apache-2.0), TryHarder + inverted. Content is shown first; "Open link" exists only for http(s) and opens only on click.
- Markup: own window per screenshot (not inside the island — it needs real keyboard focus and can be large), image at 1:1 physical pixels unless it does not fit (85 % of the monitor). Annotations are WPF shapes in image-pixel space; export = RenderTargetBitmap of the canvas at 96 dpi (exact pixels). Pixelate replaces the area with a 1/12-scale copy drawn nearest-neighbour. Saves to `Pictures\Isle\Isle yyyy-MM-dd HH.mm.ss.png`.
- Recording: ScreenRecorderLib was tried first and rejected — its package is a .NET Framework 4.8 C++/CLI assembly that cannot load in .NET 10. Own pipeline instead: Windows.Graphics.Capture (monitor item via `IGraphicsCaptureItemInterop`) → region copied into a D3D11 texture (Vortice.Direct3D11, MIT) → pooled copies as `MediaStreamSample`s → `MediaTranscoder` (H.264 + AAC in MP4). Audio: NAudio.Wasapi (MIT) loopback + default communications mic, resampled to 48 kHz stereo and mixed, 20 ms PCM blocks paced to the clock. 30 fps, output scaled to ≤ 1080 p, bitrate ≈ w·h·fps/8 (2–12 Mbit/s). On this PC the transcoder rejects hardware-accelerated processing ("Unknown") → automatic fallback to its software processing path.
- Recording cost here (1920×1080, system audio): ~38–44 % of one core (of 12 logical), peak working set ~580–600 MB (only 3 × 8 MB textures are ours, the rest is Media Foundation's software path); after Stop the process is back at ~19 MB (GC + trim). Spec allows heavy use only while active; the release is verified.
- Win+Alt+R is owned by Xbox Game Bar on this machine (registration fails, logged, non-fatal); the Tools tab Record button and a rebinding (Settings.Hotkeys["record"]) work. Win+Alt+S/Q/C register fine.
- Windows.Graphics.Capture on Windows 11 draws a yellow border around the captured monitor while recording (`IsBorderRequired` is not in the 19041 projection) — acceptable as a recording indicator.

## M10
- Weather source: Open-Meteo forecast + geocoding (free, no key) in one request (`current`, 25 `hourly`, 7 `daily`, 12 × `minutely_15` precipitation, `timezone=auto`, `timeformat=unixtime`). Times stored as Unix seconds and shown in the place's own UTC offset; temperatures stored in °C and converted for display (°F also switches wind to mph).
- Approximate location: geojs.io (`get.geojs.io/v1/ip/geo.json`, HTTPS, no key) — only when the "Approximate location (IP)" chip is on (off by default); on failure the last stored coordinates are used.
- No network at all until a place is chosen; then one refresh on start if the cache is older than 30 min and a 30-min `DispatcherTimer` while the module is enabled. HTTP through `IdleHttp` (client disposed 2 min after the last request).
- Place search lives in the Weather tab itself (pencil button) instead of the settings panel — the spec's settings panel is M15; options are kept in `Settings.Options` (`weather.*`) so M15 can show the same toggles.
- Rain alert: first 15-min slot ≥ 0.1 mm within 2 h while dry now (falls back to hourly ≥ 60 % wet code without minutely data), rounded to 5 min; "once per event" = no new banner unless the start is > 3 h from the last announced one (stored in `weather.lastAlert`). Banner is a transient pill item for 10 s; click opens the tab.
- Weather icons: original line drawings (`Wx.*` geometries in Icons.xaml) drawn with the shared `Icon` control.
- Cache `weather.json` (JsonStore, corrupt → backup + ignored) for offline display; offline state shows "Offline · Updated HH:mm" in the warning colour.

## Performance measurements
| When | State | Working set | Private | CPU |
|---|---|---|---|---|
| M0 | idle pill, tray only, software rendering, after trim | 8 MB | 31 MB | 0 ms over 30 s |
| M0 | same, before trim | 76 MB | 32 MB | — |
| M0 | hardware rendering (rejected default) | 157 MB | 109 MB | — |
| M1 | idle pill, tray, hover logic; 60 s after 1 expand/collapse | 38.7 MB | 41.3 MB | 63 ms over 60 s (0.009 %) |
| M1 | start-up to input-idle | — | — | 457 ms |
| M2 | music playing (fake SMTC session), pill collapsed, 20 s | 33 MB | — | 844 ms / 20 s (0.35 % of machine) |
| M2 | same with lyric line in the pill | 42 MB | — | 1219 ms / 20 s (0.51 %) |
| M2 | music tab expanded while playing | 55–60 MB | — | — |
| M6 | idle pill, all modules + all live sources on (notifications unavailable), 60 s after a download/Caps Lock run | 55.5 MB | 48.7 MB | 172 ms over 60 s (0.29 % of one core, ~0.04 % of machine) |
| M9 | recording 1920×1080 @30 fps + system audio (software MF path) | peak 580–600 MB | — | 38–44 % of one core |
| M9 | 4 s after recording stopped | 19 MB | — | — |
