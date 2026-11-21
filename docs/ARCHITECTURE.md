# ARCHITECTURE

Hand-written module map. One paragraph per module: what it owns, public surface, lifetime, data files.
Keep it short and current (update at the end of every milestone).

## Process shape
One WPF process (`src/Isle`, `Isle.exe`). `App.OnStartup` installs exception handlers, takes the single-instance
mutex, applies language and render mode, shows `IslandWindow`, and only after the first frame
(`DispatcherPriority.ApplicationIdle`) registers modules with `ModuleHost` and calls `Sync()`.
Static access points: `App.Host` (modules), `App.Island` (the window), `Settings.Current`.

## Shell
- **App** (`App.xaml.cs`) — start-up/shutdown, single instance (`Local\Isle.SingleInstance.*` mutex), global exception
  logging, `ToggleIsland()`, `Quit()`, repositions the island on resume from sleep.
- **IslandWindow** (`IslandWindow.xaml(.cs)` + partials) — the only always-present window. Fixed size (`BaseWidth×BaseHeight`
  × UI scale), transparent, topmost, `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`, optional `WS_EX_TRANSPARENT` (click-through).
  `Reposition()` puts it top-centre of the selected monitor (physical pixels). Partials:
  - `.Motion.cs` — springs (`Core/Spring`) for width/height/expansion/edge-shift stepped on `CompositionTarget.Rendering`
    only while moving; `ApplyShape()` writes the clip, shadow paths and content opacities (no layout, no window resize).
  - `.Hover.cs` — 30 ms cursor timer (runs only while engaged), hover intent/leave delay, `Expand(tab, sticky)`,
    `Collapse()`, `Toggle()`, `HoldOpen/ReleaseOpen(key)`, keyboard focus hand-over (`AcquireFocus`, `PasteTarget`),
    edge auto-hide, `SetHidden(reason, bool)` (user / fullscreen / excluded), foreground checks.
  - `.Content.cs` — pill (`Pill` = `Core/PillQueue`; primary view + compact secondary), tabs (`AddTab(TabInfo)`,
    `RemoveTab`, `SelectTab`, `RequestTab`; views created lazily, `ITabView.OnShown/OnHidden`), "more" overlay, `ShowToast`.
  - `.Snapshot.cs` — `--snapshot <state|all> <out.png|dir> [--theme] [--lang] [--scale]`; `SnapshotStates` registry.
  Public members used by modules: `Pill`, `Hotkeys`, `Watcher`, `IdleView`, tab API (`TabViewFor`), `ShowToast`,
  `Pulse`, `ExpandedChanged`. `PillItem.HoverExpands = false` for banners with buttons.
- **Native** (`Native.cs`) — P/Invoke declarations only.

## Core (`src/Isle/Core`)
- **Paths** — data root `%AppData%\Isle` (override: `ISLE_DATA_DIR` or `Paths.Root`), file/dir helpers.
- **Logger** — `log.txt`, rotates to `log.txt.1` at 1 MB, never throws. No secrets/user content.
- **JsonStore** — `Load<T>` / `Save<T>` / `WriteAtomic` (temp + replace) / `BackupCorrupt` (`*.bak-<timestamp>`). Every store uses it.
- **Settings** — `settings.json`; typed fields + `Modules` (id → enabled), `Hotkeys`, `Options` (free-form per-module
  strings). `Settings.Current`, `Save(changedName)` raises `Settings.Changed`. `Migrate()` upgrades old versions.
- **Strings** — EN/UK dictionaries (`Strings.Data.cs`), `Strings.T(key)`, XAML `{core:Loc key}` (live language switch).
- **EventBus** — typed publish/subscribe between modules and views.
- **ModuleHost / IModule** — modules are registered as factories, constructed and started only when enabled
  (`Sync()`), stopped/disposed when disabled; a throwing module becomes `Failed` without affecting others.
- **Screens** — monitor enumeration (physical pixels, DPI scale); `Selected(index)`.
- **Autostart** — `HKCU\...\Run` value `Isle`.
- **Theme** — Dark/Light/System palette as brushes in `Application.Resources` (`BgBrush`, `FgBrush`, `FgDimBrush`,
  `SurfaceBrush`, `AccentBrush`, …), `Apply()` swaps them live; `SetAccent(color)` animates the accent (album art).
