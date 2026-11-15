# Graph Report - island  (2026-11-15)

## Corpus Check
- 166 files · ~124,112 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 3242 nodes · 9392 edges · 160 communities (106 shown, 54 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1228 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9d2246a0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ShelfStore
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
- Equalizer
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
- HookInstaller
- AttachmentTests
- system_text
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
- AgentsModule
- IEnumerable
- ToolsModule
- AiPopupView
- List
- JsonObject
- Strings.T
- DateTimeOffset
- FrameworkElement
- TimersModule
- IDisposable
- VoiceModule
- system_windows_threading
- IAudioEndpointVolume
- LiveEventsModule
- CommandBarView
- Theme
- ArraySampleProvider
- ClipEntry
- Fact
- ForegroundWatcher
- TrayModule
- AgentSession
- IdleHttp
- TimeSpan
- OverlayWindow
- VoiceModule.Released
- HotkeyService
- BluetoothDevices.ListAsync
- ObsSource
- AgentsView
- ILiveSink
- ClipboardView
- IndicatorPill
- ClipStore
- Gesture
- RegistryWatcher
- AgentBook
- AgentPipeServer
- Calculator.TryEvaluate
- PillItem
- FrameworkElement
- WeatherModule.Start
- WeatherSnapshots.Register
- AiTests
- Units
- Modules (`src/Isle/Modules`)
- NotesView.SelectedParagraphs
- VoiceTests
- Parser
- LyricsTracker
- AppIndex
- WeatherTests
- EDataFlow
- ShelfView
- DeviceState
- Memory.TrimSoon
- Logger.Error
- Lrc.Parse
- Logger.Warn
- MarkupWindow.cs
- AudioVolume
- LiveEventsModule.Create
- ToolsTests
- App.StartModules
- Isle.Core
- OpenMeteoParser
- Func
- ChatMessage
- Fuzzy.Score
- KeyboardLayoutSource
- ControlSnapshots.Tab
- MediaModule
- ChatAttachment
- FakeMediaSource
- IslandWindow
- Note
- Conversation
- DECISIONS
- AiException
- WeatherKind
- Paths.File
- IslandWindow.ShowTab
- BitmapSource
- OpenAiProvider
- MicCapture
- IdlePillView
- system_text_json_nodes
- NotesSnapshots.Register
- ShellIcons.For
- Spring
- Isle.Tests
- ObsAuth.Response
- Native
- WhisperModel
- WeatherView.cs
- KnownFolders.SHGetKnownFolderPath
- ColorText.Hex
- Secret
- ClipSnapshots.Register
- LyricsStatus
- CurrencyParser.Parse

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 102 edges
2. `Native` - 88 edges
3. `Modules (`src/Isle/Modules`)` - 87 edges
4. `Thickness` - 83 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 50 edges
7. `AiModule` - 47 edges
8. `NotesView` - 47 edges
9. `ControlView` - 46 edges
10. `IslandWindow` - 46 edges

## Surprising Connections (you probably didn't know these)
- `Last action` --references--> `AgentTests`  [INFERRED]
  PROGRESS.md → tests/Isle.Tests/AgentTests.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `Modules (`src/Isle/Modules`)` --references--> `HookProtocol`  [INFERRED]
  ARCHITECTURE.md → src/Isle.Hook/HookProtocol.cs
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs

## Import Cycles
- None detected.

## Communities (160 total, 54 thin omitted)

### Community 1 - "ShelfStore"
Cohesion: 0.14
Nodes (12): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+4 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.20
Nodes (8): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, FakeRates

### Community 4 - "IslandWindow"
Cohesion: 0.15
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.04
Nodes (37): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+29 more)

### Community 6 - "TimersController"
Cohesion: 0.13
Nodes (12): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+4 more)

### Community 8 - "ClipboardService"
Cohesion: 0.11
Nodes (4): ClipboardModule, Id, ClipboardService, ClipPrivacy

### Community 9 - "MediaSession"
Cohesion: 0.13
Nodes (5): M2, MediaSession, Current, PlaybackMath, VERIFICATION

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.09
Nodes (19): net10.0, net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0) (+11 more)

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.06
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.12
Nodes (21): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+13 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.08
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 22 - "Equalizer"
Cohesion: 0.29
Nodes (4): ArtImage, Equalizer, Playing, TrackBanner

### Community 23 - "ReminderParser"
Cohesion: 0.05
Nodes (29): M3, How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, RepeatKind (+21 more)

### Community 26 - "NotesStore"
Cohesion: 0.22
Nodes (5): NotesStore, Doc, Folders, Notes, NotesStoreTests

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.10
Nodes (6): UnitTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.11
Nodes (17): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+9 more)

