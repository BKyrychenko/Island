# Graph Report - island  (2026-11-15)

## Corpus Check
- 131 files · ~93,345 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2597 nodes · 7207 edges · 132 communities (89 shown, 43 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 957 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e91dcab0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- ShelfView
- SystemControls.cs
- RatesSnapshot
- IslandWindow
- 4. Features (all of them)
- TimersController
- TimersView
- ClipboardService
- MediaSession
- Isle.Tests.csproj
- IReadOnlyList
- Color
- Isle
- Window
- Recorder
- WeatherReport
- MusicView
- ResourceDictionary
- TimerBook
- MediaModule
- ReminderParser
- CLAUDE.md
- Icons.xaml
- TimeSpan
- Node
- Theory
- MediaState
- WeatherModule
- NotesView
- TimersSnapshots.Register
- IntPtr
- ControlView
- Action
- TempDir
- Task
- App.StartModules
- Sources.cs
- Providers.cs
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows
- Settings
- NotesStore
- AudioVolume
- IEnumerable
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- Isle.Core
- KeyboardLayoutSource
- Strings.cs
- Action
- List
- Grid
- Strings.T
- DateTimeOffset
- FrameworkElement
- PROGRESS
- Dictionary
- IslandWindow
- Isle.Modules.Media
- IAudioEndpointVolume
- VpnIndicatorSource
- CommandBarView
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- IdlePillView
- Fact
- PlaybackMath.Choose
- ForegroundWatcher
- TrayModule
- IDisposable
- IdleHttp
- Note
- MarkupWindow
- ControlServices
- HotkeyService
- CoreAudio.cs
- ObsSource
- ShelfView
- FakeSink
- IMMNotificationClient
- IndicatorPill
- RepeatKind
- Gesture
- RegistryWatcher
- DECISIONS
- TimerItem
- TimeSpan
- DispatcherTimer
- ClipKind
- IslandWindow.TabViewFor
- IKsControl
- ClipSnapshots.Register
- Units
- Calculator.TryEvaluate
- LyricsStatus
- WindowsTheme
- Parser
- LyricsTracker
- AppIndex
- Strings
- EDataFlow
- Spring
- IModule
- ClipStore
- Radios
- Lrc.Parse
- ClipboardService.SendCtrlV
- WeatherFormat.Temp
- SteamSource
- ToolsTests
- ARCHITECTURE
- TimerPill.cs
- ScreenGrab.WindowRects
- ModuleHost
- Autostart
- Fuzzy.Score
- ObsAuth.Response
- Paths
- Secret
- BluetoothDevices.ListAsync
- KnownFolders.SHGetKnownFolderPath

## God Nodes (most connected - your core abstractions)
1. `Native` - 88 edges
2. `Isle.Core` - 83 edges
3. `Modules (`src/Isle/Modules`)` - 50 edges
4. `Recorder` - 50 edges
5. `Thickness` - 50 edges
6. `NotesView` - 47 edges
7. `ControlView` - 46 edges
8. `IslandWindow` - 46 edges
9. `MarkupWindow` - 42 edges
10. `TimersController` - 40 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `Modules (`src/Isle/Modules`)` --references--> `WeatherCodes`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Weather/WeatherData.cs
- `Modules (`src/Isle/Modules`)` --references--> `WeatherFormat`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Weather/WeatherData.cs
- `Modules (`src/Isle/Modules`)` --references--> `RainAlerts`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Weather/WeatherData.cs

## Import Cycles
- None detected.

## Communities (132 total, 43 thin omitted)

### Community 0 - "Native"
Cohesion: 0.09
Nodes (11): EnumWindowsProc, INPUT, LowLevelKeyboardProc, MonitorEnumProc, Native, NOTIFYICONDATA, POINT, RECT (+3 more)

### Community 1 - "ShelfView"
Cohesion: 0.14
Nodes (12): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+4 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.16
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.12
Nodes (6): ITabView, IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.12
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 8 - "ClipboardService"
Cohesion: 0.12
Nodes (4): ClipboardModule, Id, ClipboardService, ClipPrivacy

### Community 9 - "MediaSession"
Cohesion: 0.12
Nodes (4): Logger, MediaSession, Current, ScreenGrab

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.12
Nodes (12): Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), ZXing.Net (0.16.11) (+4 more)

