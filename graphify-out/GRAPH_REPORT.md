# Graph Report - island  (2026-11-15)

## Corpus Check
- 104 files · ~68,653 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 1963 nodes · 5236 edges · 102 communities (72 shown, 30 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 726 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `83a997a5`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- ShelfView
- SystemControls.cs
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- Modules (`src/Isle/Modules`)
- TimersView
- ClipboardService
- MediaSession
- Isle.Tests.csproj
- IReadOnlyList
- Color
- Isle
- Window
- TimerItem
- PillItem
- MusicView
- ResourceDictionary
- TimersController
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
- LyricsService.cs
- App.StartModules
- system_windows
- TimerPill.cs
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- Isle.Tests
- Settings
- NotesStore
- AudioVolume
- LyricsTracker
- DeviceState
- CoreAudio.cs
- DownloadsSource
- IslandWindow
- LiveEventsModule.Create
- Logger.Error
- Strings.cs
- RECT
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
- Thickness
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- IdlePillView
- Fact
- EDataFlow
- HotkeyService
- TrayModule
- IDisposable
- LrclibHttp
- Isle.Core
- Note
- Action
- IModule
- DeviceState
- ObsSource
- Sources.cs
- Func
- KeyboardLayoutSource
- IndicatorPill
- IslandWindow.ShowTab
- Gesture
- RegistryWatcher
- PlaybackMath.Choose
- LyricsModule
- ARCHITECTURE
- ObsAuth.Response
- NotesModule
- RepeatKind
- BluetoothDevices.ListAsync
- LyricsStatus

## God Nodes (most connected - your core abstractions)
1. `Native` - 72 edges
2. `Isle.Core` - 64 edges
3. `NotesView` - 47 edges
4. `ControlView` - 46 edges
5. `IslandWindow` - 46 edges
6. `IslandWindow` - 43 edges
7. `Modules (`src/Isle/Modules`)` - 43 edges
8. `TimersController` - 40 edges
9. `MusicView` - 37 edges
10. `MediaState` - 37 edges

## Surprising Connections (you probably didn't know these)
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M6` --references--> `ForegroundWatcher`  [INFERRED]
  DECISIONS.md → src/Isle/Core/ForegroundWatcher.cs
- `M6` --references--> `ILiveSink`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/LiveEvents/Sources.cs

## Import Cycles
- None detected.

## Communities (102 total, 30 thin omitted)

### Community 0 - "Native"
Cohesion: 0.09
Nodes (6): INPUT, LowLevelKeyboardProc, MonitorEnumProc, Native, POINT, WinEventDelegate

### Community 1 - "ShelfView"
Cohesion: 0.09
Nodes (12): ShelfFile, Items, Version, ShelfItem, Added, Id, Path, ShelfStore (+4 more)

### Community 3 - "ModuleHost"
Cohesion: 0.31
Nodes (3): Process shape, ModuleHost, Ids

### Community 4 - "IslandWindow"
Cohesion: 0.11
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.15
Nodes (11): Modules (`src/Isle/Modules`), DECISIONS, M1, M3, M4, M6, M7, Performance measurements (+3 more)

### Community 8 - "ClipboardService"
Cohesion: 0.12
Nodes (3): ClipboardService, InputUnion, KEYBDINPUT

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.15
Nodes (9): net10.0-windows10.0.19041.0, Microsoft.NET.Test.Sdk (17.14.1), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, net10.0-windows10.0.19041.0 (+1 more)

### Community 11 - "IReadOnlyList"
Cohesion: 0.08
Nodes (16): ClipPrivacy, EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles (+8 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerItem"
Cohesion: 0.10
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerDoc, Stopwatch, Timers (+9 more)

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimersController"
Cohesion: 0.08
Nodes (19): TimerBook, Doc, Finished, NextEnd, Now, Soonest, Stopwatch, Timers (+11 more)

### Community 22 - "MediaModule"
Cohesion: 0.15
Nodes (9): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPill, PillWidth (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 26 - "TimeSpan"
Cohesion: 0.13
Nodes (4): Lrc, LrcLine, DurationParser, LrcTests

### Community 27 - "Node"
Cohesion: 0.16
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.14
Nodes (4): LiveMiscTests, ExcludedAppTests, GestureTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.14
Nodes (5): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.23
Nodes (6): TimersSnapshots, Ring, Brush, Progress, Track, TimerPill

### Community 33 - "IntPtr"
Cohesion: 0.09
Nodes (9): ForegroundWatcher, LastExternal, KnownFolders, Downloads, TrayIcon, ICONINFO, MOUSEINPUT, NOTIFYICONDATA (+1 more)

### Community 34 - "ControlView"
Cohesion: 0.15
Nodes (4): VpnEntry, CanToggle, ControlView, Tile

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.20
Nodes (5): TimersModule, Controller, Id, ReminderBanner, TimerDoneBanner

### Community 38 - "App.StartModules"
Cohesion: 0.11
Nodes (6): ClipboardModule, Id, ControlModule, Id, ShelfModule, Id

### Community 40 - "TimerPill.cs"
Cohesion: 0.31
Nodes (5): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests, TimerBookTests

### Community 41 - "TabBar"
Cohesion: 0.12
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.23
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.13
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.13
Nodes (4): Shell, IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "Isle.Tests"
Cohesion: 0.18
Nodes (5): Isle.Modules.Clipboard, Isle.Views.Shelf, Isle.Modules.Shelf, Isle.Tests, Isle.Views.Clipboard

### Community 47 - "Settings"
Cohesion: 0.08
Nodes (21): Isle.Modules.Tray, Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys (+13 more)

### Community 48 - "NotesStore"
Cohesion: 0.17
Nodes (9): NotesSnapshots, NotesDoc, Folders, Notes, NotesStore, Doc, Folders, Notes (+1 more)

### Community 49 - "AudioVolume"
Cohesion: 0.17
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "LyricsTracker"
Cohesion: 0.24
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 51 - "DeviceState"
Cohesion: 0.16
Nodes (5): IKsControl, IMMDevice, IMMDeviceCollection, IMMDeviceEnumerator, KsProperty

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.12
Nodes (7): DownloadDetector, SpeedMeter, BytesPerSecond, DownloadsSource, Item, SteamSource, DownloadDetectorTests

### Community 54 - "IslandWindow"
Cohesion: 0.14
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 55 - "LiveEventsModule.Create"
Cohesion: 0.15
Nodes (5): CapsLockSource, ILiveSource, NotificationsSource, Unavailable, VpnIndicatorSource

### Community 56 - "Logger.Error"
Cohesion: 0.11
Nodes (7): Autostart, IsEnabled, JsonStore, Logger, Brightness, Level, Supported

### Community 58 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

### Community 60 - "List"
Cohesion: 0.11
Nodes (15): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+7 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 62 - "Equalizer"
Cohesion: 0.26
Nodes (3): ArtImage, Equalizer, Playing

### Community 63 - "DateTimeOffset"
Cohesion: 0.15
Nodes (9): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, TrackQuery, CacheKey, FakeHttp (+1 more)

### Community 64 - "TabInfo"
Cohesion: 0.19
Nodes (5): Paths, LogFile, Root, SettingsFile, TabInfo

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 66 - "Memory"
Cohesion: 0.32
Nodes (3): Core (`src/Isle/Core`), M0, Memory

### Community 67 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.26
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 71 - "LiveEventsModule"
Cohesion: 0.19
Nodes (4): LiveEventsModule, Id, PillSink, Ui

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.10
Nodes (17): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 8. Milestones (in order; commit after each; update PROGRESS.md and the graph) (+9 more)

### Community 74 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 75 - "Fact"
Cohesion: 0.05
Nodes (27): Secret, Spring, ClipSnapshots, ClipEntry, Cipher, Id, Image, ImageHash (+19 more)

### Community 76 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 79 - "IDisposable"
Cohesion: 0.19
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 81 - "Isle.Core"
Cohesion: 0.19
Nodes (4): Isle.Views.Notes, Isle.Core, Isle.Modules.Notes, PillPriority

### Community 82 - "Note"
Cohesion: 0.13
Nodes (11): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+3 more)

### Community 83 - "Action"
Cohesion: 0.12
Nodes (9): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots, Radios, BluetoothOn, Denied (+1 more)

### Community 84 - "IModule"
Cohesion: 0.13
Nodes (10): Entry, IModule, Id, ModuleState, Failed, Running, Stopped, Fake (+2 more)

### Community 85 - "DeviceState"
Cohesion: 0.25
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 88 - "Func"
Cohesion: 0.19
Nodes (4): ILiveSink, Ui, FakeSink, Ui

### Community 92 - "Gesture"
Cohesion: 0.22
Nodes (4): Isle.Modules.Hotkeys, Gesture, IsValid, HotkeyInfo

### Community 94 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (4): M2, PlaybackMath, PlaybackTests, VERIFICATION

### Community 96 - "ARCHITECTURE"
Cohesion: 0.29
Nodes (4): ARCHITECTURE, Styles / Views, Tests (`tests/Isle.Tests`), TestSetup

### Community 99 - "NotesModule"
Cohesion: 0.32
Nodes (3): NotesModule, Id, Store

### Community 100 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

## Knowledge Gaps
- **270 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `FileDropTarget`, `Pill` (+265 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 443 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **30 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `MediaSession`, `IReadOnlyList`, `Color`, `TimersController`, `MediaModule`, `ReminderParser`, `TimeSpan`, `FakeMediaSource`, `TimersSnapshots.Register`, `IntPtr`, `Modules (`src/Isle/Modules`)`, `App.StartModules`, `NotesStore`, `AudioVolume`, `LyricsTracker`, `Logger.Error`, `List`, `DateTimeOffset`, `Thickness`, `HotkeyService`, `Action`, `Gesture`, `PlaybackMath.Choose`, `ARCHITECTURE`, `NotesModule`, `BluetoothDevices.ListAsync`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _270 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.09250693802035152 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `SystemControls.cs`, `App.xaml.cs`, `LyricsService.cs`, `Isle.Modules.Media`, `system_windows`, `TimerPill.cs`, `Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)`, `Screens.All`, `Fact`, `Isle.Tests`, `IDisposable`, `NotesStore`, `Settings`, `IModule`, `Sources.cs`, `Strings.cs`, `Gesture`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **Should `ShelfView` be split into smaller, more focused modules?**
  _Cohesion score 0.08687943262411348 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `ShelfView`, `IslandWindow`, `ClipboardService`, `Window`, `IntPtr`, `App.StartModules`, `system_windows`, `Screens.All`, `App`, `IslandWindow`, `Native.GetForegroundWindow`, `TabInfo`, `LiveEventsModule`, `Fact`, `Action`, `Sources.cs`, `Func`, `KeyboardLayoutSource`, `RegistryWatcher`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.11076923076923077 - nodes in this community are weakly interconnected._