### Community 30 - "WeatherModule"
Cohesion: 0.15
Nodes (5): WeatherModule, Fahrenheit, HasPlace, Id, S

### Community 32 - "TimersModule"
Cohesion: 0.16
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 33 - "MarkupWindow"
Cohesion: 0.13
Nodes (4): MarkupWindow, Stroke, PickButton, IsChecked

### Community 34 - "ControlView"
Cohesion: 0.12
Nodes (4): VpnEntry, CanToggle, ControlView, Tile

### Community 36 - "TempDir"
Cohesion: 0.20
Nodes (6): Core (`src/Isle/Core`), Isle.Modules.Tray, SettingsTests, TempDir, Path, TrayIconTests

### Community 37 - "Task"
Cohesion: 0.14
Nodes (4): Place, IWeatherSource, OpenMeteoSource, FakeSource

### Community 38 - "HookInstaller"
Cohesion: 0.15
Nodes (6): Entry, HookInstaller, InstalledExe, HookPlan, HookTarget, Detected

### Community 40 - "system_text"
Cohesion: 0.11
Nodes (3): Isle.Views.Live, Isle.Modules.LiveEvents, CapabilityUse

### Community 41 - "Icon"
Cohesion: 0.10
Nodes (12): Icon, Data, Filled, Foreground, Stroke, MarqueeText, Inner, Text (+4 more)

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.26
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.08
Nodes (12): ARCHITECTURE, Process shape, Shell, Styles / Views, Tests (`tests/Isle.Tests`), Application, App, Host (+4 more)

### Community 45 - "IslandWindow"
Cohesion: 0.19
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 47 - "Settings"
Cohesion: 0.10
Nodes (17): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+9 more)

