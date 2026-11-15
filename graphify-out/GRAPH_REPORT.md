# Graph Report - island  (2026-11-15)

## Corpus Check
- 173 files · ~128,664 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 3367 nodes · 9730 edges · 166 communities (114 shown, 52 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1271 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8b77c63a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- RepeatRule
- ShelfStore
- Recorder.cs
- RatesSnapshot
- IslandWindow
- 4. Features (all of them)
- SubmitResult
- TimersView
- CreatureView
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
- Logger.Error
- TempDir
- Task
- HookInstaller
- CreatureDesign
- Providers.cs
- Icon
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows
- Settings
- Attachments
- TextAction
- CommandItem
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
- CreatureBrain
- IModule
- ClipEntry
- Fact
- ForegroundWatcher
- PillItem
- FrameworkElement
- IdleHttp
- TimerItem
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
- Mood
- Lrc.Parse
- Calculator.TryEvaluate
- PillQueue
- FrameworkElement
- MediaModule.Stop
- AiTests
- Units
- Modules (`src/Isle/Modules`)
- NotesView.SelectedParagraphs
- VoiceTests
- Parser
- LyricsTracker
- AppIndex
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- AudioVolume
- CharactersModule
- DeviceState
- ClipboardService.cs
- IDisposable
- Lrc.Parse
- Logger.Warn
- MarkupWindow.cs
- CoreAudio.cs
- Sources.cs
- ToolsTests
- App.StartModules
- TimerPill.cs
- Strings
- Func
- ChatMessage
- Fuzzy.Score
- Strings.cs
- ClipStore
- MediaModule
- ChatAttachment
- TimeSpan
- IslandWindow
- Note
- Conversation
- DECISIONS
- AiException
- WeatherKind
- Paths.File
- WebProvider.QueryAsync
- BitmapSource
- PlaybackMath.Choose
- OpenAiProvider
- MicCapture
- IdlePillView
- system_text_json_nodes
- NotificationsSource
- ARCHITECTURE
- Spring
- Isle.Tests
- ObsAuth.Response
- Native
- VpnIndicatorSource
- Isle.Core
- KnownFolders.SHGetKnownFolderPath
- MdBlockKind
- RepeatKind
- JsonStore.Save
- Secret
- ClipSnapshots.Register
- LyricsStatus
- CurrencyParser.Parse
- SpeedMeter
- Logger.Warn
- LyricsModule
- HotkeyService.List

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 105 edges
2. `Modules (`src/Isle/Modules`)` - 91 edges
3. `Native` - 90 edges
4. `Thickness` - 89 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 50 edges
7. `AiModule` - 47 edges
8. `NotesView` - 47 edges
9. `ControlView` - 46 edges
10. `IslandWindow` - 46 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `Modules (`src/Isle/Modules`)` --references--> `CharactersModule`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Characters/CharactersModule.cs
- `Modules (`src/Isle/Modules`)` --references--> `CreatureBrain`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Characters/CreatureBrain.cs
- `M14` --references--> `CreatureBrain`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/Characters/CreatureBrain.cs

## Import Cycles
- None detected.

## Communities (166 total, 52 thin omitted)

### Community 0 - "RepeatRule"
Cohesion: 0.13
Nodes (8): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParserTests, RepeatRuleTests

### Community 1 - "ShelfStore"
Cohesion: 0.09
Nodes (13): ShelfFile, Items, Version, ShelfItem, Added, Id, Path, ShelfStore (+5 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.17
Nodes (8): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, FakeRates

### Community 4 - "IslandWindow"
Cohesion: 0.09
Nodes (6): Memory, IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "SubmitResult"
Cohesion: 0.40
Nodes (5): SubmitResult, Full, Invalid, Reminder, Timer

### Community 8 - "CreatureView"
Cohesion: 0.19
Nodes (3): CreatureView, Design, Faces

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.09
Nodes (19): net10.0, net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0) (+11 more)

### Community 12 - "Color"
Cohesion: 0.14
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.07
Nodes (4): Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.07
Nodes (25): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+17 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.07
Nodes (23): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+15 more)