### Community 11 - "IReadOnlyList"
Cohesion: 0.18
Nodes (6): EmptyVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment, LocalAppData, ProgramFiles

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.06
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.06
Nodes (33): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+25 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.10
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "MediaModule"
Cohesion: 0.05
Nodes (31): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+23 more)

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (6): CurrencyQuery, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.08
Nodes (21): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots, MediaState, Accent, Album (+13 more)

### Community 30 - "WeatherModule"
Cohesion: 0.11
Nodes (9): RainAlert, WeatherModule, Fahrenheit, HasPlace, Id, S, WeatherSnapshots, WeatherActions (+1 more)

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.12
Nodes (13): Modules (`src/Isle/Modules`), TimerFinished, TimersModule, Controller, Id, TimersSnapshots, ReminderBanner, Ring (+5 more)

### Community 33 - "IntPtr"
Cohesion: 0.10
Nodes (5): CapsLockSource, TrayIcon, ICONINFO, MOUSEINPUT, SHFILEINFO

### Community 36 - "TempDir"
Cohesion: 0.21
Nodes (5): Isle.Modules.Tray, SettingsTests, TempDir, Path, NotesStoreTests

### Community 37 - "Task"
Cohesion: 0.17
Nodes (6): Place, IWeatherSource, OpenMeteoSource, WeatherService, LastFailed, FakeSource

### Community 38 - "App.StartModules"
Cohesion: 0.11
Nodes (7): LyricsModule, Id, NotesModule, Id, Store, ShelfModule, Id

### Community 40 - "Providers.cs"
Cohesion: 0.13
Nodes (3): Isle.Modules.Commands, Isle.Views.Commands, CapabilityUse

### Community 41 - "TabBar"
Cohesion: 0.11
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.11
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.17
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.11
Nodes (7): Process shape, Shell, Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.15
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "system_windows"
Cohesion: 0.11
Nodes (4): Isle.Modules.Tools, Isle, Isle.Views, Isle.Views.Tools

### Community 47 - "Settings"
Cohesion: 0.11
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "NotesStore"
Cohesion: 0.22
Nodes (5): NotesSnapshots, NotesStore, Doc, Folders, Notes