### Community 49 - "TextAction"
Cohesion: 0.14
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 50 - "CommandModule.Create"
Cohesion: 0.10
Nodes (10): Isle.Modules.Commands, Isle.Views.Commands, AppsProvider, AskAi, Cmd, CurrencyProvider, ICommandProvider, SystemProvider (+2 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.08
Nodes (16): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+8 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.14
Nodes (6): CoreAudioIds, IKsControl, IPropertyStore, KsProperty, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "Markdown.Parse"
Cohesion: 0.09
Nodes (17): Markdown, MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote (+9 more)

### Community 55 - "IslandWindow"
Cohesion: 0.15
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 56 - "AgentsModule"
Cohesion: 0.11
Nodes (11): AgentRequest, Detail, Id, IsQuestion, Questions, Reply, Session, Tool (+3 more)

### Community 57 - "IEnumerable"
Cohesion: 0.15
Nodes (10): CommandSnapshots, CalculatorProvider, CommandItem, Confirm, IconKey, IconPath, Run, Score (+2 more)

### Community 58 - "ToolsModule"
Cohesion: 0.15
Nodes (3): RecordOptions, ToolsModule, Id

### Community 60 - "List"
Cohesion: 0.11
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "JsonObject"
Cohesion: 0.14
Nodes (7): HookProtocol, PipeName, AgentQuestion, HookMessage, Event, AgentText, Name

### Community 63 - "DateTimeOffset"
Cohesion: 0.16
Nodes (7): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, FakeHttp, LyricsServiceTests

### Community 64 - "FrameworkElement"
Cohesion: 0.21
Nodes (3): TabInfo, ToolsSnapshots, AgentsActions

### Community 65 - "TimersModule"
Cohesion: 0.24
Nodes (3): TimersModule, Controller, Id

### Community 66 - "IDisposable"
Cohesion: 0.17
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 67 - "VoiceModule"
Cohesion: 0.12
Nodes (8): Phase, Idle, Recording, Transcribing, VoiceModule, Id, VoiceSnapshots, VoicePill

### Community 68 - "system_windows_threading"
Cohesion: 0.12
Nodes (7): Isle.Modules.Clipboard, Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Hotkeys, Isle.Modules.Media, Isle.Views.Shelf, Isle.Views.Clipboard

### Community 72 - "CommandBarView"
Cohesion: 0.17
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Theme"
Cohesion: 0.27
Nodes (3): Theme, Accent, IsDark

### Community 74 - "ArraySampleProvider"
Cohesion: 0.24
Nodes (3): ArraySampleProvider, WaveFormat, AudioMath

### Community 75 - "ClipEntry"
Cohesion: 0.12
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 77 - "ForegroundWatcher"
Cohesion: 0.11
Nodes (4): M6, ForegroundWatcher, LastExternal, MOUSEINPUT

### Community 79 - "AgentSession"
Cohesion: 0.13
Nodes (16): AgentSession, Agent, Cwd, Key, LastAction, Pending, Project, Reported (+8 more)

### Community 80 - "IdleHttp"
Cohesion: 0.13
Nodes (5): M10, IdleHttp, CommandModule, Id, LrclibHttp

### Community 81 - "TimeSpan"
Cohesion: 0.14
Nodes (10): DurationParser, TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning (+2 more)

### Community 82 - "OverlayWindow"
Cohesion: 0.11
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "VoiceModule.Released"
Cohesion: 0.16
Nodes (6): VoiceSelfTest, ITranscriber, LanguagePick, Transcript, WhisperEngine, IsLoaded

### Community 84 - "HotkeyService"
Cohesion: 0.24
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "BluetoothDevices.ListAsync"
Cohesion: 0.22
Nodes (4): BluetoothDevices, BtDevice, IMMDevice, IMMDeviceCollection

### Community 88 - "ILiveSink"
Cohesion: 0.14
Nodes (7): CurrencyParser, PillSink, Ui, ILiveSink, Ui, FakeSink, Ui

### Community 91 - "ClipStore"
Cohesion: 0.18
Nodes (4): ClipStore, Entries, Limit, ClipStoreTests

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "AgentBook"
Cohesion: 0.21
Nodes (6): AgentBook, AllPending, Count, Sessions, Urgent, AgentSnapshots

### Community 96 - "Calculator.TryEvaluate"
Cohesion: 0.20
Nodes (3): Calculator, Result, CalculatorTests

### Community 97 - "PillItem"
Cohesion: 0.11
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 98 - "FrameworkElement"
Cohesion: 0.25
Nodes (3): IslandWindow, MdBlock, MarkdownView

### Community 99 - "WeatherModule.Start"
Cohesion: 0.29
Nodes (3): WeatherService, LastFailed, WeatherActions

### Community 101 - "AiTests"
Cohesion: 0.41
Nodes (5): AiConfig, IsReady, AnthropicProvider, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.10
Nodes (6): AiModule, Busy, Current, Id, S, AiSnapshots

### Community 105 - "VoiceTests"
Cohesion: 0.23
Nodes (5): ChecksumException, ModelDownloader, BytesHandler, SyncProgress, VoiceTests

### Community 107 - "LyricsTracker"
Cohesion: 0.19
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.12
Nodes (12): JsonStore, AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry (+4 more)

### Community 110 - "EDataFlow"
Cohesion: 0.13
Nodes (11): DeviceCallback, EDataFlow, All, Capture, Render, ERole, Communications, Console (+3 more)

### Community 112 - "DeviceState"
Cohesion: 0.25
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 114 - "Logger.Error"
Cohesion: 0.09
Nodes (13): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Autostart, IsEnabled, Logger, Brightness, Level, Supported, Radios (+5 more)

### Community 115 - "Lrc.Parse"
Cohesion: 0.14
Nodes (4): Lrc, LrcLine, TranscriptText, LrcTests

### Community 116 - "Logger.Warn"
Cohesion: 0.19
Nodes (4): ClipboardOps, IsQuiet, InputUnion, KEYBDINPUT

### Community 118 - "AudioVolume"
Cohesion: 0.09
Nodes (13): M5, AudioVolume, Available, Level, Muted, VolumeCallback, ControlServices, Audio (+5 more)

### Community 119 - "LiveEventsModule.Create"
Cohesion: 0.10
Nodes (8): SpeedMeter, BytesPerSecond, CapsLockSource, ILiveSource, NotificationsSource, Unavailable, SteamSource, VpnIndicatorSource

### Community 121 - "App.StartModules"
Cohesion: 0.08
Nodes (10): ControlModule, Id, LyricsModule, Id, NotesModule, Id, Store, ShelfModule (+2 more)

### Community 122 - "Isle.Core"
Cohesion: 0.15
Nodes (7): Isle.Views.Timers, Isle.Views.Notes, Isle.Core, Isle.Modules.Notes, Isle.Modules.Reminders, Isle.Modules.Timers, PillPriority

### Community 124 - "Func"
Cohesion: 0.12
Nodes (12): Entry, IModule, Id, ModuleHost, Ids, ModuleState, Failed, Running (+4 more)

### Community 125 - "ChatMessage"
Cohesion: 0.17
Nodes (7): ChatMessage, Attachments, Error, Note, Role, Stopped, Text

### Community 129 - "MediaModule"
Cohesion: 0.16
Nodes (8): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPill, PillWidth

### Community 130 - "ChatAttachment"
Cohesion: 0.14
Nodes (13): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+5 more)

