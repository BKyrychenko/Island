# Graph Report - island  (2026-11-15)

## Corpus Check
- 146 files · ~108,019 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 2911 nodes · 8226 edges · 136 communities (87 shown, 49 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1096 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b7822e7b`
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
- NotesStore
- Node
- Theory
- MediaState
- WeatherModule
- NotesView
- TimersModule
- MarkupWindow
- ControlView
- Thickness
- TempDir
- Task
- LyricsModule
- Fact
- system_globalization
- Icon
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows
- Settings
- Attachments
- TextAction
- CommandModule.Create
- VpnDetector.Detect
- CoreAudio.cs
- DownloadsSource
- Markdown.Parse
- IslandWindow
- TimerItem
- ToolsModule
- AiPopupView
- List
- Grid
- Strings.T
- DateTimeOffset
- FrameworkElement
- TimersModule
- Dictionary
- Isle.Modules.Media
- IAudioEndpointVolume
- VpnIndicatorSource
- CommandBarView
- Theme
- ClipStore
- PlaybackMath.Choose
- ForegroundWatcher
- TrayModule
- CoreTests.cs
- IdleHttp
- TimeSpan
- OverlayWindow
- Isle.Tests
- HotkeyService
- IMMDevice
- ObsSource
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- FakeSink
- IMMNotificationClient
- IndicatorPill
- RepeatKind
- Gesture
- RegistryWatcher
- TrayIcon.Create
- CommandModule
- Regex
- PillItem
- FrameworkElement
- Strings
- Strings.cs
- Conversation
- Units
- Modules (`src/Isle/Modules`)
- NotesView.SelectedParagraphs
- WindowsTheme
- Parser
- LyricsTracker
- AppIndex
- IEnumerable
- EDataFlow
- ToolsModule.ToggleRecording
- DeviceState
- VpnIndicatorSource
- ControlServices
- Lrc.Parse
- Logger.Warn
- MarkupWindow.cs
- PickButton
- SteamSource
- ToolsTests
- App.StartModules
- TimerPill.cs
- Autostart
- ModuleHost
- ColorText.Hex
- Fuzzy.Score
- LiveMiscTests
- LowLevelKeyboardProc
- IslandWindow
- BitmapSource
- Providers.cs
- Isle.Tests
- system_text
- RECT
- WinEventDelegate

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 92 edges
2. `Native` - 88 edges
3. `Thickness` - 72 edges
4. `Modules (`src/Isle/Modules`)` - 69 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 50 edges
7. `AiModule` - 47 edges
8. `NotesView` - 47 edges
9. `ControlView` - 46 edges
10. `IslandWindow` - 46 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Modules (`src/Isle/Modules`)` --references--> `TextActions`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/AiChat/TextActions.cs
- `Modules (`src/Isle/Modules`)` --references--> `AiPopupView`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Views/AiChat/AiPopupView.cs
- `M11` --references--> `AiPopupView`  [INFERRED]
  DECISIONS.md → src/Isle/Views/AiChat/AiPopupView.cs
- `Last action` --references--> `AiPopupView`  [INFERRED]
  PROGRESS.md → src/Isle/Views/AiChat/AiPopupView.cs

## Import Cycles
- None detected.

## Communities (136 total, 49 thin omitted)

### Community 0 - "Native"
Cohesion: 0.08
Nodes (8): KnownFolders, Downloads, MONITORINFOEX, MOUSEINPUT, Native, NOTIFYICONDATA, POINT, SHFILEINFO

### Community 1 - "Dictionary"
Cohesion: 0.07
Nodes (18): Paths, LogFile, Root, SettingsFile, Store, ShelfFile, Items, Version (+10 more)

### Community 2 - "SystemControls.cs"
Cohesion: 0.09
Nodes (6): Isle.Views.Live, Isle.Core, Isle.Modules.ControlCenter, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents, Isle.Views.Control

### Community 3 - "RatesSnapshot"
Cohesion: 0.12
Nodes (11): JsonStore, CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates (+3 more)

### Community 4 - "IslandWindow"
Cohesion: 0.06
Nodes (13): Spring, ITabView, IslandWindow, CurrentTab, IdleView, TabList, IsExpanded, IdlePillView (+5 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.13
Nodes (12): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+4 more)

### Community 8 - "ClipboardService"
Cohesion: 0.13
Nodes (3): ClipboardModule, Id, ClipboardService

### Community 9 - "MediaSession"
Cohesion: 0.17
Nodes (3): Logger, MediaSession, Current

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.11
Nodes (14): net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3) (+6 more)

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.10
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.06
Nodes (34): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+26 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.10
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "MediaModule"
Cohesion: 0.11
Nodes (12): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, ArtImage, Equalizer (+4 more)

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 26 - "NotesStore"
Cohesion: 0.10
Nodes (18): NotesSnapshots, Note, Created, Folder, Id, Pinned, Plain, Preview (+10 more)