- **Spring** — damped spring value used by the island's motion.
- **PillQueue / PillItem / PillPriority** — what the collapsed pill shows; pure `Select()` rule (tested).
- **TabInfo / ITabView** — tab registration record and the shown/hidden callbacks for tab views.
- **ForegroundWatcher** — `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`; `LastExternal` (paste target), `IsFullscreen`, `ProcessName`.
- **ClipboardOps** — snapshot/restore the clipboard, `SendCtrl(vk)`, `CopySelectionAsync`, `PasteTextAsync`, and
  `Quiet(span)` (clipboard history ignores changes meanwhile). Used by the clipboard module and AI text actions.
- **Memory** — `TrimSoon()`: GC + `EmptyWorkingSet` after start-up and after heavy features.

## Styles / Views
- `Styles/Theme.xaml` — the flat control set (FlatButton, AccentButton, GhostButton, IconButton, Switch, Chip, slider,
  FlatTextBox, scrollbars, FlatList, TabButton, Card, text styles). `Styles/Icons.xaml` — original 24×24 line icons.
- `Views/Icon` (draws an icon geometry), `Views/TabBar` (tab strip + sliding indicator + more + gear), `Views/IdlePillView`
  (optional clock, left/right slots for mascot and weather).

## Modules (`src/Isle/Modules`)
- **Media** (`media`, `Modules/Media`) — `MediaSession` wraps GSMTC (manager + chosen session events, marshalled to
  the UI thread) and exposes `IMediaSource` (`Current: MediaState`, `Changed`, `PlayPause/Next/Previous/Seek`).
  `MediaModule` puts the `music` pill item (`Views/Music/MusicPill`, compact equalizer for the side slot), the
  `music.track` banner, registers the Music tab (`Views/Music/MusicView`), sets `Theme.SetAccent` from the art, and owns
  the `LyricsTracker`. Pure helpers: `PlaybackMath` (extrapolation, formatting, app names, session choice), `ArtColors`.
  `FakeMediaSource` + `MediaSnapshots` provide snapshot states. Options: `lyrics.inPill`, `lyrics.alwaysFetch`.
- **Lyrics** (`lyrics`, `Modules/Lyrics`) — `LyricsService` (LRCLIB get→search, memory + disk cache in
  `%AppData%\Isle\lyrics\<hash>.json`, lazy HttpClient disposed after 2 min idle, `ILyricsHttp` seam), `Lrc` parser,
  `LyricsTracker` (lyrics for the current track, fetched only on demand).
- **Timers & reminders** (`timers`, `Modules/Timers`, `Modules/Reminders`) — `TimersController` (all operations,
  `Submit(text)` → timer or reminder, persistence to `timers.json` / `reminders.json`, `FormatDue/FormatRepeat`),
  `TimersModule` (wake-up timer aimed at `NextWake`, pill item `timer`, banners `t:<id>` / `r:<id>`, sound + pulse,
  publishes `TimerFinished`). Pure logic: `DurationParser`, `TimerBook` (+ stopwatch), `ReminderParser` (EN/UK rules),
  `RepeatRule`, `ReminderBook`. Views: `Views/Timers/TimersView`, `TimerPill` (+ `Ring`), `TimerDoneBanner`, `ReminderBanner`.
  The command bar (M8) uses `Controller.AddTimer` / `AddReminder`.
- **Notes** (`notes`, `Modules/Notes`) — `NotesStore` (notes.json: notes with XAML + plain shadow, one-level folders,
  pins, `Search`, `Delete`/`Restore` for Undo), `NotesModule` (tab, hotkey `newNote` = Win+Alt+N → `QuickCapture`).
  View `Views/Notes/NotesView` (list + RichTextBox editor; `Serialize`/`Load` helpers; autosave 500 ms).
