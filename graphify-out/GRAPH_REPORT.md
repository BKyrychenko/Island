# Graph Report - island  (2026-11-15)

## Corpus Check
- 143 files · ~104,654 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2845 nodes · 8023 edges · 150 communities (106 shown, 44 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1082 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `79d93eb1`
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
- Strings.T
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
- TimersModule
- MarkupWindow
- ControlView
- Logger.Error
- TempDir
- Task
- LyricsModule
- DateTimeOffset
- Providers.cs
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows
- Settings
- Attachments
- AudioVolume
- IEnumerable
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- Markdown.Parse
- KeyboardLayoutSource
- FakeMediaSource
- Action
- List
- Grid
- Strings.T
- DateTimeOffset
- FrameworkElement
- AiModule
- Dictionary
- Icon
- Isle.Modules.Media
- IAudioEndpointVolume
- VpnIndicatorSource
- CommandBarView
- Theme
- Fact
- PlaybackMath.Choose
- ForegroundWatcher
- TrayModule
- IDisposable
- IdleHttp
- OpenMeteoParser.Forecast
- OverlayWindow
- ControlServices
- HotkeyService
- CoreAudio.cs
- ObsSource
- CurrentWeather
- FakeSink
- IMMNotificationClient
- IndicatorPill
- RepeatKind
- Gesture
- RegistryWatcher
- DECISIONS
- MdBlockKind
- Regex
- PillItem
- FrameworkElement
- WeatherSnapshots.Register
- IKsControl
- AiTests
- Units
- Conversation
- ClipSnapshots.Register
- WindowsTheme
- Parser
- LyricsTracker
- AppIndex
- CurrencyParser.Parse
- EDataFlow
- Spring
- IModule
- ClipStore
- Radios
- Lrc.Parse
- ClipboardService.SendCtrlV
- MarkupWindow.cs
- WeatherData.cs
- SteamSource
- ToolsTests
- App.StartModules
- TimerPill.cs
- Gesture.TryParse
- ModuleHost
- ChatAttachment
- Fuzzy.Score
- LiveMiscTests
- AnthropicProvider
- Paths
- Secret
- BluetoothDevices.ListAsync
- IslandWindow
- Conversation
- KnownFolders.SHGetKnownFolderPath
- OpenAiProvider
- ClipboardView
- WeatherKind
- TestSetup
- Logger.Info
- CommandModule.Create
- ControlView.ControlView
- Isle.Tests
- IReadOnlyList
- NotesModule
- Isle.Core
- ClipEntry
- RECT
- MonitorEnumProc
- WinEventDelegate

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 90 edges
2. `Native` - 88 edges
3. `Thickness` - 67 edges
4. `Modules (`src/Isle/Modules`)` - 63 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 50 edges
7. `NotesView` - 47 edges
8. `ControlView` - 46 edges
9. `IslandWindow` - 46 edges
10. `MarkupWindow` - 42 edges

## Surprising Connections (you probably didn't know these)
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Modules (`src/Isle/Modules`)` --references--> `AiConfig`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/AiChat/AiModels.cs
- `Modules (`src/Isle/Modules`)` --references--> `AiModule`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/AiChat/AiModule.cs

## Import Cycles
- None detected.

## Communities (150 total, 44 thin omitted)

### Community 0 - "Native"
Cohesion: 0.07
Nodes (10): EnumWindowsProc, ICONINFO, INPUT, LowLevelKeyboardProc, MONITORINFOEX, MOUSEINPUT, Native, NOTIFYICONDATA (+2 more)

### Community 1 - "Dictionary"
Cohesion: 0.08
Nodes (14): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+6 more)

### Community 2 - "SystemControls.cs"
Cohesion: 0.09
Nodes (4): Isle.Views.Live, Isle.Modules.ControlCenter, Isle.Modules.LiveEvents, Isle.Views.Control

### Community 3 - "RatesSnapshot"
Cohesion: 0.16
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.05
Nodes (16): IslandWindow, CurrentTab, IdleView, TabList, IslandWindow, FileDropTarget, IsExpanded, Handle (+8 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.04
Nodes (37): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+29 more)

### Community 6 - "TimersController"
Cohesion: 0.11
Nodes (12): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+4 more)

### Community 8 - "ClipboardService"
Cohesion: 0.17
Nodes (3): ClipboardModule, Id, ClipboardService

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.11
Nodes (14): net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3) (+6 more)

### Community 12 - "Color"
Cohesion: 0.18
Nodes (3): ArtColors, ColorText, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.08
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.13
Nodes (12): HourWeather, PrecipSlot, WeatherReport, Current, Days, Fetched, Hours, Minutely (+4 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.07
Nodes (26): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+18 more)

### Community 22 - "MediaModule"
Cohesion: 0.14
Nodes (9): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPill, PillWidth (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.13
Nodes (5): ParsedReminder, ReminderParser, Rule, State, HasDate

### Community 26 - "TimeSpan"
Cohesion: 0.16
Nodes (4): Calculator, Result, CalculatorTests, UnitTests

### Community 27 - "Node"
Cohesion: 0.16
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (5): DownloadDetectorTests, PlaybackTests, ExcludedAppTests, DurationParserTests, ReminderParserTests

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (17): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+9 more)

### Community 30 - "WeatherModule"
Cohesion: 0.17
Nodes (5): WeatherModule, Fahrenheit, HasPlace, Id, S

### Community 31 - "NotesView"
Cohesion: 0.05
Nodes (25): NotesSnapshots, Note, Created, Folder, Id, Pinned, Plain, Preview (+17 more)

### Community 32 - "TimersModule"
Cohesion: 0.12
Nodes (10): TimersModule, Controller, Id, TimersSnapshots, Ring, Brush, Progress, Track (+2 more)

### Community 33 - "MarkupWindow"
Cohesion: 0.14
Nodes (4): MarkupWindow, Stroke, PickButton, IsChecked

### Community 35 - "Logger.Error"
Cohesion: 0.17
Nodes (3): OcrReader, Thickness, ToolsView

### Community 36 - "TempDir"
Cohesion: 0.19
Nodes (6): Core (`src/Isle/Core`), Isle.Modules.Tray, SettingsTests, TempDir, Path, NotesStoreTests

### Community 37 - "Task"
Cohesion: 0.17
Nodes (7): Place, IWeatherSource, OpenMeteoSource, WeatherService, LastFailed, WeatherActions, FakeSource

### Community 39 - "DateTimeOffset"
Cohesion: 0.20
Nodes (7): RepeatRule, DayOfMonth, Days, Interval, Kind, Repeats, RepeatRuleTests

### Community 40 - "Providers.cs"
Cohesion: 0.12
Nodes (3): Isle.Modules.Weather, Isle.Views.Weather, CapabilityUse

### Community 41 - "TabBar"
Cohesion: 0.23
Nodes (4): TabBar, AvailableWidth, Hidden, SelectedId

### Community 42 - "PreserveSig"
Cohesion: 0.11
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.33
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.10
Nodes (9): ARCHITECTURE, Process shape, Shell, Styles / Views, Application, App, Host, Island (+1 more)

### Community 45 - "IslandWindow"
Cohesion: 0.13
Nodes (4): ITabView, IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "system_windows"
Cohesion: 0.10
Nodes (5): Isle.Modules.Clipboard, Isle, Isle.Views, Isle.Views.Shelf, Isle.Views.Clipboard

### Community 47 - "Settings"
Cohesion: 0.11
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 48 - "Attachments"
Cohesion: 0.18
Nodes (3): Attachments, DialogFilter, AttachmentTests

### Community 49 - "AudioVolume"
Cohesion: 0.16
Nodes (4): AudioVolume, Available, Level, Muted

### Community 50 - "IEnumerable"
Cohesion: 0.12
Nodes (15): M8, Next step, ClipPrivacy, CommandSnapshots, AskAi, CalculatorProvider, CommandItem, Confirm (+7 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.11
Nodes (11): Client, IVpnEnvironment, LocalAppData, ProgramFiles, VpnDetector, VpnEntry, CanToggle, FakeEnv (+3 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "Markdown.Parse"
Cohesion: 0.14
Nodes (9): Markdown, MdSpan, MdSpanKind, Bold, BoldItalic, Code, Italic, Text (+1 more)

### Community 57 - "FakeMediaSource"
Cohesion: 0.21
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 58 - "Action"
Cohesion: 0.13
Nodes (3): RecordOptions, ToolsModule, Id

### Community 60 - "List"
Cohesion: 0.11
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.10
Nodes (14): Service, DiskEntry, LyricsResult, LyricsService, LyricsStatus, Error, Instrumental, NotFound (+6 more)

### Community 64 - "FrameworkElement"
Cohesion: 0.22
Nodes (3): TabInfo, IslandWindow, ToolsSnapshots

### Community 65 - "AiModule"
Cohesion: 0.12
Nodes (10): How to check quickly, Known blockers, Last action, Milestones, PROGRESS, AiModule, Busy, Current (+2 more)

### Community 66 - "Dictionary"
Cohesion: 0.18
Nodes (5): CurrencyParser, LiveEventsModule, Id, PillSink, Ui

### Community 67 - "Icon"
Cohesion: 0.29
Nodes (3): ArtImage, Equalizer, Playing

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.23
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.13
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 71 - "VpnIndicatorSource"
Cohesion: 0.13
Nodes (6): CapsLockSource, ILiveSource, MicCameraSource, NotificationsSource, Unavailable, VpnIndicatorSource

### Community 72 - "CommandBarView"
Cohesion: 0.16
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Theme"
Cohesion: 0.27
Nodes (3): Theme, Accent, IsDark

### Community 76 - "PlaybackMath.Choose"
Cohesion: 0.33
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 79 - "IDisposable"
Cohesion: 0.17
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 80 - "IdleHttp"
Cohesion: 0.23
Nodes (4): M10, IdleHttp, ILyricsHttp, LrclibHttp

### Community 82 - "OverlayWindow"
Cohesion: 0.11
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "ControlServices"
Cohesion: 0.23
Nodes (8): ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots, EmptyVpnEnvironment, LocalAppData, ProgramFiles

### Community 84 - "HotkeyService"
Cohesion: 0.22
Nodes (4): Isle.Modules.Hotkeys, Entry, HotkeyInfo, HotkeyService

### Community 85 - "CoreAudio.cs"
Cohesion: 0.13
Nodes (10): CoreAudioIds, DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice (+2 more)

### Community 87 - "CurrentWeather"
Cohesion: 0.22
Nodes (9): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+1 more)

### Community 88 - "FakeSink"
Cohesion: 0.22
Nodes (4): ILiveSink, Ui, FakeSink, Ui

### Community 89 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 91 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 94 - "DECISIONS"
Cohesion: 0.15
Nodes (11): DECISIONS, M0, M1, M11, M3, M4, M6, M7 (+3 more)

### Community 95 - "MdBlockKind"
Cohesion: 0.25
Nodes (8): MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote, Rule

### Community 97 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 98 - "FrameworkElement"
Cohesion: 0.22
Nodes (3): IslandWindow, MdBlock, MarkdownView

### Community 99 - "WeatherSnapshots.Register"
Cohesion: 0.39
Nodes (3): DayWeather, WeatherSnapshots, WeatherPill

### Community 101 - "AiTests"
Cohesion: 0.27
Nodes (5): AiConfig, IsReady, AnthropicProvider, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Conversation"
Cohesion: 0.24
Nodes (5): ChatStore, All, D, Data, Chats

### Community 107 - "LyricsTracker"
Cohesion: 0.24
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 108 - "AppIndex"
Cohesion: 0.12
Nodes (11): JsonStore, AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry (+3 more)

### Community 110 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 112 - "IModule"
Cohesion: 0.18
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 113 - "ClipStore"
Cohesion: 0.28
Nodes (3): ClipStore, Entries, Limit

### Community 114 - "Radios"
Cohesion: 0.22
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 115 - "Lrc.Parse"
Cohesion: 0.17
Nodes (4): LrcLine, StringsTests, TrayIconTests, LrcTests

### Community 118 - "WeatherData.cs"
Cohesion: 0.24
Nodes (3): RainAlert, RainAlerts, WeatherCodes

### Community 119 - "SteamSource"
Cohesion: 0.25
Nodes (3): SpeedMeter, BytesPerSecond, SteamSource

### Community 121 - "App.StartModules"
Cohesion: 0.13
Nodes (4): ControlModule, Id, ShelfModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.43
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 124 - "ModuleHost"
Cohesion: 0.25
Nodes (5): ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 125 - "ChatAttachment"
Cohesion: 0.14
Nodes (13): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+5 more)

### Community 127 - "LiveMiscTests"
Cohesion: 0.20
Nodes (3): ConsentStore, ObsAuth, LiveMiscTests

### Community 128 - "AnthropicProvider"
Cohesion: 0.25
Nodes (3): AiException, Detail, Key

### Community 129 - "Paths"
Cohesion: 0.25
Nodes (5): Paths, LogFile, Root, SettingsFile, Store

### Community 132 - "IslandWindow"
Cohesion: 0.25
Nodes (8): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect, Text

### Community 133 - "Conversation"
Cohesion: 0.13
Nodes (14): ChatMessage, Attachments, Error, Note, Role, Stopped, Text, Conversation (+6 more)

### Community 135 - "OpenAiProvider"
Cohesion: 0.24
Nodes (4): Modules (`src/Isle/Modules`), IChatProvider, OpenAiProvider, SseEvent

### Community 137 - "WeatherKind"
Cohesion: 0.22
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 140 - "Logger.Info"
Cohesion: 0.22
Nodes (3): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Logger, ScreenGrab

### Community 141 - "CommandModule.Create"
Cohesion: 0.09
Nodes (10): Isle.Modules.Commands, Isle.Views.Commands, CommandModule, Id, AppsProvider, Cmd, CurrencyProvider, ICommandProvider (+2 more)

### Community 144 - "ControlView.ControlView"
Cohesion: 0.24
Nodes (4): Brightness, Level, Supported, Tile

### Community 145 - "Isle.Tests"
Cohesion: 0.09
Nodes (3): Isle.Modules.AiChat, Isle.Views.AiChat, Isle.Tests

### Community 146 - "IReadOnlyList"
Cohesion: 0.33
Nodes (3): MachineVpnEnvironment, LocalAppData, ProgramFiles

### Community 148 - "NotesModule"
Cohesion: 0.27
Nodes (4): NotesModule, Id, Store, View

### Community 149 - "Isle.Core"
Cohesion: 0.17
Nodes (6): Isle.Views.Notes, Isle.Core, Isle.Modules.Notes, Autostart, IsEnabled, PillPriority

### Community 150 - "ClipEntry"
Cohesion: 0.12
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 151 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **384 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `net10.0-windows10.0.19041.0`, `Anthropic (13.8.0)` (+379 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 627 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **44 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `OpenAiProvider` to `BluetoothDevices.ListAsync`, `Conversation`, `TimersController`, `MediaSession`, `Strings.T`, `Color`, `ControlView.ControlView`, `NotesModule`, `TimerBook`, `MediaModule`, `ReminderParser`, `MediaState`, `WeatherModule`, `NotesView`, `TimersModule`, `Task`, `DateTimeOffset`, `App`, `Attachments`, `AudioVolume`, `VpnDetector.Detect`, `Markdown.Parse`, `FakeMediaSource`, `List`, `Strings.T`, `DateTimeOffset`, `AiModule`, `Theme`, `PlaybackMath.Choose`, `IdleHttp`, `OpenMeteoParser.Forecast`, `ControlServices`, `HotkeyService`, `DECISIONS`, `Regex`, `FrameworkElement`, `WeatherSnapshots.Register`, `AiTests`, `Conversation`, `WindowsTheme`, `LyricsTracker`, `Radios`, `WeatherData.cs`, `App.StartModules`, `ChatAttachment`?**
  _High betweenness centrality (0.114) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _384 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.06894664842681258 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Dictionary`, `SystemControls.cs`, `4. Features (all of them)`, `CommandModule.Create`, `Isle.Tests`, `TempDir`, `Task`, `Providers.cs`, `Screens.All`, `system_windows`, `Recorder.cs`, `Isle.Modules.Media`, `IDisposable`, `HotkeyService`, `AppIndex`, `Spring`, `IModule`, `MarkupWindow.cs`, `TimerPill.cs`, `ChatAttachment`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.08421985815602837 - nodes in this community are weakly interconnected._
- **Why does `Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)` connect `4. Features (all of them)` to `Logger.Info`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.09243697478991597 - nodes in this community are weakly interconnected._