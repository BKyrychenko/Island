# Graph Report - island  (2026-11-15)

## Corpus Check
- 116 files · ~76,554 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2180 nodes · 5898 edges · 122 communities (77 shown, 45 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 793 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `dbdf8464`
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
- Logger.Error
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
- system_windows_media
- Settings
- Modules (`src/Isle/Modules`)
- AudioVolume
- Action
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- IslandWindow
- LiveEventsModule.Create
- Button
- Strings.cs
- ShelfView
- List
- Grid
- Equalizer
- DateTimeOffset
- TabInfo
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
- LrclibHttp
- NotesFormattingTests.Formatting_survives_save_and_load
- Note
- ControlServices
- ModuleHost
- DeviceState
- ObsSource
- IndicatorPill.cs
- FakeSink
- MicCameraSource.InUse
- IndicatorPill
- IslandWindow.Expand
- Gesture
- RegistryWatcher
- PlaybackMath.Choose
- LyricsModule
- TimeSpan
- ObsAuth.Response
- TempDir
- NotesModule
- Calculator.TryEvaluate
- BluetoothDevices.ListAsync
- Units
- CommandItem
- LyricsStatus
- system_windows_controls
- Parser
- LyricsServiceTests
- ShellIcons.For
- Strings
- CommandModule
- Spring
- MusicPill
- ClipboardView
- Radios
- VpnIndicatorSource
- ClipboardService.SendCtrlV
- NotesStoreTests.Crud_and_persistence
- EventBusTests.Publish_reaches_subscribers_until_disposed_and_survives_throwing_handlers
- NotesSnapshots.Register
- LowLevelKeyboardProc

## God Nodes (most connected - your core abstractions)
1. `Native` - 74 edges
2. `Isle.Core` - 71 edges
3. `IslandWindow` - 53 edges
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
- `Modules (`src/Isle/Modules`)` --references--> `BluetoothDevices`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/ControlCenter/Bluetooth.cs
- `Modules (`src/Isle/Modules`)` --references--> `VpnDetector`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/ControlCenter/Vpn.cs
- `Modules (`src/Isle/Modules`)` --references--> `ArtColors`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Media/ArtColors.cs

## Import Cycles
- None detected.

## Communities (122 total, 45 thin omitted)

### Community 0 - "Native"
Cohesion: 0.10
Nodes (8): INPUT, MonitorEnumProc, Native, POINT, RECT, Height, Width, WinEventDelegate

### Community 1 - "ShelfView"
Cohesion: 0.11
Nodes (12): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+4 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.16
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.14
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.13
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 9 - "Logger.Error"
Cohesion: 0.06
Nodes (18): Autostart, IsEnabled, JsonStore, Logger, AppIndex, Entries, EntryKind, App (+10 more)

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.15
Nodes (9): net10.0-windows10.0.19041.0, Microsoft.NET.Test.Sdk (17.14.1), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, net10.0-windows10.0.19041.0 (+1 more)

### Community 11 - "IVpnEnvironment"
Cohesion: 0.15
Nodes (9): EmptyVpnEnvironment, LocalAppData, ProgramFiles, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment, LocalAppData (+1 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerItem"
Cohesion: 0.20
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 19 - "MusicView"
Cohesion: 0.15
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, MusicView, LyricsOpen

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.08
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "MediaModule"
Cohesion: 0.16
Nodes (7): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, TrackBanner

### Community 23 - "ReminderParser"
Cohesion: 0.07
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (7): CalculatorTests, UnitTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.10
Nodes (17): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+9 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.21
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.19
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 33 - "IntPtr"
Cohesion: 0.15
Nodes (6): KnownFolders, Downloads, TrayIcon, ICONINFO, MOUSEINPUT, NOTIFYICONDATA

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.18
Nodes (4): TimersModule, Controller, Id, ReminderBanner

### Community 38 - "App.StartModules"
Cohesion: 0.13
Nodes (6): ClipboardModule, Id, ControlModule, Id, ShelfModule, Id

### Community 39 - "system_windows"
Cohesion: 0.14
Nodes (3): Isle, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents

### Community 40 - "Isle.Core"
Cohesion: 0.13
Nodes (6): Isle.Views.Timers, Isle.Core, Isle.Tests, Isle.Modules.Reminders, Isle.Modules.Timers, PillPriority

### Community 41 - "TabBar"
Cohesion: 0.13
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.23
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.11
Nodes (6): Shell, Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.14
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "system_windows_media"
Cohesion: 0.18
Nodes (4): Isle.Modules.Clipboard, Isle.Modules.Tray, Isle.Views.Shelf, Isle.Views.Clipboard

### Community 47 - "Settings"
Cohesion: 0.10
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.26
Nodes (5): Modules (`src/Isle/Modules`), NotesStore, Doc, Folders, Notes

### Community 49 - "AudioVolume"
Cohesion: 0.14
Nodes (5): AudioVolume, Available, Level, Muted, IMMDeviceEnumerator

### Community 50 - "Action"
Cohesion: 0.19
Nodes (8): AppsProvider, CalculatorProvider, Cmd, CurrencyProvider, ICommandProvider, SystemProvider, TimerProvider, WebProvider

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.15
Nodes (8): Client, VpnDetector, VpnEntry, CanToggle, FakeEnv, LocalAppData, ProgramFiles, VpnDetectorTests

### Community 52 - "CoreAudio.cs"
Cohesion: 0.14
Nodes (6): CoreAudioIds, IKsControl, IPropertyStore, KsProperty, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.15
Nodes (5): DownloadDetector, SpeedMeter, BytesPerSecond, DownloadsSource, Item

### Community 54 - "IslandWindow"
Cohesion: 0.11
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 55 - "LiveEventsModule.Create"
Cohesion: 0.12
Nodes (9): CapsLockSource, ILiveSink, Ui, ILiveSource, KeyboardLayoutSource, MicCameraSource, NotificationsSource, Unavailable (+1 more)

### Community 56 - "Button"
Cohesion: 0.29
Nodes (4): Brightness, Level, Supported, Tile

### Community 60 - "List"
Cohesion: 0.08
Nodes (24): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+16 more)

### Community 61 - "Grid"
Cohesion: 0.25
Nodes (3): MarqueeText, Inner, Text

### Community 62 - "Equalizer"
Cohesion: 0.26
Nodes (3): ArtImage, Equalizer, Playing

### Community 63 - "DateTimeOffset"
Cohesion: 0.24
Nodes (3): DiskEntry, LyricsResult, LyricsService

### Community 64 - "TabInfo"
Cohesion: 0.19
Nodes (5): Paths, LogFile, Root, SettingsFile, TabInfo

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 67 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.23
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 71 - "LiveEventsModule"
Cohesion: 0.16
Nodes (5): CurrencyParser, LiveEventsModule, Id, PillSink, Ui

### Community 72 - "CommandBarView"
Cohesion: 0.17
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.18
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 75 - "Fact"
Cohesion: 0.06
Nodes (26): Secret, ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth (+18 more)

### Community 76 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 80 - "LrclibHttp"
Cohesion: 0.23
Nodes (3): IdleHttp, ILyricsHttp, LrclibHttp

### Community 81 - "NotesFormattingTests.Formatting_survives_save_and_load"
Cohesion: 0.28
Nodes (3): Isle.Views.Notes, Isle.Modules.Notes, NotesFormattingTests

### Community 82 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 83 - "ControlServices"
Cohesion: 0.31
Nodes (5): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots

### Community 84 - "ModuleHost"
Cohesion: 0.05
Nodes (30): ARCHITECTURE, Core (`src/Isle/Core`), Process shape, Styles / Views, Tests (`tests/Isle.Tests`), DECISIONS, M0, M1 (+22 more)

### Community 85 - "DeviceState"
Cohesion: 0.13
Nodes (9): BluetoothDevices, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice (+1 more)

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "PlaybackMath.Choose"
Cohesion: 0.24
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 95 - "LyricsModule"
Cohesion: 0.33
Nodes (3): LyricsModule, Id, Service

### Community 98 - "TempDir"
Cohesion: 0.28
Nodes (3): SettingsTests, TempDir, Path

### Community 99 - "NotesModule"
Cohesion: 0.32
Nodes (3): NotesModule, Id, Store

### Community 100 - "Calculator.TryEvaluate"
Cohesion: 0.21
Nodes (3): Calculator, Result, CurrencyQuery

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "CommandItem"
Cohesion: 0.20
Nodes (9): CommandSnapshots, CommandItem, Confirm, IconKey, IconPath, Run, Score, Subtitle (+1 more)

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 105 - "system_windows_controls"
Cohesion: 0.22
Nodes (3): Isle.Views, Isle.Modules.Commands, Isle.Views.Commands

### Community 107 - "LyricsServiceTests"
Cohesion: 0.27
Nodes (4): TrackQuery, CacheKey, FakeHttp, LyricsServiceTests

### Community 109 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 114 - "Radios"
Cohesion: 0.25
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

## Knowledge Gaps
- **295 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `PasteTarget`, `IsHiddenByUser` (+290 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 483 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **45 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IslandWindow` connect `IslandWindow` to `TabInfo`, `IntPtr`, `IslandWindow`, `App.StartModules`, `system_windows`, `ClipboardService`, `Screens.All`, `ForegroundWatcher`, `Window`, `IslandWindow`, `MediaModule`, `LiveEventsModule.Create`, `ShelfView`, `IslandWindow.Expand`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _295 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.09696969696969697 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `ShelfView`, `SystemControls.cs`, `Isle.Modules.Media`, `LyricsService.cs`, `system_windows`, `Logger.Error`, `system_windows_controls`, `Screens.All`, `Strings`, `system_windows_media`, `IDisposable`, `Spring`, `NotesFormattingTests.Formatting_survives_save_and_load`, `Action`, `Note`, `ModuleHost`, `IndicatorPill.cs`, `Strings.cs`?**
  _High betweenness centrality (0.080) - this node is a cross-community bridge._
- **Should `ShelfView` be split into smaller, more focused modules?**
  _Cohesion score 0.10952380952380952 - nodes in this community are weakly interconnected._
- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `TimersController`, `Logger.Error`, `Color`, `MusicView`, `TimerBook`, `MediaModule`, `ReminderParser`, `TimeSpan`, `MediaState`, `FakeMediaSource`, `TimersSnapshots.Register`, `IntPtr`, `Modules (`src/Isle/Modules`)`, `App.StartModules`, `AudioVolume`, `VpnDetector.Detect`, `Button`, `List`, `DateTimeOffset`, `LrclibHttp`, `ControlServices`, `ModuleHost`, `DeviceState`, `PlaybackMath.Choose`, `TimeSpan`, `NotesModule`, `Radios`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.13157894736842105 - nodes in this community are weakly interconnected._