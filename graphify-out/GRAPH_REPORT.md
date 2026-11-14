# Graph Report - island  (2026-11-14)

## Corpus Check
- 86 files · ~55,568 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 1557 nodes · 3964 edges · 86 communities (54 shown, 32 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 508 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d75446bf`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Fact
- SystemControls.cs
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- IntPtr
- TimersView
- IDisposable
- Logger.Error
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
- TimeSpan
- ReminderBook
- MediaModule
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
- TrayModule
- AudioVolume
- LyricsTracker
- DeviceState
- CoreAudio.cs
- Equalizer
- StopwatchState
- MediaSnapshots.Register
- LyricsService
- Strings.cs
- RECT
- WinEventDelegate
- Reminder
- Grid
- Strings
- DateTimeOffset
- LyricsModule
- PROGRESS
- Memory
- IMMNotificationClient
- MonitorEnumProc
- IAudioEndpointVolume
- IslandWindow.Collapse
- Grid
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- IdlePillView
- Spring
- EDataFlow
- IslandWindow.ShowTab
- MusicPill
- LrclibHttp
- NotesView.cs
- ControlSnapshots.Tab

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 56 edges
2. `IslandWindow` - 54 edges
3. `Isle.Core` - 51 edges
4. `NotesView` - 47 edges
5. `Native` - 46 edges
6. `ControlView` - 46 edges
7. `Modules (`src/Isle/Modules`)` - 43 edges
8. `TimersController` - 40 edges
9. `MusicView` - 37 edges
10. `MediaState` - 37 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Modules (`src/Isle/Modules`)` --references--> `AudioVolume`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/ControlCenter/AudioVolume.cs
- `Modules (`src/Isle/Modules`)` --references--> `BluetoothDevices`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/ControlCenter/Bluetooth.cs
- `Modules (`src/Isle/Modules`)` --references--> `ControlServices`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/ControlCenter/ControlModule.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs

## Import Cycles
- None detected.

## Communities (86 total, 32 thin omitted)

### Community 1 - "Fact"
Cohesion: 0.18
Nodes (5): Core (`src/Isle/Core`), SettingsTests, StringsTests, TempDir, Path

### Community 2 - "SystemControls.cs"
Cohesion: 0.11
Nodes (3): Isle.Modules.ControlCenter, Isle.Tests, Isle.Views.Control

### Community 3 - "ModuleHost"
Cohesion: 0.07
Nodes (20): ARCHITECTURE, Process shape, Styles / Views, Tests (`tests/Isle.Tests`), Entry, IModule, Id, ModuleHost (+12 more)

### Community 4 - "IslandWindow"
Cohesion: 0.12
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "IntPtr"
Cohesion: 0.24
Nodes (4): TrayIcon, ICONINFO, NOTIFYICONDATA, TrayIconTests

### Community 8 - "IDisposable"
Cohesion: 0.11
Nodes (11): ControlServices, Brightness, Radios, VpnEnvironment, Brightness, Level, Supported, Radios (+3 more)

### Community 9 - "Logger.Error"
Cohesion: 0.08
Nodes (8): Autostart, IsEnabled, JsonStore, Logger, MediaSession, Current, PlaybackMath, VERIFICATION

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.17
Nodes (8): net10.0-windows10.0.19041.0, Microsoft.NET.Test.Sdk (17.14.1), System.Management (11.0.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "IReadOnlyList"
Cohesion: 0.09
Nodes (15): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+7 more)

### Community 12 - "Color"
Cohesion: 0.14
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerBook"
Cohesion: 0.11
Nodes (8): TimerBook, Doc, Finished, NextEnd, Now, Soonest, Stopwatch, Timers

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimersController"
Cohesion: 0.16
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 22 - "TimerItem"
Cohesion: 0.20
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.06
Nodes (21): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly (+13 more)

### Community 26 - "TimeSpan"
Cohesion: 0.07
Nodes (10): Gesture, IsValid, Lrc, LrcLine, DurationParser, LrcTests, PlaybackTests, ExcludedAppTests (+2 more)

### Community 27 - "ReminderBook"
Cohesion: 0.16
Nodes (7): ReminderBook, Doc, NextDue, Reminders, Showing, Upcoming, ReminderBanner

### Community 28 - "MediaModule"
Cohesion: 0.18
Nodes (7): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, TrackBanner

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.18
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 31 - "NotesView"
Cohesion: 0.05
Nodes (23): TabInfo, NotesSnapshots, Note, Created, Folder, Id, Pinned, Plain (+15 more)

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.17
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.27
Nodes (3): TimersModule, Controller, Id

### Community 37 - "LyricsService.cs"
Cohesion: 0.15
Nodes (8): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced, TrackQuery, CacheKey

### Community 38 - "App.StartModules"
Cohesion: 0.13
Nodes (8): Modules (`src/Isle/Modules`), ControlModule, Id, WindowsTheme, IsDark, NotesModule, Id, Store

### Community 39 - "MusicView.cs"
Cohesion: 0.19
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 40 - "TimerPill.cs"
Cohesion: 0.21
Nodes (5): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests, TimerBookTests

### Community 41 - "TabBar"
Cohesion: 0.06
Nodes (20): DECISIONS, M1, M2, M3, M4, Performance measurements, EventBus, Unsubscriber (+12 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.19
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.12
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.13
Nodes (7): IslandWindow, IsHiddenByUser, PasteTarget, Handle, Hotkeys, Pill, Watcher

### Community 46 - "Isle.Core"
Cohesion: 0.12
Nodes (5): Isle.Core, Isle, Isle.Modules.Hotkeys, Isle.Modules.Tray, PillPriority

### Community 47 - "Settings"
Cohesion: 0.10
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 49 - "AudioVolume"
Cohesion: 0.14
Nodes (5): AudioVolume, Available, Level, Muted, IMMDeviceEnumerator

### Community 50 - "LyricsTracker"
Cohesion: 0.20
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 51 - "DeviceState"
Cohesion: 0.13
Nodes (9): BluetoothDevices, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice (+1 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.14
Nodes (6): CoreAudioIds, IKsControl, IPropertyStore, KsProperty, PropertyKey, PropVariant

### Community 53 - "Equalizer"
Cohesion: 0.29
Nodes (3): ArtImage, Equalizer, Playing

### Community 54 - "StopwatchState"
Cohesion: 0.12
Nodes (12): Paths, LogFile, Root, SettingsFile, StopwatchState, Accumulated, Active, IsRunning (+4 more)

### Community 56 - "LyricsService"
Cohesion: 0.26
Nodes (5): Service, ILyricsHttp, LyricsService, FakeHttp, LyricsServiceTests

### Community 58 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

### Community 60 - "Reminder"
Cohesion: 0.20
Nodes (9): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderDoc (+1 more)

### Community 61 - "Grid"
Cohesion: 0.53
Nodes (3): MarqueeText, Inner, Text

### Community 62 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 67 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 76 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 83 - "ControlSnapshots.Tab"
Cohesion: 0.40
Nodes (3): ControlSnapshots, VpnEntry, CanToggle

## Knowledge Gaps
- **237 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `LastExternal`, `IdleView` (+232 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 392 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `App.StartModules` to `ModuleHost`, `IntPtr`, `IDisposable`, `Logger.Error`, `IReadOnlyList`, `Color`, `TimerBook`, `TimersController`, `ReminderParser`, `TimeSpan`, `ReminderBook`, `MediaModule`, `FakeMediaSource`, `NotesView`, `TimersSnapshots.Register`, `Modules (`src/Isle/Modules`)`, `TabBar`, `AudioVolume`, `LyricsTracker`, `DeviceState`, `MediaSnapshots.Register`, `LyricsService`?**
  _High betweenness centrality (0.145) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _237 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.11 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `Native`, `IslandWindow`, `IntPtr`, `MusicView.cs`, `IslandWindow.Collapse`, `App.StartModules`, `IReadOnlyList`, `Screens.All`, `IslandWindow.ShowTab`, `Window`, `IslandWindow.ApplyExStyle`, `Isle.Core`, `EventArgs`, `MusicView`, `IslandWindow.HideMoreOverlay`, `TimeSpan`, `NotesView`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Should `ModuleHost` be split into smaller, more focused modules?**
  _Cohesion score 0.06654567453115548 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Fact`, `SystemControls.cs`, `ModuleHost`, `system_windows`, `LyricsService.cs`, `MusicView.cs`, `TimerPill.cs`, `TabBar`, `Screens.All`, `Spring`, `NotesView.cs`, `Strings.cs`, `Strings`, `NotesView`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.11956521739130435 - nodes in this community are weakly interconnected._