# Graph Report - island  (2026-11-15)

## Corpus Check
- 124 files · ~84,272 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2352 nodes · 6452 edges · 136 communities (88 shown, 48 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 841 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a7204e02`
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
- IVpnEnvironment
- Color
- Isle
- Window
- TimerItem
- PillItem
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
- FakeMediaSource
- NotesView
- TimersSnapshots.Register
- IntPtr
- ControlView
- Modules (`src/Isle/Modules`)
- Task
- LyricsService.cs
- App.StartModules
- system_windows
- Isle.Core
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- MarkupWindow.cs
- Settings
- NotesStore
- AudioVolume
- Action
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- IslandWindow
- DispatcherTimer
- Brightness
- Strings.cs
- Action
- List
- Grid
- Equalizer
- DateTimeOffset
- FrameworkElement
- PROGRESS
- Memory
- IMMNotificationClient
- Isle.Modules.Media
- IAudioEndpointVolume
- LiveEventsModule
- CommandBarView
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- IdlePillView
- Fact
- EDataFlow
- ForegroundWatcher
- TrayModule
- IDisposable
- CommandModule
- NotesFormattingTests.Formatting_survives_save_and_load
- MarkupWindow
- ControlServices
- HotkeyService
- DeviceState
- ObsSource
- system_windows_threading
- Dictionary
- Logger.Error
- IndicatorPill
- IslandWindow.Expand
- Gesture
- RegistryWatcher
- Modules (`src/Isle/Modules`)
- LyricsModule
- TimeSpan
- ClipEntry
- NotesModule
- Calculator.TryEvaluate
- Units
- CommandItem
- LyricsStatus
- ClipStore
- Parser
- LyricsTracker
- AppIndex
- Strings
- CoreAudio.cs
- Spring
- IModule
- ClipboardView
- Radios
- Lrc.Parse
- ClipboardService.SendCtrlV
- Isle.Tests
- SteamSource
- ToolsTests
- ARCHITECTURE
- TimerPill.cs
- ScreenGrab.WindowRects
- ModuleHost
- ModuleHostTests.Modules_are_lazy_and_follow_enabled_state
- Fuzzy.Score
- NotificationsSource
- WebProvider.QueryAsync
- Paths
- Secret
- BluetoothDevices.ListAsync
- ScreenGrab
- Shell
- KnownFolders.SHGetKnownFolderPath
- RECT

## God Nodes (most connected - your core abstractions)
1. `Native` - 88 edges
2. `Isle.Core` - 77 edges
3. `NotesView` - 47 edges
4. `ControlView` - 46 edges
5. `IslandWindow` - 46 edges
6. `Modules (`src/Isle/Modules`)` - 43 edges
7. `MarkupWindow` - 42 edges
8. `Thickness` - 41 edges
9. `TimersController` - 40 edges
10. `IslandWindow` - 38 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M6` --references--> `ForegroundWatcher`  [INFERRED]
  DECISIONS.md → src/Isle/Core/ForegroundWatcher.cs
- `M6` --references--> `ILiveSink`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/LiveEvents/Sources.cs

## Import Cycles
- None detected.

## Communities (136 total, 48 thin omitted)

### Community 0 - "Native"
Cohesion: 0.10
Nodes (7): EnumWindowsProc, INPUT, LowLevelKeyboardProc, MonitorEnumProc, Native, POINT, WinEventDelegate

### Community 1 - "ShelfView"
Cohesion: 0.07
Nodes (15): Isle.Modules.Shelf, ClipSnapshots, ShelfFile, Items, Version, ShelfItem, Added, Id (+7 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.16
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.13
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.12
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 8 - "ClipboardService"
Cohesion: 0.15
Nodes (3): ClipboardModule, Id, ClipboardService

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.14
Nodes (10): net10.0-windows10.0.19041.0, Microsoft.NET.Test.Sdk (17.14.1), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), ZXing.Net (0.16.11), Microsoft.NET.Sdk (+2 more)

### Community 11 - "IVpnEnvironment"
Cohesion: 0.15
Nodes (9): EmptyVpnEnvironment, LocalAppData, ProgramFiles, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment, LocalAppData (+1 more)

### Community 12 - "Color"
Cohesion: 0.12
Nodes (6): Theme, Accent, IsDark, ArtColors, ColorText, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerItem"
Cohesion: 0.21
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 18 - "PillItem"
Cohesion: 0.11
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.10
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "MediaModule"
Cohesion: 0.15
Nodes (9): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPill, PillWidth (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.06
Nodes (21): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly (+13 more)

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (7): CalculatorTests, UnitTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.11
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.16
Nodes (5): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.14
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 33 - "IntPtr"
Cohesion: 0.12
Nodes (6): CapsLockSource, TrayIcon, ICONINFO, MOUSEINPUT, NOTIFYICONDATA, SHFILEINFO

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.27
Nodes (3): TimersModule, Controller, Id

### Community 40 - "Isle.Core"
Cohesion: 0.14
Nodes (7): Isle.Modules.Clipboard, Isle.Views.Notes, Isle.Core, Isle.Modules.Notes, Isle.Views.Shelf, Isle.Views.Clipboard, PillPriority

### Community 41 - "TabBar"
Cohesion: 0.15
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.26
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.13
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.20
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 47 - "Settings"
Cohesion: 0.08
Nodes (21): Isle.Modules.Tray, Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys (+13 more)

### Community 48 - "NotesStore"
Cohesion: 0.11
Nodes (18): NotesSnapshots, Note, Created, Folder, Id, Pinned, Plain, Preview (+10 more)

### Community 49 - "AudioVolume"
Cohesion: 0.17
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "Action"
Cohesion: 0.13
Nodes (8): Isle.Modules.Commands, AppsProvider, CalculatorProvider, Cmd, CurrencyProvider, ICommandProvider, SystemProvider, TimerProvider

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.14
Nodes (8): Client, VpnDetector, VpnEntry, CanToggle, FakeEnv, LocalAppData, ProgramFiles, VpnDetectorTests

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "IslandWindow"
Cohesion: 0.15
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 55 - "DispatcherTimer"
Cohesion: 0.18
Nodes (4): ILiveSource, KeyboardLayoutSource, MicCameraSource, VpnIndicatorSource

### Community 56 - "Brightness"
Cohesion: 0.29
Nodes (3): Brightness, Level, Supported

### Community 58 - "Action"
Cohesion: 0.14
Nodes (5): OcrReader, ToolsModule, Id, Thickness, ToolsView

### Community 60 - "List"
Cohesion: 0.10
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 62 - "Equalizer"
Cohesion: 0.29
Nodes (3): ArtImage, Equalizer, Playing

### Community 63 - "DateTimeOffset"
Cohesion: 0.18
Nodes (5): DiskEntry, LyricsResult, LyricsService, FakeHttp, LyricsServiceTests

### Community 64 - "FrameworkElement"
Cohesion: 0.19
Nodes (3): TabInfo, IslandWindow, ToolsSnapshots

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 67 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.21
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 72 - "CommandBarView"
Cohesion: 0.14
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.27
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 75 - "Fact"
Cohesion: 0.23
Nodes (3): ClipStoreTests, StringsTests, TrayIconTests

### Community 76 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 79 - "IDisposable"
Cohesion: 0.17
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 80 - "CommandModule"
Cohesion: 0.13
Nodes (4): IdleHttp, CommandModule, Id, LrclibHttp

### Community 82 - "MarkupWindow"
Cohesion: 0.05
Nodes (21): PxRect, IsEmpty, MarkupWindow, Stroke, PickButton, IsChecked, Tool, Arrow (+13 more)

### Community 83 - "ControlServices"
Cohesion: 0.17
Nodes (7): ControlModule, Id, ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "DeviceState"
Cohesion: 0.25
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 87 - "system_windows_threading"
Cohesion: 0.29
Nodes (3): Isle.Views.Live, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents

### Community 88 - "Dictionary"
Cohesion: 0.16
Nodes (7): CurrencyParser, PillSink, Ui, ILiveSink, Ui, FakeSink, Ui

### Community 89 - "Logger.Error"
Cohesion: 0.16
Nodes (4): Autostart, IsEnabled, JsonStore, Logger

### Community 90 - "IndicatorPill"
Cohesion: 0.23
Nodes (3): LiveSnapshots, ConsentStore, IndicatorPill

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.11
Nodes (14): Modules (`src/Isle/Modules`), DECISIONS, M1, M2, M3, M4, M6, M7 (+6 more)

### Community 95 - "LyricsModule"
Cohesion: 0.29
Nodes (3): LyricsModule, Id, Service

### Community 98 - "ClipEntry"
Cohesion: 0.11
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 99 - "NotesModule"
Cohesion: 0.27
Nodes (4): NotesModule, Id, Store, View

### Community 100 - "Calculator.TryEvaluate"
Cohesion: 0.22
Nodes (3): Calculator, Result, CurrencyQuery

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "CommandItem"
Cohesion: 0.18
Nodes (10): Isle.Views.Commands, CommandSnapshots, CommandItem, Confirm, IconKey, IconPath, Run, Score (+2 more)

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 105 - "ClipStore"
Cohesion: 0.25
Nodes (3): ClipStore, Entries, Limit

### Community 107 - "LyricsTracker"
Cohesion: 0.18
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.19
Nodes (10): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+2 more)

### Community 109 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 110 - "CoreAudio.cs"
Cohesion: 0.16
Nodes (5): IKsControl, IMMDevice, IMMDeviceCollection, IMMDeviceEnumerator, KsProperty

### Community 112 - "IModule"
Cohesion: 0.18
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 114 - "Radios"
Cohesion: 0.33
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 119 - "SteamSource"
Cohesion: 0.27
Nodes (3): SpeedMeter, BytesPerSecond, SteamSource

### Community 121 - "ARCHITECTURE"
Cohesion: 0.20
Nodes (6): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), M0, TestSetup