### Community 27 - "Node"
Cohesion: 0.13
Nodes (8): CapabilityUse, Node, ObsAuth, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.11
Nodes (6): CalculatorTests, UnitTests, DownloadDetectorTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.08
Nodes (21): FakeMediaSource, Current, IMediaSource, Current, MediaSnapshots, MediaState, Accent, Album (+13 more)

### Community 30 - "WeatherModule"
Cohesion: 0.12
Nodes (7): RainAlert, WeatherModule, Fahrenheit, HasPlace, Id, S, WeatherPill

### Community 32 - "TimersModule"
Cohesion: 0.19
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 34 - "ControlView"
Cohesion: 0.11
Nodes (6): BtDevice, ControlSnapshots, VpnEntry, CanToggle, ControlView, Tile

### Community 36 - "TempDir"
Cohesion: 0.21
Nodes (5): Core (`src/Isle/Core`), SettingsTests, TempDir, Path, NotesStoreTests

### Community 37 - "Task"
Cohesion: 0.12
Nodes (7): Place, IWeatherSource, OpenMeteoSource, WeatherService, LastFailed, WeatherActions, FakeSource

### Community 38 - "LyricsModule"
Cohesion: 0.33
Nodes (3): LyricsModule, Id, Service

### Community 39 - "Fact"
Cohesion: 0.16
Nodes (4): AttachmentTests, StringsTests, FakeHttp, LyricsServiceTests

### Community 41 - "Icon"
Cohesion: 0.08
Nodes (18): DECISIONS, M1, M10, M3, M4, M6, M7, Performance measurements (+10 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.20
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.08
Nodes (11): ARCHITECTURE, Process shape, Shell, Styles / Views, Tests (`tests/Isle.Tests`), Application, App, Host (+3 more)

### Community 45 - "IslandWindow"
Cohesion: 0.11
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 46 - "system_windows"
Cohesion: 0.11
Nodes (3): Isle, Isle.Views, PillPriority

### Community 47 - "Settings"
Cohesion: 0.11
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 49 - "TextAction"
Cohesion: 0.14
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 50 - "CommandModule.Create"
Cohesion: 0.16
Nodes (5): AppsProvider, CalculatorProvider, CurrencyProvider, ICommandProvider, TimerProvider

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.09
Nodes (15): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+7 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.15
Nodes (6): CoreAudioIds, IKsControl, IPropertyStore, KsProperty, PropertyKey, PropVariant

### Community 54 - "Markdown.Parse"
Cohesion: 0.09
Nodes (17): Markdown, MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote (+9 more)

### Community 55 - "IslandWindow"
Cohesion: 0.13
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 57 - "TimerItem"
Cohesion: 0.14
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 58 - "ToolsModule"
Cohesion: 0.16
Nodes (3): ToolsModule, Id, View

### Community 60 - "List"
Cohesion: 0.12
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.14
Nodes (10): DiskEntry, ILyricsHttp, LyricsResult, LyricsService, LyricsStatus, Error, Instrumental, NotFound (+2 more)

### Community 64 - "FrameworkElement"
Cohesion: 0.18
Nodes (4): TabInfo, IslandWindow, IslandWindow, ToolsSnapshots

### Community 65 - "TimersModule"
Cohesion: 0.25
Nodes (3): TimersModule, Controller, Id

### Community 66 - "Dictionary"
Cohesion: 0.11
Nodes (8): EventBus, Unsubscriber, CurrencyParser, LiveEventsModule, Id, PillSink, Ui, ILiveSource

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.24
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.12
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 71 - "VpnIndicatorSource"
Cohesion: 0.12
Nodes (6): CapsLockSource, ILiveSink, Ui, KeyboardLayoutSource, NotificationsSource, Unavailable

### Community 72 - "CommandBarView"
Cohesion: 0.12
Nodes (14): CommandItem, Confirm, IconKey, IconPath, Run, Score, Subtitle, Title (+6 more)

### Community 73 - "Theme"
Cohesion: 0.27
Nodes (3): Theme, Accent, IsDark

### Community 75 - "ClipStore"
Cohesion: 0.06
Nodes (24): Secret, ClipSnapshots, ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight (+16 more)

### Community 76 - "PlaybackMath.Choose"
Cohesion: 0.33
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 79 - "CoreTests.cs"
Cohesion: 0.28
Nodes (4): Isle.Modules.Tray, EventBusTests, Ping, TrayIconTests

### Community 82 - "OverlayWindow"
Cohesion: 0.11
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "Isle.Tests"
Cohesion: 0.25
Nodes (5): Isle.Modules.Clipboard, Isle.Views.Shelf, Isle.Modules.Shelf, Isle.Tests, Isle.Views.Clipboard

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "IMMDevice"
Cohesion: 0.18
Nodes (4): BluetoothDevices, IMMDevice, IMMDeviceCollection, IMMDeviceEnumerator

### Community 87 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 89 - "IMMNotificationClient"
Cohesion: 0.10
Nodes (11): M5, AudioVolume, Available, Level, Muted, DeviceCallback, VolumeCallback, Audio (+3 more)

### Community 91 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 93 - "RegistryWatcher"
Cohesion: 0.14
Nodes (3): Memory, MicCameraSource, RegistryWatcher

### Community 94 - "TrayIcon.Create"
Cohesion: 0.22
Nodes (4): M0, M11, TrayIcon, ICONINFO

### Community 96 - "Regex"
Cohesion: 0.15
Nodes (3): Calculator, Result, CurrencyQuery

### Community 97 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 99 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 101 - "Conversation"
Cohesion: 0.08
Nodes (27): AiConfig, IsReady, AiException, Detail, Key, AiProviderKind, Anthropic, OpenAi (+19 more)

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.06
Nodes (22): Modules (`src/Isle/Modules`), AttachmentKind, Image, Pdf, Text, ChatAttachment, Data, Kind (+14 more)

### Community 107 - "LyricsTracker"
Cohesion: 0.18
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.19
Nodes (10): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+2 more)