### Community 22 - "Equalizer"
Cohesion: 0.24
Nodes (5): ArtImage, Equalizer, Playing, MusicPill, PillWidth

### Community 23 - "ReminderParser"
Cohesion: 0.14
Nodes (5): ReminderParser, Rule, State, HasDate, Repeats

### Community 26 - "NotesStore"
Cohesion: 0.22
Nodes (5): NotesSnapshots, NotesStore, Doc, Folders, Notes

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.10
Nodes (6): CalculatorTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.08
Nodes (20): FakeMediaSource, Current, IMediaSource, Current, MediaState, Accent, Album, Art (+12 more)

### Community 30 - "WeatherModule"
Cohesion: 0.09
Nodes (14): Place, WeatherModule, Fahrenheit, HasPlace, Id, S, IWeatherSource, OpenMeteoSource (+6 more)

### Community 32 - "TimersModule"
Cohesion: 0.19
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 34 - "ControlView"
Cohesion: 0.11
Nodes (6): BtDevice, ControlSnapshots, VpnEntry, CanToggle, ControlView, Tile

### Community 35 - "Logger.Error"
Cohesion: 0.09
Nodes (12): Brightness, Level, Supported, WindowsTheme, IsDark, Icon, Data, Filled (+4 more)

### Community 36 - "TempDir"
Cohesion: 0.23
Nodes (5): Core (`src/Isle/Core`), SettingsTests, TempDir, Path, NotesStoreTests

### Community 38 - "HookInstaller"
Cohesion: 0.15
Nodes (6): Entry, HookInstaller, InstalledExe, HookPlan, HookTarget, Detected

### Community 39 - "CreatureDesign"
Cohesion: 0.12
Nodes (16): CreatureDesign, Body, BodyBrush, CheekBrush, CheekDx, CheekY, Extra, ExtraBrush (+8 more)

### Community 41 - "Icon"
Cohesion: 0.13
Nodes (8): MarqueeText, Inner, Text, TabBar, AvailableWidth, Hidden, Mascot, SelectedId

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 44 - "App"
Cohesion: 0.13
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.10
Nodes (5): ITabView, IslandWindow, IsHiddenByUser, PasteTarget, IslandWindow

### Community 46 - "system_windows"
Cohesion: 0.08
Nodes (9): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Characters, Isle, Isle.Views, Isle.Modules.Media, Isle.Views.Characters, PillPriority (+1 more)

### Community 47 - "Settings"
Cohesion: 0.10
Nodes (18): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+10 more)

