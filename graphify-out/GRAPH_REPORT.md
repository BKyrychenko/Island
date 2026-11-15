# Graph Report - island  (2026-11-15)

## Corpus Check
- 93 files · ~62,255 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 1763 nodes · 4567 edges · 109 communities (70 shown, 39 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 611 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c9ff5bfa`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- IntPtr
- Fact
- SystemControls.cs
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- DECISIONS
- TimersView
- Radios
- MediaSession
- Isle.Tests.csproj
- IReadOnlyList
- Color
- Isle
- Window
- TimerBook
- PillItem
- MusicView
- ResourceDictionary
- TimersController
- TimerItem
- ReminderParser
- CLAUDE.md
- Icons.xaml
- Lrc.Parse
- Node
- Theory
- MediaState
- FakeMediaSource
- NotesView
- TimersSnapshots.Register
- ForegroundWatcher
- ControlView
- Modules (`src/Isle/Modules`)
- system_windows
- LyricsService.cs
- App.StartModules
- MusicView.cs
- TimerPill.cs
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- Isle.Core
- Settings
- NotesStore
- AudioVolume
- LyricsTracker
- DeviceState
- CoreAudio.cs
- DownloadsSource
- Paths
- LiveEventsModule.Create
- Task
- Strings.cs
- RECT
- WinEventDelegate
- List
- Grid
- Strings
- DateTimeOffset
- MediaModule.Start
- PROGRESS
- EventArgs
- IMMNotificationClient
- MonitorEnumProc
- IAudioEndpointVolume
- LiveEventsModule
- Grid
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- IdlePillView
- Spring
- EDataFlow
- HotkeyService
- Logger.Error
- IDisposable
- LrclibHttp
- NotesView.cs
- Note
- Action
- IModule
- ObsSource
- Sources.cs
- Func
- KeyboardLayoutSource
- IndicatorPill
- SteamSource
- Gesture
- RegistryWatcher
- PlaybackMath.Choose
- ARCHITECTURE
- DispatcherTimer
- TimeSpan
- NotesModule
- RepeatKind
- BluetoothDevices.ListAsync
- Secret
- LyricsStatus
- NotesFormattingTests.Formatting_survives_save_and_load
- SubmitResult
- KnownFolders.SHGetKnownFolderPath
- LowLevelKeyboardProc

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 56 edges
2. `IslandWindow` - 54 edges
3. `Native` - 53 edges
4. `NotesView` - 47 edges
5. `ControlView` - 46 edges
6. `IslandWindow` - 46 edges
7. `Modules (`src/Isle/Modules`)` - 43 edges
8. `TimersController` - 40 edges
9. `MusicView` - 37 edges
10. `MediaState` - 37 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `M6` --references--> `ILiveSink`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/LiveEvents/Sources.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M6` --references--> `ForegroundWatcher`  [INFERRED]
  DECISIONS.md → src/Isle/Core/ForegroundWatcher.cs

## Import Cycles
- None detected.

## Communities (109 total, 39 thin omitted)

### Community 0 - "IntPtr"
Cohesion: 0.09
Nodes (6): TrayModule, Id, ICONINFO, Native, NOTIFYICONDATA, POINT

### Community 1 - "Fact"
Cohesion: 0.22
Nodes (5): SettingsTests, StringsTests, TempDir, Path, NotesStoreTests

### Community 3 - "ModuleHost"
Cohesion: 0.22
Nodes (6): Process shape, ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 4 - "IslandWindow"
Cohesion: 0.10
Nodes (6): ITabView, IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "DECISIONS"
Cohesion: 0.18
Nodes (8): DECISIONS, M0, M1, M4, M6, Performance measurements, TrayIcon, TrayIconTests

### Community 8 - "Radios"
Cohesion: 0.24
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.15
Nodes (9): net10.0-windows10.0.19041.0, Microsoft.NET.Test.Sdk (17.14.1), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, net10.0-windows10.0.19041.0 (+1 more)

### Community 11 - "IReadOnlyList"
Cohesion: 0.09
Nodes (15): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+7 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerBook"
Cohesion: 0.09
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 18 - "PillItem"
Cohesion: 0.06
Nodes (30): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+22 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimersController"
Cohesion: 0.14
Nodes (6): TimersController, Clock, NextWake, Reminders, Timers, Zone

### Community 22 - "TimerItem"
Cohesion: 0.21
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 26 - "Lrc.Parse"
Cohesion: 0.24
Nodes (3): Lrc, LrcLine, LrcTests

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.18
Nodes (4): DownloadDetectorTests, LiveMiscTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.17
Nodes (5): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.21
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 34 - "ControlView"
Cohesion: 0.13
Nodes (3): VpnEntry, CanToggle, ControlView

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.19
Nodes (7): Modules (`src/Isle/Modules`), M3, TimerFinished, TimersModule, Controller, Id, ReminderBanner

### Community 36 - "system_windows"
Cohesion: 0.14
Nodes (3): Isle, Isle.Views, PillPriority

### Community 39 - "MusicView.cs"
Cohesion: 0.20
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 40 - "TimerPill.cs"
Cohesion: 0.42
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 41 - "TabBar"
Cohesion: 0.12
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.29
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.10
Nodes (6): Application, App, Host, Island, IsSnapshot, IslandWindow

### Community 45 - "IslandWindow"
Cohesion: 0.10
Nodes (8): Shell, IslandWindow, IsHiddenByUser, PasteTarget, Handle, Hotkeys, Pill, Watcher

### Community 46 - "Isle.Core"
Cohesion: 0.18
Nodes (4): Isle.Core, Isle.Modules.Hotkeys, Isle.Modules.Tray, Isle.Tests

### Community 47 - "Settings"
Cohesion: 0.09
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "NotesStore"
Cohesion: 0.23
Nodes (5): NotesSnapshots, NotesStore, Doc, Folders, Notes

### Community 49 - "AudioVolume"
Cohesion: 0.14
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "LyricsTracker"
Cohesion: 0.17
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 51 - "DeviceState"
Cohesion: 0.11
Nodes (11): CoreAudioIds, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IKsControl (+3 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "Paths"
Cohesion: 0.33
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 55 - "LiveEventsModule.Create"
Cohesion: 0.16
Nodes (6): CapsLockSource, ILiveSink, Ui, MicCameraSource, NotificationsSource, Unavailable

### Community 56 - "Task"
Cohesion: 0.20
Nodes (4): Logger, Brightness, Level, Supported

### Community 58 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

### Community 60 - "List"
Cohesion: 0.12
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 62 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 63 - "DateTimeOffset"
Cohesion: 0.16
Nodes (7): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, FakeHttp, LyricsServiceTests

### Community 64 - "MediaModule.Start"
Cohesion: 0.21
Nodes (3): TabInfo, LyricsModule, Id

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 67 - "IMMNotificationClient"
Cohesion: 0.14
Nodes (8): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMDeviceEnumerator, IMMNotificationClient

### Community 71 - "LiveEventsModule"
Cohesion: 0.18
Nodes (5): LiveEventsModule, Id, PillSink, Ui, ILiveSource

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.27
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 76 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 77 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 78 - "Logger.Error"
Cohesion: 0.15
Nodes (5): Autostart, IsEnabled, JsonStore, WindowsTheme, IsDark

### Community 79 - "IDisposable"
Cohesion: 0.19
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 82 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 83 - "Action"
Cohesion: 0.25
Nodes (5): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots

### Community 84 - "IModule"
Cohesion: 0.18
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 91 - "SteamSource"
Cohesion: 0.27
Nodes (3): SpeedMeter, BytesPerSecond, SteamSource

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (4): M2, PlaybackMath, PlaybackTests, VERIFICATION

### Community 96 - "ARCHITECTURE"
Cohesion: 0.22
Nodes (5): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), TestSetup

### Community 99 - "NotesModule"
Cohesion: 0.32
Nodes (3): NotesModule, Id, Store

### Community 100 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 106 - "SubmitResult"
Cohesion: 0.40
Nodes (5): SubmitResult, Full, Invalid, Reminder, Timer

## Knowledge Gaps
- **245 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `net10.0-windows10.0.19041.0`, `System.Management (11.0.0)` (+240 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 413 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **39 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `DECISIONS`, `Radios`, `MediaSession`, `IReadOnlyList`, `Color`, `TimerBook`, `PillItem`, `TimersController`, `ReminderParser`, `Lrc.Parse`, `FakeMediaSource`, `NotesView`, `TimersSnapshots.Register`, `App.StartModules`, `NotesStore`, `AudioVolume`, `LyricsTracker`, `Task`, `List`, `DateTimeOffset`, `HotkeyService`, `Logger.Error`, `Action`, `PlaybackMath.Choose`, `ARCHITECTURE`, `TimeSpan`, `NotesModule`, `BluetoothDevices.ListAsync`?**
  _High betweenness centrality (0.125) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _245 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `IntPtr` be split into smaller, more focused modules?**
  _Cohesion score 0.09453551912568306 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `IntPtr`, `DispatcherTimer`, `ForegroundWatcher`, `EventArgs`, `IslandWindow`, `NotesModule`, `MediaModule.Start`, `LiveEventsModule`, `MusicView.cs`, `Screens.All`, `Window`, `Isle.Core`, `NotesStore`, `KeyboardLayoutSource`, `NotesView`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.1380952380952381 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `IslandWindow`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.10252100840336134 - nodes in this community are weakly interconnected._