### Community 131 - "FakeMediaSource"
Cohesion: 0.20
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 132 - "IslandWindow"
Cohesion: 0.25
Nodes (8): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect, Text

### Community 133 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 134 - "Conversation"
Cohesion: 0.18
Nodes (11): Conversation, Id, Messages, SystemPrompt, Title, Updated, ChatStore, All (+3 more)

### Community 135 - "DECISIONS"
Cohesion: 0.11
Nodes (12): DECISIONS, M0, M1, M12, M13, M4, M7, M9 (+4 more)

### Community 136 - "AiException"
Cohesion: 0.27
Nodes (3): AiException, Detail, Key

### Community 137 - "WeatherKind"
Cohesion: 0.22
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 138 - "Paths.File"
Cohesion: 0.22
Nodes (6): Paths, LogFile, Root, SettingsFile, Store, LocalPath

### Community 142 - "OpenAiProvider"
Cohesion: 0.27
Nodes (4): Modules (`src/Isle/Modules`), IChatProvider, OpenAiProvider, SseEvent

### Community 144 - "IdlePillView"
Cohesion: 0.18
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 145 - "system_text_json_nodes"
Cohesion: 0.17
Nodes (3): Isle.Agents, Isle.Views.Agents, Isle.Modules.Agents

### Community 149 - "Isle.Tests"
Cohesion: 0.11
Nodes (3): Isle.Modules.AiChat, Isle.Views.AiChat, Isle.Tests

### Community 151 - "Native"
Cohesion: 0.07
Nodes (12): EnumWindowsProc, INPUT, LowLevelKeyboardProc, MonitorEnumProc, MONITORINFOEX, Native, NOTIFYICONDATA, POINT (+4 more)

### Community 152 - "WhisperModel"
Cohesion: 0.22
Nodes (7): WhisperModel, IsDownloaded, SizeMb, Url, WhisperModels, All, Model

### Community 153 - "WeatherView.cs"
Cohesion: 0.20
Nodes (5): Isle.Views.Voice, Isle.Modules.Weather, Isle.Views.Weather, Isle.Modules.Voice, WeatherSnapshots

### Community 160 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

## Knowledge Gaps
- **438 isolated node(s):** `PipeName`, `net10.0`, `Microsoft.NET.Sdk`, `Host`, `Island` (+433 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 688 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **54 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `OpenAiProvider` to `MediaModule`, `FakeMediaSource`, `Conversation`, `TimersController`, `DECISIONS`, `MediaSession`, `Strings.T`, `Color`, `MicCapture`, `WeatherReport`, `TimerBook`, `ReminderParser`, `WhisperModel`, `NotesStore`, `MediaState`, `WeatherModule`, `TimersModule`, `Task`, `HookInstaller`, `App`, `Attachments`, `TextAction`, `CommandModule.Create`, `VpnDetector.Detect`, `Markdown.Parse`, `AgentsModule`, `AiPopupView`, `List`, `JsonObject`, `Strings.T`, `DateTimeOffset`, `TimersModule`, `VoiceModule`, `Theme`, `ArraySampleProvider`, `AgentSession`, `TimeSpan`, `VoiceModule.Released`, `HotkeyService`, `BluetoothDevices.ListAsync`, `AgentBook`, `AgentPipeServer`, `FrameworkElement`, `WeatherModule.Start`, `WeatherSnapshots.Register`, `AiTests`, `Modules (`src/Isle/Modules`)`, `VoiceTests`, `LyricsTracker`, `WeatherTests`, `Logger.Error`, `Lrc.Parse`, `Logger.Warn`, `AudioVolume`, `App.StartModules`, `OpenMeteoParser`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **What connects `PipeName`, `net10.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _438 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ShelfStore` be split into smaller, more focused modules?**
  _Cohesion score 0.1354679802955665 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `ControlSnapshots.Tab`, `ShelfStore`, `SystemControls.cs`, `ChatAttachment`, `RatesSnapshot`, `4. Features (all of them)`, `Note`, `system_text_json_nodes`, `Spring`, `Isle.Tests`, `WeatherView.cs`, `TempDir`, `Task`, `system_text`, `Screens.All`, `system_windows`, `CommandModule.Create`, `IDisposable`, `system_windows_threading`, `AppIndex`, `MarkupWindow.cs`, `Func`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.06612685560053981 - nodes in this community are weakly interconnected._
- **Why does `MarkupWindow` connect `MarkupWindow` to `Thickness`, `IslandWindow`, `Strings.T`, `Color`, `Window`, `OverlayWindow`, `MarkupWindow.cs`, `ILiveSink`, `List`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.14736842105263157 - nodes in this community are weakly interconnected._