### Community 49 - "TextAction"
Cohesion: 0.14
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 50 - "CommandItem"
Cohesion: 0.09
Nodes (16): CommandSnapshots, AppsProvider, CalculatorProvider, Cmd, CommandItem, Confirm, IconKey, IconPath (+8 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.09
Nodes (15): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+7 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.22
Nodes (3): DownloadDetector, DownloadsSource, Item

### Community 54 - "Markdown.Parse"
Cohesion: 0.14
Nodes (9): Markdown, MdSpan, MdSpanKind, Bold, BoldItalic, Code, Italic, Text (+1 more)

### Community 55 - "IslandWindow"
Cohesion: 0.10
Nodes (8): Shell, IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Screen, Watcher

### Community 56 - "AgentsModule"
Cohesion: 0.07
Nodes (22): AgentRequest, Detail, Id, IsQuestion, Questions, Reply, Session, Tool (+14 more)

### Community 58 - "ToolsModule"
Cohesion: 0.10
Nodes (5): OcrReader, RecordOptions, ToolsModule, Id, View

### Community 60 - "List"
Cohesion: 0.12
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "JsonObject"
Cohesion: 0.10
Nodes (12): HookProtocol, PipeName, AgentBook, AllPending, Count, Sessions, Urgent, AgentQuestion (+4 more)

### Community 63 - "DateTimeOffset"
Cohesion: 0.14
Nodes (8): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, MediaSnapshots, FakeHttp, LyricsServiceTests

### Community 64 - "FrameworkElement"
Cohesion: 0.23
Nodes (3): TabInfo, ToolsSnapshots, AgentsActions

### Community 65 - "TimersModule"
Cohesion: 0.26
Nodes (4): TimersModule, Controller, Id, ReminderBanner

### Community 66 - "IDisposable"
Cohesion: 0.20
Nodes (3): EventBus, EventBusTests, Ping

### Community 67 - "VoiceModule"
Cohesion: 0.10
Nodes (13): WhisperModel, IsDownloaded, SizeMb, Url, WhisperModels, All, Phase, Idle (+5 more)

### Community 68 - "system_windows_threading"
Cohesion: 0.07
Nodes (6): Isle.Views.Live, Isle.Modules.ControlCenter, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents, Isle.Views.Control, LiveSnapshots

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.10
Nodes (5): M9, CoreAudioIds, IAudioEndpointVolume, IDirect3DDxgiInterfaceAccess, IGraphicsCaptureItemInterop

### Community 71 - "LiveEventsModule"
Cohesion: 0.15
Nodes (5): CurrencyParser, LiveEventsModule, Id, PillSink, Ui

### Community 72 - "CommandBarView"
Cohesion: 0.14
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "CreatureBrain"
Cohesion: 0.26
Nodes (5): CreatureBrain, MusicPlaying, SleepAfter, CharacterTests, FakeClock

### Community 74 - "IModule"
Cohesion: 0.18
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 75 - "ClipEntry"
Cohesion: 0.12
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 76 - "Fact"
Cohesion: 0.14
Nodes (3): AgentTests, AttachmentTests, StringsTests

### Community 78 - "PillItem"
Cohesion: 0.15
Nodes (13): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+5 more)

### Community 79 - "FrameworkElement"
Cohesion: 0.18
Nodes (8): AgentState, Done, Idle, Waiting, Working, AgentPill, PickButton, IsChecked

### Community 80 - "IdleHttp"
Cohesion: 0.13
Nodes (5): M10, IdleHttp, CommandModule, Id, LrclibHttp

### Community 81 - "TimerItem"
Cohesion: 0.18
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 82 - "OverlayWindow"
Cohesion: 0.12
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "VoiceModule.Released"
Cohesion: 0.11
Nodes (9): M0, TrayIcon, AudioMath, VoiceSelfTest, ITranscriber, LanguagePick, Transcript, WhisperEngine (+1 more)

### Community 85 - "BluetoothDevices.ListAsync"
Cohesion: 0.23
Nodes (3): BluetoothDevices, IMMDevice, IMMDeviceCollection

### Community 88 - "ILiveSink"
Cohesion: 0.22
Nodes (4): ILiveSink, Ui, FakeSink, Ui

### Community 90 - "IndicatorPill"
Cohesion: 0.15
Nodes (4): ConsentStore, VoiceSnapshots, IndicatorPill, VoicePill

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "Mood"
Cohesion: 0.24
Nodes (6): CharacterSnapshots, Mood, Awake, Dancing, Happy, Sleeping

### Community 97 - "PillQueue"
Cohesion: 0.27
Nodes (5): PillQueue, Count, Primary, Secondary, PillQueueTests

### Community 99 - "MediaModule.Stop"
Cohesion: 0.18
Nodes (7): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, MusicPlaying

### Community 101 - "AiTests"
Cohesion: 0.37
Nodes (5): AiConfig, IsReady, AnthropicProvider, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.16
Nodes (7): Modules (`src/Isle/Modules`), AiModule, Busy, Current, Id, S, AiSnapshots

### Community 105 - "VoiceTests"
Cohesion: 0.22
Nodes (5): ChecksumException, ModelDownloader, BytesHandler, SyncProgress, VoiceTests

### Community 107 - "LyricsTracker"
Cohesion: 0.19
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 108 - "AppIndex"
Cohesion: 0.19
Nodes (10): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+2 more)