### Community 122 - "TimerPill.cs"
Cohesion: 0.36
Nodes (4): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests

### Community 124 - "ModuleHost"
Cohesion: 0.42
Nodes (3): Process shape, ModuleHost, Ids

### Community 125 - "ModuleHostTests.Modules_are_lazy_and_follow_enabled_state"
Cohesion: 0.39
Nodes (3): Fake, Id, ModuleHostTests

### Community 128 - "WebProvider.QueryAsync"
Cohesion: 0.39
Nodes (3): M8, AskAi, WebProvider

### Community 129 - "Paths"
Cohesion: 0.29
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 135 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **309 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `net10.0-windows10.0.19041.0`, `System.Management (11.0.0)` (+304 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 511 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **48 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `BluetoothDevices.ListAsync`, `TimersController`, `MediaSession`, `Color`, `TimerBook`, `MediaModule`, `ReminderParser`, `TimeSpan`, `FakeMediaSource`, `TimersSnapshots.Register`, `IntPtr`, `Modules (`src/Isle/Modules`)`, `LyricsService.cs`, `NotesStore`, `AudioVolume`, `VpnDetector.Detect`, `Brightness`, `List`, `DateTimeOffset`, `ControlServices`, `HotkeyService`, `TimeSpan`, `NotesModule`, `LyricsTracker`, `Radios`, `ARCHITECTURE`?**
  _High betweenness centrality (0.062) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _309 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.10080645161290322 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `ShelfView`, `SystemControls.cs`, `RatesSnapshot`, `LyricsService.cs`, `system_windows`, `Screens.All`, `MarkupWindow.cs`, `Settings`, `NotesStore`, `Action`, `Strings.cs`, `Isle.Modules.Media`, `IDisposable`, `CommandModule`, `system_windows_threading`, `AppIndex`, `Strings`, `Spring`, `IModule`, `Isle.Tests`, `TimerPill.cs`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **Should `ShelfView` be split into smaller, more focused modules?**
  _Cohesion score 0.07078039927404718 - nodes in this community are weakly interconnected._
- **Why does `NotesView` connect `NotesView` to `FrameworkElement`, `ShelfView`, `ControlView`, `system_windows`, `CommandBarView`, `TabBar`, `IslandWindow`, `IDisposable`, `NotesStore`, `DispatcherTimer`, `Action`, `Equalizer`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.13450292397660818 - nodes in this community are weakly interconnected._