### Community 110 - "EDataFlow"
Cohesion: 0.21
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 112 - "DeviceState"
Cohesion: 0.29
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 114 - "ControlServices"
Cohesion: 0.09
Nodes (13): ControlModule, Id, ControlServices, Brightness, Radios, VpnEnvironment, Brightness, Level (+5 more)

### Community 115 - "Lrc.Parse"
Cohesion: 0.24
Nodes (3): Lrc, LrcLine, LrcTests

### Community 116 - "Logger.Warn"
Cohesion: 0.11
Nodes (10): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, ClipboardOps, IsQuiet (+2 more)

### Community 119 - "SteamSource"
Cohesion: 0.22
Nodes (4): SpeedMeter, BytesPerSecond, Item, SteamSource

### Community 121 - "App.StartModules"
Cohesion: 0.13
Nodes (5): NotesModule, Id, Store, ShelfModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.42
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 124 - "ModuleHost"
Cohesion: 0.13
Nodes (12): Entry, IModule, Id, ModuleHost, Ids, ModuleState, Failed, Running (+4 more)

### Community 132 - "IslandWindow"
Cohesion: 0.25
Nodes (8): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect, Text

### Community 141 - "Providers.cs"
Cohesion: 0.11
Nodes (8): Isle.Modules.Commands, Isle.Views.Commands, M8, CommandSnapshots, AskAi, Cmd, SystemProvider, WebProvider

### Community 151 - "RECT"
Cohesion: 0.14
Nodes (6): EnumWindowsProc, INPUT, MonitorEnumProc, RECT, Height, Width

## Knowledge Gaps
- **393 isolated node(s):** `IsQuiet`, `Id`, `Current`, `Busy`, `S` (+388 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 638 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **49 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `TimersController`, `MediaSession`, `Color`, `Providers.cs`, `WeatherReport`, `TimerBook`, `MediaModule`, `ReminderParser`, `NotesStore`, `MediaState`, `WeatherModule`, `TimersModule`, `Task`, `Icon`, `App`, `Attachments`, `TextAction`, `VpnDetector.Detect`, `Markdown.Parse`, `ToolsModule`, `AiPopupView`, `List`, `Strings.T`, `DateTimeOffset`, `TimersModule`, `Theme`, `PlaybackMath.Choose`, `TimeSpan`, `HotkeyService`, `IMMDevice`, `IMMNotificationClient`, `TrayIcon.Create`, `FrameworkElement`, `Conversation`, `WindowsTheme`, `LyricsTracker`, `ControlServices`, `Lrc.Parse`, `Logger.Warn`, `App.StartModules`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **What connects `IsQuiet`, `Id`, `Current` to the rest of the system?**
  _393 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.0837245696400626 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `SystemControls.cs` to `Dictionary`, `RatesSnapshot`, `IslandWindow`, `Providers.cs`, `Isle.Tests`, `system_text`, `NotesStore`, `system_globalization`, `Screens.All`, `system_windows`, `Recorder.cs`, `DateTimeOffset`, `Dictionary`, `Isle.Modules.Media`, `CoreTests.cs`, `IdleHttp`, `Isle.Tests`, `Strings`, `Strings.cs`, `Conversation`, `AppIndex`, `MarkupWindow.cs`, `TimerPill.cs`, `ModuleHost`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.06641604010025062 - nodes in this community are weakly interconnected._
- **Why does `AiPopupView` connect `AiPopupView` to `Native`, `Dictionary`, `ControlView`, `FrameworkElement`, `Modules (`src/Isle/Modules`)`, `TimersView`, `ClipStore`, `Strings.T`, `Isle.Tests`, `TextAction`, `Logger.Warn`, `RegistryWatcher`, `TrayIcon.Create`?**
  _High betweenness centrality (0.029) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.09175377468060394 - nodes in this community are weakly interconnected._