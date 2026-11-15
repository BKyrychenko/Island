# Graph Report - island  (2026-11-15)

## Corpus Check
- 136 files · ~96,487 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2702 nodes · 7502 edges · 156 communities (102 shown, 54 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1010 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6c354119`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Dictionary
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
- TrayIcon.Create
- ControlView
- Action
- TempDir
- Task
- LyricsModule
- system_windows
- Providers.cs
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows_media
- Settings
- NotesStore
- AudioVolume
- IEnumerable
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- ShelfView.cs
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
- Action
- FakeSink
- IMMNotificationClient
- IndicatorPill
- RepeatKind
- Gesture
- RegistryWatcher
- DECISIONS
- TimerItem
- TimeSpan
- PillItem
- IslandWindow.Expand
- TimersSnapshots.Register
- IKsControl
- AiTests
- Units
- Conversation
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
- MarkupWindow.cs
- WeatherView.ShowReport
- SteamSource
- ToolsTests
- App.StartModules
- TimerPill.cs
- ScreenGrab.WindowRects
- ModuleHost
- ChatAttachment
- Fuzzy.Score
- ObsAuth.Response
- AnthropicProvider
- Paths
- Secret
- BluetoothDevices.ListAsync
- IslandWindow
- ChatMessage
- KnownFolders.SHGetKnownFolderPath
- OpenAiProvider
- ClipboardView
- WeatherKind
- ShelfModule
- WeatherService
- BitmapSource
- Isle.Modules.Commands
- JsonStore.Save
- ClipboardService.Capture
- Brightness
- Isle.Tests
- ShellIcons.For
- NotesModule
- NotesView.cs
- ClipFile
- RECT
- INPUT
- LowLevelKeyboardProc
- MonitorEnumProc
- WinEventDelegate

## God Nodes (most connected - your core abstractions)
1. `Native` - 88 edges
2. `Isle.Core` - 86 edges
3. `Recorder` - 50 edges
4. `Modules (`src/Isle/Modules`)` - 50 edges
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
- `Last action` --references--> `AiConfig`  [INFERRED]
  PROGRESS.md → src/Isle/Modules/AiChat/AiModels.cs
- `Last action` --references--> `AiException`  [INFERRED]
  PROGRESS.md → src/Isle/Modules/AiChat/AiModels.cs
- `Last action` --references--> `AnthropicProvider`  [INFERRED]
  PROGRESS.md → src/Isle/Modules/AiChat/AnthropicProvider.cs

## Import Cycles
- None detected.

## Communities (156 total, 54 thin omitted)

### Community 0 - "Native"
Cohesion: 0.10
Nodes (3): EnumWindowsProc, Native, POINT

### Community 1 - "Dictionary"
Cohesion: 0.07
Nodes (16): Isle.Modules.Shelf, ClipSnapshots, CurrencyParser, ShelfFile, Items, Version, ShelfItem, Added (+8 more)

### Community 2 - "SystemControls.cs"
Cohesion: 0.13
Nodes (3): Isle.Core, Isle.Modules.ControlCenter, Isle.Views.Control

### Community 3 - "RatesSnapshot"
Cohesion: 0.17
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.13
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.16
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 8 - "ClipboardService"
Cohesion: 0.10
Nodes (15): ClipboardService, ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth (+7 more)

### Community 9 - "MediaSession"
Cohesion: 0.14
Nodes (5): Autostart, IsEnabled, Logger, MediaSession, Current

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.12
Nodes (13): net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3), xunit (2.9.3) (+5 more)

### Community 11 - "IReadOnlyList"
Cohesion: 0.15
Nodes (9): EmptyVpnEnvironment, LocalAppData, ProgramFiles, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment, LocalAppData (+1 more)

### Community 12 - "Color"
Cohesion: 0.12
Nodes (6): Theme, Accent, IsDark, ArtColors, ColorText, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.06
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.09
Nodes (24): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+16 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.08
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "MediaModule"
Cohesion: 0.10
Nodes (12): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, ArtImage, Equalizer (+4 more)

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 26 - "TimeSpan"
Cohesion: 0.18
Nodes (3): Calculator, Result, CurrencyQuery

### Community 27 - "Node"
Cohesion: 0.16
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.10
Nodes (6): CalculatorTests, UnitTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.08
Nodes (21): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots, MediaState, Accent, Album (+13 more)

### Community 30 - "WeatherModule"
Cohesion: 0.12
Nodes (7): RainAlert, WeatherModule, Fahrenheit, HasPlace, Id, S, WeatherPill

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.16
Nodes (8): Modules (`src/Isle/Modules`), M3, TimerFinished, TimersModule, Controller, Id, ReminderBanner, TimerDoneBanner

### Community 33 - "TrayIcon.Create"
Cohesion: 0.25
Nodes (3): TrayIcon, ICONINFO, TrayIconTests

### Community 36 - "TempDir"
Cohesion: 0.19
Nodes (6): Isle.Modules.Tray, SettingsTests, TempDir, Path, FakeHttp, LyricsServiceTests

### Community 37 - "Task"
Cohesion: 0.22
Nodes (5): Place, IWeatherSource, OpenMeteoSource, WeatherActions, FakeSource

### Community 38 - "LyricsModule"
Cohesion: 0.33
Nodes (3): LyricsModule, Id, Service

### Community 39 - "system_windows"
Cohesion: 0.11
Nodes (4): Isle.Views.Live, Isle, Isle.Modules.LiveEvents, PillPriority

### Community 41 - "TabBar"
Cohesion: 0.24
Nodes (4): TabBar, AvailableWidth, Hidden, SelectedId

### Community 42 - "PreserveSig"
Cohesion: 0.11
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.26
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.08
Nodes (11): ARCHITECTURE, Process shape, Shell, Styles / Views, Tests (`tests/Isle.Tests`), Application, App, Host (+3 more)

### Community 45 - "IslandWindow"
Cohesion: 0.22
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "system_windows_media"
Cohesion: 0.15
Nodes (3): Isle.Modules.Clipboard, Isle.Views, Isle.Views.Clipboard

### Community 47 - "Settings"
Cohesion: 0.11
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "NotesStore"
Cohesion: 0.19
Nodes (6): NotesSnapshots, NotesStore, Doc, Folders, Notes, NotesStoreTests

### Community 49 - "AudioVolume"
Cohesion: 0.16
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "IEnumerable"
Cohesion: 0.08
Nodes (19): M8, CommandSnapshots, AppsProvider, AskAi, CalculatorProvider, Cmd, CommandItem, Confirm (+11 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.15
Nodes (8): Client, VpnDetector, VpnEntry, CanToggle, FakeEnv, LocalAppData, ProgramFiles, VpnDetectorTests

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, DownloadDetectorTests

### Community 54 - "ShelfView.cs"
Cohesion: 0.22
Nodes (3): Isle.Modules.Weather, Isle.Views.Shelf, Isle.Views.Weather

### Community 58 - "Action"
Cohesion: 0.11
Nodes (3): ToolsModule, Id, View

### Community 60 - "List"
Cohesion: 0.11
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.17
Nodes (5): DiskEntry, LyricsResult, LyricsService, TrackQuery, CacheKey

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 66 - "Dictionary"
Cohesion: 0.18
Nodes (5): LiveEventsModule, Id, PillSink, Ui, ILiveSource

### Community 67 - "IslandWindow"
Cohesion: 0.16
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.24
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.13
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 71 - "VpnIndicatorSource"
Cohesion: 0.12
Nodes (4): CapsLockSource, NotificationsSource, Unavailable, VpnIndicatorSource

### Community 72 - "CommandBarView"
Cohesion: 0.15
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 74 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 76 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 77 - "ForegroundWatcher"
Cohesion: 0.12
Nodes (5): M6, ForegroundWatcher, LastExternal, MOUSEINPUT, NOTIFYICONDATA

### Community 79 - "IDisposable"
Cohesion: 0.17
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 80 - "IdleHttp"
Cohesion: 0.26
Nodes (3): IdleHttp, ILyricsHttp, LrclibHttp

### Community 81 - "Note"
Cohesion: 0.11
Nodes (14): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+6 more)

### Community 82 - "MarkupWindow"
Cohesion: 0.06
Nodes (14): RecordOptions, PxRect, IsEmpty, MarkupWindow, Stroke, PickButton, IsChecked, OverlayWindow (+6 more)

### Community 83 - "ControlServices"
Cohesion: 0.31
Nodes (5): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "CoreAudio.cs"
Cohesion: 0.13
Nodes (10): CoreAudioIds, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice (+2 more)

### Community 87 - "Action"
Cohesion: 0.22
Nodes (5): Icon, Data, Filled, Foreground, Stroke

### Community 88 - "FakeSink"
Cohesion: 0.24
Nodes (4): ILiveSink, Ui, FakeSink, Ui

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
Cohesion: 0.14
Nodes (10): Core (`src/Isle/Core`), DECISIONS, M0, M1, M10, M11, M4, M7 (+2 more)

### Community 95 - "TimerItem"
Cohesion: 0.18
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 97 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 99 - "TimersSnapshots.Register"
Cohesion: 0.17
Nodes (6): TimersSnapshots, Ring, Brush, Progress, Track, TimerPill

### Community 101 - "AiTests"
Cohesion: 0.29
Nodes (4): AiConfig, IsReady, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Conversation"
Cohesion: 0.17
Nodes (11): Conversation, Id, Messages, SystemPrompt, Title, Updated, ChatStore, All (+3 more)

### Community 104 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 107 - "LyricsTracker"
Cohesion: 0.22
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 108 - "AppIndex"
Cohesion: 0.10
Nodes (12): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+4 more)

### Community 109 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 110 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 112 - "IModule"
Cohesion: 0.28
Nodes (3): Entry, IModule, Id

### Community 113 - "ClipStore"
Cohesion: 0.24
Nodes (3): ClipStore, Entries, Limit

### Community 114 - "Radios"
Cohesion: 0.22
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 115 - "Lrc.Parse"
Cohesion: 0.20
Nodes (3): Lrc, LrcLine, LrcTests

### Community 119 - "SteamSource"
Cohesion: 0.23
Nodes (4): SpeedMeter, BytesPerSecond, Item, SteamSource

### Community 121 - "App.StartModules"
Cohesion: 0.15
Nodes (4): ClipboardModule, Id, ControlModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.31
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 124 - "ModuleHost"
Cohesion: 0.17
Nodes (9): ModuleHost, Ids, ModuleState, Failed, Running, Stopped, Fake, Id (+1 more)

### Community 125 - "ChatAttachment"
Cohesion: 0.13
Nodes (14): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+6 more)

### Community 128 - "AnthropicProvider"
Cohesion: 0.27
Nodes (4): AiException, Detail, Key, AnthropicProvider

### Community 129 - "Paths"
Cohesion: 0.29
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 132 - "IslandWindow"
Cohesion: 0.18
Nodes (9): IslandWindow, Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect (+1 more)

### Community 133 - "ChatMessage"
Cohesion: 0.24
Nodes (5): ChatMessage, Attachments, Error, Role, Text

### Community 137 - "WeatherKind"
Cohesion: 0.22
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 144 - "Brightness"
Cohesion: 0.29
Nodes (3): Brightness, Level, Supported

### Community 148 - "NotesModule"
Cohesion: 0.40
Nodes (3): NotesModule, Id, Store

### Community 150 - "ClipFile"
Cohesion: 0.67
Nodes (3): ClipFile, Entries, Version

### Community 151 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **364 isolated node(s):** `net10.0-windows10.0.19041.0`, `Anthropic (13.8.0)`, `NAudio.Wasapi (3.3.0)`, `System.Management (11.0.0)`, `System.Security.Cryptography.ProtectedData (11.0.0)` (+359 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 606 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **54 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Isle.Core` connect `SystemControls.cs` to `Dictionary`, `RatesSnapshot`, `Isle.Modules.Commands`, `Isle.Tests`, `NotesView.cs`, `TempDir`, `Task`, `system_windows`, `Providers.cs`, `Screens.All`, `system_windows_media`, `ShelfView.cs`, `Recorder.cs`, `Strings.cs`, `Isle.Modules.Media`, `IDisposable`, `Note`, `AppIndex`, `Strings`, `Spring`, `MarkupWindow.cs`, `TimerPill.cs`, `ModuleHost`, `ChatAttachment`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **What connects `net10.0-windows10.0.19041.0`, `Anthropic (13.8.0)`, `NAudio.Wasapi (3.3.0)` to the rest of the system?**
  _364 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.09577677224736049 - nodes in this community are weakly interconnected._
- **Why does `Modules (`src/Isle/Modules`)` connect `TimersSnapshots.Register` to `BluetoothDevices.ListAsync`, `TimersController`, `MediaSession`, `WeatherService`, `Color`, `Brightness`, `WeatherReport`, `NotesModule`, `TimerBook`, `MediaModule`, `ReminderParser`, `MediaState`, `WeatherModule`, `TrayIcon.Create`, `Task`, `App`, `NotesStore`, `AudioVolume`, `VpnDetector.Detect`, `Action`, `List`, `DateTimeOffset`, `PlaybackMath.Choose`, `IdleHttp`, `ControlServices`, `HotkeyService`, `TimeSpan`, `TimersSnapshots.Register`, `WindowsTheme`, `LyricsTracker`, `Radios`, `Lrc.Parse`, `WeatherView.ShowReport`, `App.StartModules`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.06892655367231638 - nodes in this community are weakly interconnected._
- **Why does `MarkupWindow` connect `MarkupWindow` to `Dictionary`, `IslandWindow`, `Task`, `ClipboardView`, `Color`, `Window`, `MarkupWindow.cs`, `Action`, `Action`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.12962962962962962 - nodes in this community are weakly interconnected._