### Community 109 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 110 - "AudioVolume"
Cohesion: 0.10
Nodes (13): AudioVolume, Available, Level, Muted, EDataFlow, All, Capture, Render (+5 more)

### Community 111 - "CharactersModule"
Cohesion: 0.13
Nodes (8): CharactersModule, Brain, Id, InPill, S, Selected, CharacterGallery, GalleryActions

### Community 112 - "DeviceState"
Cohesion: 0.14
Nodes (10): DeviceCallback, VolumeCallback, Audio, DeviceState, Active, All, Disabled, NotPresent (+2 more)

### Community 113 - "ClipboardService.cs"
Cohesion: 0.24
Nodes (4): Isle.Modules.Clipboard, Isle.Views.Shelf, Isle.Modules.Shelf, Isle.Views.Clipboard

### Community 114 - "IDisposable"
Cohesion: 0.11
Nodes (11): Unsubscriber, ControlModule, Id, ControlServices, Brightness, Radios, VpnEnvironment, Radios (+3 more)

### Community 116 - "Logger.Warn"
Cohesion: 0.10
Nodes (5): ClipboardOps, IsQuiet, ClipboardService, InputUnion, KEYBDINPUT

### Community 118 - "CoreAudio.cs"
Cohesion: 0.20
Nodes (5): M5, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IKsControl, KsProperty

### Community 119 - "Sources.cs"
Cohesion: 0.14
Nodes (5): CapsLockSource, ILiveSource, KeyboardLayoutSource, MicCameraSource, SteamSource

### Community 120 - "ToolsTests"
Cohesion: 0.25
Nodes (3): QrReader, ColorText, ToolsTests

### Community 121 - "App.StartModules"
Cohesion: 0.09
Nodes (7): ClipboardModule, Id, NotesModule, Id, Store, ShelfModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.22
Nodes (5): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, Autostart, IsEnabled

### Community 123 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 124 - "Func"
Cohesion: 0.24
Nodes (5): ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 125 - "ChatMessage"
Cohesion: 0.17
Nodes (7): ChatMessage, Attachments, Error, Note, Role, Stopped, Text

### Community 128 - "ClipStore"
Cohesion: 0.29
Nodes (3): ClipStore, Entries, Limit

### Community 129 - "MediaModule"
Cohesion: 0.12
Nodes (8): MediaModule, Id, IsPlaying, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, TrackBanner

### Community 130 - "ChatAttachment"
Cohesion: 0.14
Nodes (14): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+6 more)

### Community 132 - "IslandWindow"
Cohesion: 0.18
Nodes (9): IslandWindow, Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect (+1 more)

### Community 133 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 134 - "Conversation"
Cohesion: 0.16
Nodes (11): Conversation, Id, Messages, SystemPrompt, Title, Updated, ChatStore, All (+3 more)

### Community 135 - "DECISIONS"
Cohesion: 0.17
Nodes (12): DECISIONS, M1, M11, M12, M13, M14, M3, M4 (+4 more)

### Community 136 - "AiException"
Cohesion: 0.27
Nodes (3): AiException, Detail, Key

### Community 137 - "WeatherKind"
Cohesion: 0.22
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 138 - "Paths.File"
Cohesion: 0.22
Nodes (6): Paths, LogFile, Root, SettingsFile, Store, LocalPath

### Community 139 - "WebProvider.QueryAsync"
Cohesion: 0.39
Nodes (3): M8, AskAi, WebProvider

### Community 141 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 143 - "MicCapture"
Cohesion: 0.10
Nodes (4): ArraySampleProvider, WaveFormat, MicCapture, Duration

### Community 144 - "IdlePillView"
Cohesion: 0.27
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 145 - "system_text_json_nodes"
Cohesion: 0.18
Nodes (3): Isle.Agents, Isle.Views.Agents, Isle.Modules.Agents

### Community 147 - "ARCHITECTURE"
Cohesion: 0.25
Nodes (5): ARCHITECTURE, Process shape, Styles / Views, Tests (`tests/Isle.Tests`), TestSetup

