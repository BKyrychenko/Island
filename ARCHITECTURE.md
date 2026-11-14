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
- **Hotkeys** (`HotkeyService`, owned by the island, not a module) — `Register(id, titleKey, default, pressed, released?)`,
  `Rebind`, `List()` for the settings table; user overrides in `Settings.Hotkeys`; failures logged, never fatal.
  Hold-hotkeys poll the key state at 30 ms only while held. Default: `toggle` = Win+Alt+Space.
- **Tray** (`tray`) — notification-area icon (`Shell_NotifyIcon`, hidden message window), native menu:
  Show/Hide, Settings, Start with Windows, Quit. Icon drawn by `TrayIcon.Render`. Re-adds itself on `TaskbarCreated`.

## Tests (`tests/Isle.Tests`)
xUnit; `TestSetup` redirects `Paths.Root` to a temp folder. Pure logic only; UI is checked through snapshots (from M1).