- **Control Center** (`control`, `Modules/ControlCenter`) — `ControlModule` (tab; `NetworkChanged` from
  `NetworkChange.NetworkAddressChanged`), `ControlServices` (lazy: `AudioVolume` Core Audio COM, `Radios` WinRT,
  `Brightness` WMI, VPN environment), `WindowsTheme` (HKCU Personalize + WM_SETTINGCHANGE broadcast),
  `BluetoothDevices` (WinRT list + KS one-shot reconnect/disconnect), `VpnDetector` (pbk profiles, client detection, CLI
  runner). COM interfaces in `CoreAudio.cs`. View `Views/Control/ControlView` (tiles, sliders, in-panel overlay lists).
- **Live events** (`live`, `Modules/LiveEvents`) — `LiveEventsModule` owns small `ILiveSource`s, each switched by option
  `live.<name>` and constructed only when on: `DownloadsSource` (FileSystemWatcher on Downloads, `DownloadDetector`
  for Chrome/Edge/Firefox partial files, `SpeedMeter`), `MicCameraSource` (`RegistryWatcher` on the consent store,
  `ConsentStore` parser), `ObsSource` (WebSocket v5, `ObsAuth`), `SteamSource` (`Vdf` + `Steam` manifest parser, file
  watcher), `VpnIndicatorSource` (`NetworkChange`), `KeyboardLayoutSource` (foreground events + 500 ms check),
  `CapsLockSource` (WH_KEYBOARD_LL), `NotificationsSource` (unavailable unpackaged). They talk to the pill through
  `ILiveSink` (`PillSink`); views `Views/Live/IndicatorPill`. Options `live.obsPort/obsPassword/downloadsFolder`.