### Community 149 - "Isle.Tests"
Cohesion: 0.08
Nodes (4): Isle.Modules.AiChat, Isle.Views.AiChat, Isle.Tests, CapabilityUse

### Community 151 - "Native"
Cohesion: 0.05
Nodes (18): TrayModule, Id, EnumWindowsProc, ICONINFO, INPUT, LASTINPUTINFO, LowLevelKeyboardProc, MonitorEnumProc (+10 more)

### Community 153 - "Isle.Core"
Cohesion: 0.15
Nodes (8): Isle.Views.Notes, Isle.Core, Isle.Views.Voice, Isle.Modules.Weather, Isle.Modules.Notes, Isle.Modules.Tray, Isle.Views.Weather, TrayIconTests

### Community 155 - "MdBlockKind"
Cohesion: 0.25
Nodes (8): MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote, Rule

### Community 156 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 160 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 161 - "CurrencyParser.Parse"
Cohesion: 0.24
Nodes (3): CurrencyQuery, CurrencyTests, UnitTests

## Knowledge Gaps
- **464 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `Id`, `S` (+459 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 723 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **52 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `RepeatRule`, `MediaModule`, `ChatAttachment`, `TimeSpan`, `Conversation`, `DECISIONS`, `MediaSession`, `Strings.T`, `Color`, `WebProvider.QueryAsync`, `OpenAiProvider`, `PlaybackMath.Choose`, `MicCapture`, `WeatherReport`, `ARCHITECTURE`, `TimerBook`, `ReminderParser`, `NotesStore`, `MediaState`, `WeatherModule`, `TimersModule`, `Logger.Error`, `Task`, `HookInstaller`, `CreatureDesign`, `HotkeyService.List`, `Attachments`, `TextAction`, `VpnDetector.Detect`, `Markdown.Parse`, `AgentsModule`, `AiPopupView`, `List`, `JsonObject`, `Strings.T`, `DateTimeOffset`, `TimersModule`, `VoiceModule`, `CreatureBrain`, `Fact`, `FrameworkElement`, `VoiceModule.Released`, `HotkeyService`, `BluetoothDevices.ListAsync`, `FrameworkElement`, `MediaModule.Stop`, `AiTests`, `VoiceTests`, `LyricsTracker`, `AudioVolume`, `CharactersModule`, `IDisposable`, `Lrc.Parse`, `Logger.Warn`, `App.StartModules`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **Are the 90 inferred relationships involving `Modules (`src/Isle/Modules`)` (e.g. with `ClipboardOps.CopySelectionAsync()` and `ClipboardOps.PasteTextAsync()`) actually correct?**
  _`Modules (`src/Isle/Modules`)` has 90 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _464 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `RepeatRule` be split into smaller, more focused modules?**
  _Cohesion score 0.12807881773399016 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `ShelfStore`, `ChatAttachment`, `Recorder.cs`, `Note`, `system_text_json_nodes`, `Spring`, `Isle.Tests`, `WeatherModule`, `Providers.cs`, `Screens.All`, `system_windows`, `IDisposable`, `system_windows_threading`, `IModule`, `AppIndex`, `ClipboardService.cs`, `MarkupWindow.cs`, `Sources.cs`, `TimerPill.cs`, `Strings`, `Strings.cs`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **Should `ShelfStore` be split into smaller, more focused modules?**
  _Cohesion score 0.09191919191919191 - nodes in this community are weakly interconnected._
- **Why does `VoiceModule` connect `VoiceModule` to `Isle.Core`, `Logger.Error`, `IslandWindow`, `Task`, `HookInstaller`, `Modules (`src/Isle/Modules`)`, `TimeSpan`, `MicCapture`, `IdlePillView`, `IDisposable`, `VoiceModule.Released`, `Native`, `App.StartModules`, `IndicatorPill`?**
  _High betweenness centrality (0.034) - this node is a cross-community bridge._