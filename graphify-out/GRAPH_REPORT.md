# Graph Report - island  (2026-11-15)

## Corpus Check
- 125 files · ~86,979 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2435 nodes · 6620 edges · 134 communities (91 shown, 43 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 857 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9e483733`
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
- MachineVpnEnvironment
- Color
- Isle
- Window
- Recorder
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
- Action
- TempDir
- CommandModule
- App.StartModules
- system_windows
- Isle.Core
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows_media
- Settings
- NotesStore
- AudioVolume
- Action
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- RepeatRule
- KeyboardLayoutSource
- Strings.cs
- Action
- List
- Grid
- DateTimeOffset
- FrameworkElement
- PROGRESS
- RepeatRule.Next
- Theme
- Isle.Modules.Media
- IAudioEndpointVolume
- LiveEventsModule.Create
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
- DeviceState
- ObsSource
- ReminderParser.Parse
- Dictionary
- TrayIcon.Create
- IndicatorPill
- RepeatKind
- Gesture
- RegistryWatcher
- DECISIONS
- LyricsModule
- TimeSpan
- ClipEntry
- NotesModule
- NotesView.cs
- ClipSnapshots.Register
- Units
- IEnumerable
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
- SubmitResult
- Fact
- RecordOptions
- ColorText.Hex
- Paths
- Secret
- BluetoothDevices.ListAsync
- KnownFolders.SHGetKnownFolderPath
- RECT

## God Nodes (most connected - your core abstractions)
1. `Native` - 88 edges
2. `Isle.Core` - 78 edges
3. `Recorder` - 50 edges
4. `NotesView` - 47 edges
5. `ControlView` - 46 edges
6. `IslandWindow` - 46 edges
7. `Modules (`src/Isle/Modules`)` - 43 edges
8. `MarkupWindow` - 42 edges
9. `Thickness` - 42 edges
10. `TimersController` - 40 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M5` --references--> `IMMNotificationClient`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M6` --references--> `ForegroundWatcher`  [INFERRED]
  DECISIONS.md → src/Isle/Core/ForegroundWatcher.cs

## Import Cycles
- None detected.

## Communities (134 total, 43 thin omitted)

### Community 0 - "Native"
Cohesion: 0.10
Nodes (8): EnumWindowsProc, INPUT, LowLevelKeyboardProc, MonitorEnumProc, MOUSEINPUT, Native, POINT, WinEventDelegate

### Community 1 - "ShelfView"
Cohesion: 0.08
Nodes (14): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+6 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.15
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.10
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.14
Nodes (6): TimersController, Clock, NextWake, Reminders, Timers, Zone

### Community 8 - "ClipboardService"
Cohesion: 0.13
Nodes (4): ClipboardModule, Id, ClipboardService, ClipPrivacy

### Community 9 - "MediaSession"
Cohesion: 0.15
Nodes (4): Autostart, IsEnabled, MediaSession, Current

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.12
Nodes (12): Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), ZXing.Net (0.16.11) (+4 more)

### Community 11 - "MachineVpnEnvironment"
Cohesion: 0.25
Nodes (3): MachineVpnEnvironment, LocalAppData, ProgramFiles

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.07
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.08
Nodes (25): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+17 more)

### Community 22 - "MediaModule"
Cohesion: 0.10
Nodes (12): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, ArtImage, Equalizer (+4 more)

### Community 27 - "Node"
Cohesion: 0.13
Nodes (8): CapabilityUse, Node, ObsAuth, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (7): CalculatorTests, UnitTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.13
Nodes (17): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+9 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.20
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.12
Nodes (11): TimersModule, Controller, Id, TimersSnapshots, ReminderBanner, Ring, Brush, Progress (+3 more)

### Community 35 - "Action"
Cohesion: 0.22
Nodes (3): Cmd, Thickness, ToolsView

### Community 36 - "TempDir"
Cohesion: 0.22
Nodes (5): SettingsTests, TempDir, Path, FakeHttp, LyricsServiceTests

### Community 37 - "CommandModule"
Cohesion: 0.14
Nodes (4): Isle.Modules.Commands, Isle.Views.Commands, CommandModule, Id

### Community 38 - "App.StartModules"
Cohesion: 0.18
Nodes (4): ControlModule, Id, ShelfModule, Id

### Community 39 - "system_windows"
Cohesion: 0.14
Nodes (3): Isle, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents

### Community 40 - "Isle.Core"
Cohesion: 0.13
Nodes (4): Isle.Views.Live, Isle.Core, PillPriority, LiveSnapshots

### Community 41 - "TabBar"
Cohesion: 0.12
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.26
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.12
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.08
Nodes (11): Shell, ITabView, IslandWindow, FileDropTarget, IslandWindow, IsHiddenByUser, PasteTarget, Handle (+3 more)

### Community 46 - "system_windows_media"
Cohesion: 0.14
Nodes (4): Isle.Modules.Clipboard, Isle.Views, Isle.Views.Shelf, Isle.Views.Clipboard

### Community 47 - "Settings"
Cohesion: 0.09
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "NotesStore"
Cohesion: 0.16
Nodes (9): Modules (`src/Isle/Modules`), WindowsTheme, IsDark, NotesSnapshots, NotesStore, Doc, Folders, Notes (+1 more)

### Community 49 - "AudioVolume"
Cohesion: 0.13
Nodes (9): M5, AudioVolume, Available, Level, Muted, VolumeCallback, Audio, AudioVolumeNotificationData (+1 more)

### Community 50 - "Action"
Cohesion: 0.13
Nodes (8): M8, AppsProvider, AskAi, CurrencyProvider, ICommandProvider, SystemProvider, TimerProvider, WebProvider

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.12
Nodes (11): Client, IVpnEnvironment, LocalAppData, ProgramFiles, VpnDetector, VpnEntry, CanToggle, FakeEnv (+3 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 54 - "RepeatRule"
Cohesion: 0.20
Nodes (7): RepeatRule, DayOfMonth, Days, Interval, Kind, ReminderParserTests, Repeats

### Community 58 - "Action"
Cohesion: 0.14
Nodes (3): OcrReader, ToolsModule, Id

### Community 60 - "List"
Cohesion: 0.13
Nodes (15): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+7 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.23
Nodes (3): DiskEntry, LyricsResult, LyricsService

### Community 64 - "FrameworkElement"
Cohesion: 0.21
Nodes (3): TabInfo, IslandWindow, ToolsSnapshots

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 67 - "Theme"
Cohesion: 0.27
Nodes (3): Theme, Accent, IsDark

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.23
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 71 - "LiveEventsModule.Create"
Cohesion: 0.10
Nodes (7): LiveEventsModule, Id, CapsLockSource, ILiveSource, NotificationsSource, Unavailable, VpnIndicatorSource

### Community 72 - "CommandBarView"
Cohesion: 0.16
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.27
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 76 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 79 - "IDisposable"
Cohesion: 0.19
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 80 - "IdleHttp"
Cohesion: 0.26
Nodes (3): IdleHttp, ILyricsHttp, LrclibHttp

### Community 81 - "Note"
Cohesion: 0.11
Nodes (14): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+6 more)

### Community 82 - "MarkupWindow"
Cohesion: 0.05
Nodes (22): PxRect, IsEmpty, ScreenGrab, MarkupWindow, Stroke, PickButton, IsChecked, Tool (+14 more)

### Community 83 - "ControlServices"
Cohesion: 0.16
Nodes (11): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots, EmptyVpnEnvironment, LocalAppData, ProgramFiles (+3 more)

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "DeviceState"
Cohesion: 0.13
Nodes (8): DeviceCallback, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMNotificationClient

### Community 87 - "ReminderParser.Parse"
Cohesion: 0.25
Nodes (3): ParsedReminder, State, HasDate

### Community 88 - "Dictionary"
Cohesion: 0.14
Nodes (6): PillSink, Ui, ILiveSink, Ui, FakeSink, Ui

### Community 89 - "TrayIcon.Create"
Cohesion: 0.29
Nodes (3): M0, TrayIcon, ICONINFO

### Community 90 - "IndicatorPill"
Cohesion: 0.16
Nodes (3): ConsentStore, DownloadDetector, IndicatorPill

### Community 91 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "DECISIONS"
Cohesion: 0.15
Nodes (10): DECISIONS, M1, M3, M4, M6, M7, M9, Performance measurements (+2 more)

### Community 95 - "LyricsModule"
Cohesion: 0.33
Nodes (3): LyricsModule, Id, Service

### Community 98 - "ClipEntry"
Cohesion: 0.12
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 99 - "NotesModule"
Cohesion: 0.27
Nodes (4): NotesModule, Id, Store, View

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "IEnumerable"
Cohesion: 0.16
Nodes (10): CommandSnapshots, CalculatorProvider, CommandItem, Confirm, IconKey, IconPath, Run, Score (+2 more)

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 105 - "ClipStore"
Cohesion: 0.23
Nodes (3): ClipStore, Entries, Limit

### Community 106 - "Parser"
Cohesion: 0.27
Nodes (3): Parser, DidWork, Result

### Community 107 - "LyricsTracker"
Cohesion: 0.18
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.11
Nodes (12): JsonStore, Logger, AppIndex, Entries, EntryKind, App, File, Recent (+4 more)

### Community 109 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 110 - "CoreAudio.cs"
Cohesion: 0.10
Nodes (14): CoreAudioIds, EDataFlow, All, Capture, Render, ERole, Communications, Console (+6 more)

### Community 112 - "IModule"
Cohesion: 0.13
Nodes (10): Entry, IModule, Id, ModuleState, Failed, Running, Stopped, Fake (+2 more)

### Community 114 - "Radios"
Cohesion: 0.18
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 115 - "Lrc.Parse"
Cohesion: 0.23
Nodes (3): Lrc, LrcLine, LrcTests

### Community 117 - "Isle.Tests"
Cohesion: 0.15
Nodes (3): Isle.Modules.Tools, Isle.Tests, Isle.Views.Tools

### Community 119 - "SteamSource"
Cohesion: 0.19
Nodes (4): CurrencyParser, SpeedMeter, BytesPerSecond, SteamSource

### Community 121 - "ARCHITECTURE"
Cohesion: 0.16
Nodes (6): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), Memory, TestSetup

### Community 122 - "TimerPill.cs"
Cohesion: 0.20
Nodes (5): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests, TimerBookTests

### Community 124 - "ModuleHost"
Cohesion: 0.42
Nodes (3): Process shape, ModuleHost, Ids

### Community 125 - "SubmitResult"
Cohesion: 0.40
Nodes (5): SubmitResult, Full, Invalid, Reminder, Timer

### Community 126 - "Fact"
Cohesion: 0.16
Nodes (5): Isle.Modules.Tray, Fuzzy, FuzzyTests, StringsTests, TrayIconTests

### Community 129 - "Paths"
Cohesion: 0.29
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 135 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **314 isolated node(s):** `net10.0-windows10.0.19041.0`, `NAudio.Wasapi (3.3.0)`, `System.Management (11.0.0)`, `System.Security.Cryptography.ProtectedData (11.0.0)`, `Vortice.Direct3D11 (3.8.3)` (+309 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 553 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **43 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Isle.Core` connect `Isle.Core` to `ShelfView`, `SystemControls.cs`, `RatesSnapshot`, `CommandModule`, `system_windows`, `Screens.All`, `system_windows_media`, `Action`, `Recorder.cs`, `Strings.cs`, `Isle.Modules.Media`, `IDisposable`, `Note`, `RegistryWatcher`, `NotesView.cs`, `AppIndex`, `Strings`, `Spring`, `IModule`, `Isle.Tests`, `TimerPill.cs`, `Fact`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **What connects `net10.0-windows10.0.19041.0`, `NAudio.Wasapi (3.3.0)`, `System.Management (11.0.0)` to the rest of the system?**
  _314 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.10080645161290322 - nodes in this community are weakly interconnected._
- **Why does `Modules (`src/Isle/Modules`)` connect `NotesStore` to `BluetoothDevices.ListAsync`, `TimersController`, `MediaSession`, `Color`, `TimerBook`, `MediaModule`, `ReminderParser`, `MediaState`, `FakeMediaSource`, `TimersSnapshots.Register`, `App.StartModules`, `AudioVolume`, `VpnDetector.Detect`, `RepeatRule`, `List`, `DateTimeOffset`, `Theme`, `PlaybackMath.Choose`, `IdleHttp`, `ControlServices`, `HotkeyService`, `TrayIcon.Create`, `DECISIONS`, `TimeSpan`, `NotesModule`, `LyricsTracker`, `Radios`, `Lrc.Parse`, `ARCHITECTURE`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._
- **Should `ShelfView` be split into smaller, more focused modules?**
  _Cohesion score 0.0797872340425532 - nodes in this community are weakly interconnected._
- **Why does `MarkupWindow` connect `MarkupWindow` to `Action`, `Color`, `system_windows_media`, `Window`, `ClipboardView`, `SteamSource`, `List`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.1225296442687747 - nodes in this community are weakly interconnected._