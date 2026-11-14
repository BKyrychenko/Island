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