### Community 49 - "AudioVolume"
Cohesion: 0.16
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "IEnumerable"
Cohesion: 0.10
Nodes (18): M8, CommandSnapshots, AppsProvider, AskAi, CalculatorProvider, CommandItem, Confirm, IconKey (+10 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.12
Nodes (11): Client, IVpnEnvironment, LocalAppData, ProgramFiles, VpnDetector, VpnEntry, CanToggle, FakeEnv (+3 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.23
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "Isle.Core"
Cohesion: 0.11
Nodes (8): Isle.Modules.Clipboard, Isle.Core, Isle.Modules.Weather, Isle.Modules.Hotkeys, Isle.Views.Shelf, Isle.Views.Weather, Isle.Tests, Isle.Views.Clipboard

### Community 55 - "KeyboardLayoutSource"
Cohesion: 0.21
Nodes (4): ILiveSink, Ui, KeyboardLayoutSource, MicCameraSource

### Community 58 - "Action"
Cohesion: 0.11
Nodes (4): OcrReader, RecordOptions, ToolsModule, Id

### Community 60 - "List"
Cohesion: 0.11
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.16
Nodes (7): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, FakeHttp, LyricsServiceTests

### Community 64 - "FrameworkElement"
Cohesion: 0.17
Nodes (4): TabInfo, IslandWindow, IslandWindow, ToolsSnapshots

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 66 - "Dictionary"
Cohesion: 0.14
Nodes (6): CurrencyParser, LiveEventsModule, Id, PillSink, Ui, ILiveSource

### Community 67 - "IslandWindow"
Cohesion: 0.14
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.35
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.13
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 71 - "VpnIndicatorSource"
Cohesion: 0.16
Nodes (3): NotificationsSource, Unavailable, VpnIndicatorSource

### Community 72 - "CommandBarView"
Cohesion: 0.18
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 75 - "Fact"
Cohesion: 0.18
Nodes (6): ClipFile, Entries, Version, ClipStoreTests, StringsTests, TrayIconTests

### Community 76 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 79 - "IDisposable"
Cohesion: 0.17
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 80 - "IdleHttp"
Cohesion: 0.14
Nodes (5): M10, IdleHttp, CommandModule, Id, LrclibHttp

### Community 81 - "Note"
Cohesion: 0.10
Nodes (16): Isle.Views.Notes, Isle.Modules.Notes, Note, Created, Folder, Id, Pinned, Plain (+8 more)

### Community 82 - "MarkupWindow"
Cohesion: 0.05
Nodes (22): PxRect, IsEmpty, MarkupWindow, Stroke, PickButton, IsChecked, Tool, Arrow (+14 more)

### Community 83 - "ControlServices"
Cohesion: 0.12
Nodes (10): ControlModule, Id, ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots, Brightness (+2 more)

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "CoreAudio.cs"
Cohesion: 0.13
Nodes (10): CoreAudioIds, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice (+2 more)

### Community 89 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 90 - "IndicatorPill"
Cohesion: 0.19
Nodes (3): LiveSnapshots, ConsentStore, IndicatorPill

### Community 91 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "DECISIONS"
Cohesion: 0.25
Nodes (7): DECISIONS, M1, M3, M4, M6, M7, Performance measurements

### Community 95 - "TimerItem"
Cohesion: 0.18
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 98 - "ClipKind"
Cohesion: 0.67
Nodes (3): ClipKind, Image, Text

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Calculator.TryEvaluate"
Cohesion: 0.17
Nodes (3): Result, CalculatorTests, UnitTests

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 107 - "LyricsTracker"
Cohesion: 0.18
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.10
Nodes (12): JsonStore, AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry (+4 more)

### Community 109 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 110 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 112 - "IModule"
Cohesion: 0.17
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 113 - "ClipStore"
Cohesion: 0.10
Nodes (15): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+7 more)

### Community 114 - "Radios"
Cohesion: 0.28
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 115 - "Lrc.Parse"
Cohesion: 0.21
Nodes (3): Lrc, LrcLine, LrcTests

### Community 119 - "SteamSource"
Cohesion: 0.25
Nodes (3): SpeedMeter, BytesPerSecond, SteamSource

### Community 121 - "ARCHITECTURE"
Cohesion: 0.20
Nodes (6): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), M0, TestSetup

### Community 122 - "TimerPill.cs"
Cohesion: 0.42
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 124 - "ModuleHost"
Cohesion: 0.25
Nodes (5): ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 129 - "Paths"
Cohesion: 0.29
Nodes (4): Paths, LogFile, Root, SettingsFile

## Knowledge Gaps
- **339 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `Place`, `Fetched` (+334 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 576 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **43 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Isle.Core` connect `Isle.Core` to `ShelfView`, `SystemControls.cs`, `MediaModule`, `TempDir`, `Task`, `Sources.cs`, `Providers.cs`, `Screens.All`, `system_windows`, `Recorder.cs`, `Strings.cs`, `Isle.Modules.Media`, `IDisposable`, `Note`, `AppIndex`, `Strings`, `Spring`, `IModule`, `TimerPill.cs`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _339 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.08907563025210084 - nodes in this community are weakly interconnected._
- **Why does `Modules (`src/Isle/Modules`)` connect `TimersSnapshots.Register` to `BluetoothDevices.ListAsync`, `TimersController`, `MediaSession`, `Color`, `WeatherReport`, `TimerBook`, `MediaModule`, `ReminderParser`, `MediaState`, `WeatherModule`, `IntPtr`, `Task`, `App.StartModules`, `NotesStore`, `AudioVolume`, `VpnDetector.Detect`, `List`, `DateTimeOffset`, `PlaybackMath.Choose`, `ControlServices`, `HotkeyService`, `TimeSpan`, `IslandWindow.TabViewFor`, `WindowsTheme`, `LyricsTracker`, `Radios`, `Lrc.Parse`, `WeatherFormat.Temp`, `ARCHITECTURE`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Should `ShelfView` be split into smaller, more focused modules?**
  _Cohesion score 0.1354679802955665 - nodes in this community are weakly interconnected._
- **Why does `AudioVolume` connect `AudioVolume` to `TimersSnapshots.Register`, `SystemControls.cs`, `Dictionary`, `IAudioEndpointVolume`, `IDisposable`, `ControlServices`, `CoreAudio.cs`, `IMMNotificationClient`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.11576354679802955 - nodes in this community are weakly interconnected._