- **Clipboard** (`clipboard`, `Modules/Clipboard`) — `ClipboardService` (AddClipboardFormatListener on a message
  window, `ClipPrivacy`: exclusion formats + excluded apps), `ClipStore` (`clipboard.json`, text DPAPI-encrypted, images
  as PNG in `clip\`, dedupe, pins, limit `clip.limit`), `ClipboardModule` (tab `Views/Clipboard/ClipboardView`; click =
  `ClipboardOps.PasteTextAsync`/image paste into `PasteTarget`).
- **Shelf** (`shelf`, `Modules/Shelf`) — `ShelfStore` (`shelf.json`, references only, missing-file flag), `ShelfModule`
  (tab `Views/Shelf/ShelfView`, drag-out with `DoDragDrop` FileDrop, option `shelf.removeAfterDrop`); drops arrive via
  `IslandWindow.Drop.cs` (expands to the Shelf on drag-over).
- **Command bar** (`commands`, `Modules/Commands`) — `CommandModule` (hotkey `commandBar` = Ctrl+Alt+Space → full-panel
  overlay `Views/Commands/CommandBarView`, 80 ms debounce, providers run off the UI thread). Providers (`ICommandProvider`):
  `AppsProvider` (`AppIndex`: Start Menu shortcuts + recent files + `cmd.folders`, cached `index.json`; `Fuzzy` matcher),
  `CalculatorProvider` (`Calculator` + `Units`), `CurrencyProvider` (`CurrencyParser`, `CurrencyService` with
  `NbuRatesSource`, `rates.json`), `TimerProvider` (reuses `TimersController`), `SystemProvider` (confirm before
  lock/sleep/restart/shutdown/recycle bin; Settings pages), `WebProvider` (`g` search, URLs, `?` → `AskAi`).
- **Screen tools** (`tools`, `Modules/Tools`) — `ToolsModule` (hotkeys screenshot Win+Alt+S, capture Win+Alt+Q, colour
  picker Win+Alt+C, record Win+Alt+R; one tool at a time, island hidden from captures). `ScreenGrab` (physical-pixel
  capture across mixed-DPI monitors), `Views/Tools/SelectionOverlay` (region/window/fullscreen pick, loupe for the
  colour picker), `Views/Tools/MarkupWindow` (pen, highlighter, arrow, shapes, text, pixelate, undo/redo, copy/save),
  `QrReader` (ZXing) / `OcrReader` (Windows.Media.Ocr), `Recorder` (Windows.Graphics.Capture + Media Foundation H.264
  MP4, WASAPI system audio/mic via NAudio, 30 fps ≤ 1080p). Tab `Views/Tools/ToolsView` (tools, OCR/QR results, colour
  history `tools.colors`). Files go to `Pictures\Isle\` and `Videos\Isle\`.
- **Hotkeys** (`HotkeyService`, owned by the island, not a module) — `Register(id, titleKey, default, pressed, released?)`,
  `Rebind`, `List()` for the settings table; user overrides in `Settings.Hotkeys`; failures logged, never fatal.
  Hold-hotkeys poll the key state at 30 ms only while held. Default: `toggle` = Win+Alt+Space.
- **Tray** (`tray`) — notification-area icon (`Shell_NotifyIcon`, hidden message window), native menu:
  Show/Hide, Settings, Start with Windows, Quit. Icon drawn by `TrayIcon.Render`. Re-adds itself on `TaskbarCreated`.

- **Weather** (`weather`, `Modules/Weather`) — `WeatherModule` (tab, idle-pill right slot `WeatherPill`, rain banner
  `weather.rain`, 30-min refresh timer only once a place is set), `WeatherService` (last report + `weather.json` cache,
  `IWeatherSource` seam), `OpenMeteoSource` (forecast, geocoding, geojs.io IP location), pure `OpenMeteoParser`,
  `WeatherCodes` (WMO → text/icon), `WeatherFormat`, `RainAlerts`. View `Views/Weather/WeatherView` (report with 24 h /
  7 days, setup with search + option chips). Options: `weather.city/lat/lon/ip/units/inPill/rainAlerts/lastAlert`.

- **AI chat** (`ai`, `Modules/AiChat`) — `AiModule` owns chats, cancellation and one active request at a time.
  `ChatStore` stores up to 100 chats in `chats.json`. `CodexChatProvider` and `ClaudeCodeProvider` use `ChatCli`
  to run the installed CLIs in background processes with redirected input and output. Codex uses its stdio app-server
  for streaming, with a one-shot CLI fallback before sending when the installed configuration is incompatible. `ai.provider` selects
  `codex` or `claude`. `Attachments` handles file input (file →
  image/PDF/text attachment, docx/xlsx via Open XML zip, PDF text via PdfPig), `Markdown` (block + inline parser).
  Views `Views/AiChat/AiChatView` (header, bubbles, chips, input, overlays) and `MarkdownView` (renderer).
  Text actions: hotkey `textActions` (Win+Alt+A) → `ClipboardOps.CopySelectionAsync` → `AiPopupView` (island overlay)
  with `TextActions` prompts. Copy is available for usable results. Replace uses `ClipboardOps.PasteTextAsync`
  after a complete response for the full selection. Command bar
  `? question` → `AskAi` on the EventBus → `AiModule.Ask` → the same popup; "Open in chat" stores it as a chat.

- **Voice to text** (`voice`, `Modules/Voice`) — `VoiceModule` (hold hotkey `voice` = Win+Alt+V: press → `MicCapture`
  + `Views/Voice/VoicePill` level waveform; release → `WhisperEngine.TranscribeAsync` → `ClipboardOps.PasteTextAsync`
  into the window that was active; first press without a model → `ModelDownloader` with a progress pill). `MicCapture`
  (NAudio WASAPI → mono → 16 kHz), `AudioMath` (level, silence, resample), `WhisperEngine` (`ITranscriber`; Whisper.net
  loaded on demand, unloaded 2 min after use), `LanguagePick`, `TranscriptText`, `WhisperModels` (base/small, URL, size,
  SHA-256; files in `%AppData%\Isle\models\`). `VoiceSelfTest` = debug CLI `--transcribe`. Options `voice.model`, `voice.lang`.

- **Agents** (`agents`, `Modules/Agents` + `src/Isle.Hook`) — `Isle.Hook.exe <claude|codex> [--timeout s] [--async]`
  is registered as a command hook, reads the hook JSON from stdin and sends it over the named pipe
  `Isle.Agents.v1.<user>` (`HookProtocol`: one JSON line each way, `BuildOutput` makes the hook's stdout; fails open).
  `AgentPipeServer` (accept loop, per-connection serve, disconnect watch) → `AgentsModule.Handle` → `AgentBook`
  (sessions keyed by agent + session_id, states idle/working/waiting/done, pending `AgentRequest`s with a reply TCS).
  View `Views/Agents/AgentsView` (cards with Allow / Deny / In terminal, question option chips, hooks panel with
  plan + confirm), pill `AgentPill` (dot: working / waiting / done check). `HookInstaller` (plan, install, remove,
  backup; helper copied to `%AppData%\Isle\bin`). Options `agents.approvals`, `agents.timeout`.

- **Characters** (`characters`, `Modules/Characters`) — `CharactersModule` (panel mascot via `IslandWindow.Tabs.Mascot`,
  optional idle-pill mascot via `IdleView.LeftSlot`, gallery overlay `Views/Characters/CharacterGallery`; subscribes to
  `MusicPlaying` (media module) and `TimerFinished`), `CreatureBrain` (pure mood + motion math), `CreatureDesign`
  (Pebb / Sprig / Kubo frozen shapes), `Views/Characters/CreatureView` (OnRender drawing, own adaptive clock, paused
  while hidden). Options `chars.kind`, `chars.inPill`, `chars.sleepMin`.

### Developer modules
On by default only in the developer edition (`-p:Edition=Dev` → `DEV_EDITION` → `Core/Edition.IsDev`); the standard
edition lists them in Settings, switched off. Shared helpers: `Core/Cli` (run a console tool, capture stdout, timeout),
`Core/ProcessOps` (end a process if its name still matches, open with the shell).

- **System** (`sysmon`, `Modules/SystemMonitor`) — `SystemSampler` (GetSystemTimes, per-core
  `NtQuerySystemInformation`, `GlobalMemoryStatusEx`, network interface counters, PDH for GPU engines, GPU memory and
  disk; DXGI for total VRAM), `SystemMath` (pure), `SustainedAlert` (threshold held for a while, then a cooldown).
  View `Views/SystemMonitor/SystemMonitorView` samples once a second only while shown. Option `sysmon.alerts`.
- **Ports** (`ports`, `Modules/Ports`) — `PortScanner` (`GetExtendedTcpTable`, owner PIDs, session-0 flag),
  `PortFilter` (merge per port and program, filter system processes / vendor services / dynamic ports),
  `Containers` (`docker ps`, `wsl -l -v`, stop), `PortsProvider` (command bar `port 3000`). View `Views/Ports/PortsView`.
  Option `ports.notify`.
- **Projects** (`projects`, `Modules/Projects`) — `RepoParse` (git porcelain v2, GitHub remotes, Actions runs, search
  counts), `BuildWatcher` + `BuildCommand` (recognise build/test command lines via `NtQueryInformationProcess`, keep a
  process handle for the exit code), `ProjectsModule` (git via `Cli`, GitHub REST with an optional DPAPI token, pills).
  View `Views/Projects/ProjectsView`. Options `projects.paths`, `projects.builds`, `projects.githubToken`.
- **Log watcher** (`logs`, `Modules/Logs`) — `LogTail` (follow a file from its end, survive rotation, first matching
  line), `LogWatchModule` (2 s poll, pill, 15 s quiet period). Options `logs.files`, `logs.pattern`.
- Command bar: `DevTools` / `DevToolsProvider` (encodings, hashes, JSON, UUID, time, colours, number bases, JWT) and
  `DevSystemProvider` (`ip`, `ping`, `flushdns`, `power …`, `ssh`), registered when `cmd.devTools` is on.

## Settings panel (`Views/SettingsPanel`)
`SettingsView` — full-panel overlay opened by `App.OpenSettings(section?)` (tab-bar gear, tray "Settings"). Sections
General · Modules · Options · Hotkeys · About. Writes `Settings.Current` and saves (`Settings.Changed` handlers apply
live); everything module-specific goes through `SettingsActions` (hotkey list/rebind/suspend, `ModuleHost.Sync`,
restart one module for options read at start, open the AI settings / agent hooks / character gallery / weather tab).
While it is open the island ignores click-through. `SettingsSnapshots` registers `settings-*` snapshot states.

## Tests (`tests/Isle.Tests`)
xUnit; `TestSetup` redirects `Paths.Root` to a temp folder. Pure logic only; UI is checked through snapshots (from M1).
