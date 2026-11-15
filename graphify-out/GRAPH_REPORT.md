# Graph Report - island  (2026-11-15)

## Corpus Check
- 176 files · ~135,847 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 3443 nodes · 10023 edges · 169 communities (115 shown, 54 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1337 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d485eba6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Action
- ShelfStore
- RatesSnapshot
- DispatcherTimer
- 4. Features (all of them)
- ShelfView
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
- Thickness
- WeatherModule.Start
- Task
- HookInstaller
- CreatureDesign
- WeatherData.cs
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
- JsonObject
- ToolsModule
- AiPopupView
- List
- JsonObject
- Strings.T
- DateTimeOffset
- FrameworkElement
- TimersModule
- VoicePill
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
- AudioVolume.Bind
- FrameworkElement
- IdleHttp
- TimeSpan
- OverlayWindow
- VoiceModule.Released
- HotkeyService
- BluetoothDevices.ListAsync
- ObsSource
- RainAlerts.Find
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
- CreatureView.FacesOf
- AiTests
- Units
- WeatherTests
- NotesFormattingTests.Formatting_survives_save_and_load
- VoiceTests
- Parser
- OpenMeteoParser.Forecast
- AppIndex
- TrayModule
- AudioVolume
- CharactersModule
- DeviceState
- system_windows_media_imaging
- ControlServices
- Lrc.Parse
- Logger.Warn
- MarkupWindow.cs
- ArraySampleProvider
- KeyboardLayoutSource
- ToolsTests
- App.StartModules
- TimerPill.cs
- NotesModule
- ModuleHost
- ChatMessage
- Fuzzy.Score
- WhisperModel
- ClipStore
- MediaModule
- ChatAttachment
- IDisposable
- Tool
- Note
- Conversation
- DECISIONS
- AiException
- WeatherKind
- Paths.File
- ShellIcons.For
- Logger.Info
- PlaybackMath.Choose
- OpenAiProvider
- MicCapture
- IdlePillView
- AttachmentTests
- NotificationsSource
- ARCHITECTURE
- Spring
- Isle.Tests
- ObsAuth.Response
- Native
- VpnIndicatorSource
- Isle.Core
- KnownFolders.SHGetKnownFolderPath
- MarqueeText
- RepeatKind
- JsonStore.Save
- Secret
- ClipSnapshots.Register
- LyricsStatus
- Dictionary
- Brightness
- LyricsModule
- SettingsView
- EventBusTests.Publish_reaches_subscribers_until_disposed_and_survives_throwing_handlers
- RECT

## God Nodes (most connected - your core abstractions)
1. `Modules (`src/Isle/Modules`)` - 135 edges
2. `Isle.Core` - 108 edges
3. `Thickness` - 100 edges
4. `Native` - 90 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 51 edges
7. `SettingsView` - 48 edges
8. `NotesView` - 47 edges
9. `AiModule` - 46 edges
10. `ControlView` - 46 edges

## Surprising Connections (you probably didn't know these)
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `M5` --references--> `IAudioEndpointVolume`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M6` --references--> `ILiveSink`  [INFERRED]
  DECISIONS.md → src/Isle/Modules/LiveEvents/Sources.cs
- `M10` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `M11` --references--> `AiPopupView`  [INFERRED]
  DECISIONS.md → src/Isle/Views/AiChat/AiPopupView.cs

## Import Cycles
- None detected.

## Communities (169 total, 54 thin omitted)

### Community 1 - "ShelfStore"
Cohesion: 0.15
Nodes (11): ShelfFile, Items, Version, ShelfItem, Added, Id, Path, ShelfStore (+3 more)

### Community 3 - "RatesSnapshot"
Cohesion: 0.17
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "DispatcherTimer"
Cohesion: 0.12
Nodes (4): Memory, ClipboardModule, Id, ClipboardService

### Community 5 - "4. Features (all of them)"
Cohesion: 0.04
Nodes (37): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+29 more)

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.09
Nodes (19): net10.0, net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0) (+11 more)

### Community 11 - "Strings.T"
Cohesion: 0.07
Nodes (7): AiModule, Busy, Current, Id, S, AiSnapshots, AiChatView

### Community 12 - "Color"
Cohesion: 0.14
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 13 - "Isle"
Cohesion: 0.29
Nodes (6): Build and run, Data and privacy, Debug flags, Hotkeys (all can be changed in Settings → Hotkeys), Isle, What it does

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.07
Nodes (5): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc

### Community 18 - "WeatherReport"
Cohesion: 0.12
Nodes (19): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+11 more)

### Community 19 - "MusicView"
Cohesion: 0.14
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, MusicView, LyricsOpen

### Community 20 - "ResourceDictionary"
Cohesion: 0.14
Nodes (17): ToolTip, Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary (+9 more)

### Community 21 - "TimerBook"
Cohesion: 0.06
Nodes (29): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+21 more)

### Community 22 - "Equalizer"
Cohesion: 0.18
Nodes (7): MediaSnapshots, ArtImage, Equalizer, Playing, MusicPill, PillWidth, TrackBanner

### Community 23 - "ReminderParser"
Cohesion: 0.08
Nodes (13): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParser, Rule (+5 more)

### Community 26 - "NotesStore"
Cohesion: 0.21
Nodes (5): NotesSnapshots, NotesStore, Doc, Folders, Notes

### Community 27 - "Node"
Cohesion: 0.17
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.09
Nodes (7): CalculatorTests, UnitTests, DownloadDetectorTests, LiveMiscTests, PlaybackTests, ExcludedAppTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.08
Nodes (20): FakeMediaSource, Current, IMediaSource, Current, MediaState, Accent, Album, Art (+12 more)

### Community 30 - "WeatherModule"
Cohesion: 0.13
Nodes (7): InPill, WeatherModule, Fahrenheit, HasPlace, Id, S, WeatherPill

### Community 32 - "TimersModule"
Cohesion: 0.19
Nodes (7): TimersSnapshots, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 34 - "ControlView"
Cohesion: 0.10
Nodes (7): BtDevice, WindowsTheme, IsDark, VpnEntry, CanToggle, ControlView, Tile

### Community 35 - "Thickness"
Cohesion: 0.10
Nodes (11): AgentsView, OverlayOpen, CharacterGallery, Icon, Data, Filled, Foreground, Stroke (+3 more)

### Community 36 - "WeatherModule.Start"
Cohesion: 0.24
Nodes (5): WeatherService, LastFailed, WeatherSnapshots, WeatherActions, FakeSource

### Community 37 - "Task"
Cohesion: 0.17
Nodes (3): Place, IWeatherSource, OpenMeteoSource

### Community 38 - "HookInstaller"
Cohesion: 0.13
Nodes (6): Entry, HookInstaller, InstalledExe, HookPlan, HookTarget, Detected

### Community 39 - "CreatureDesign"
Cohesion: 0.11
Nodes (16): CreatureDesign, Body, BodyBrush, CheekBrush, CheekDx, CheekY, Extra, ExtraBrush (+8 more)

### Community 40 - "WeatherData.cs"
Cohesion: 0.12
Nodes (6): Isle.Views.Voice, Isle.Modules.Weather, Isle.Modules.Commands, Isle.Views.Weather, Isle.Modules.Voice, Isle.Views.Commands

### Community 41 - "Icon"
Cohesion: 0.23
Nodes (5): TabBar, AvailableWidth, Hidden, Mascot, SelectedId

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.33
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.09
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.05
Nodes (15): Shell, ITabView, IslandWindow, CurrentTab, IdleView, TabList, IslandWindow, FileDropTarget (+7 more)

### Community 46 - "system_windows"
Cohesion: 0.09
Nodes (6): Isle.Modules.Characters, Isle, Isle.Views, Isle.Views.Agents, Isle.Views.Characters, FreezeExt

### Community 47 - "Settings"
Cohesion: 0.09
Nodes (21): Core (`src/Isle/Core`), Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys (+13 more)

### Community 49 - "TextAction"
Cohesion: 0.13
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 50 - "CommandItem"
Cohesion: 0.08
Nodes (22): Modules (`src/Isle/Modules`), M8, ClipPrivacy, CommandSnapshots, AppsProvider, AskAi, CalculatorProvider, Cmd (+14 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.08
Nodes (15): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+7 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.15
Nodes (6): CoreAudioIds, IKsControl, IPropertyStore, KsProperty, PropertyKey, PropVariant

### Community 54 - "Markdown.Parse"
Cohesion: 0.09
Nodes (17): Markdown, MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote (+9 more)

### Community 55 - "IslandWindow"
Cohesion: 0.17
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 56 - "AgentsModule"
Cohesion: 0.12
Nodes (5): AgentsModule, Id, S, TimeoutSeconds, SleepMinutes

### Community 57 - "JsonObject"
Cohesion: 0.14
Nodes (6): HookProtocol, PipeName, AgentQuestion, HookMessage, Event, AgentText

### Community 58 - "ToolsModule"
Cohesion: 0.13
Nodes (3): ToolsModule, Id, ColorText

### Community 60 - "List"
Cohesion: 0.12
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 61 - "JsonObject"
Cohesion: 0.08
Nodes (25): AgentBook, AllPending, Count, Sessions, Urgent, AgentRequest, Detail, Id (+17 more)

### Community 63 - "DateTimeOffset"
Cohesion: 0.13
Nodes (9): Service, DiskEntry, ILyricsHttp, LyricsResult, LyricsService, TrackQuery, CacheKey, FakeHttp (+1 more)

### Community 65 - "TimersModule"
Cohesion: 0.25
Nodes (3): TimersModule, Controller, Id

### Community 67 - "VoiceModule"
Cohesion: 0.16
Nodes (6): Phase, Idle, Recording, Transcribing, VoiceModule, Id

### Community 68 - "system_windows_threading"
Cohesion: 0.09
Nodes (4): Isle.Views.Live, Isle.Modules.ControlCenter, Isle.Modules.LiveEvents, Isle.Views.Control

### Community 70 - "IAudioEndpointVolume"
Cohesion: 0.12
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 71 - "LiveEventsModule"
Cohesion: 0.13
Nodes (7): LiveEventsModule, Id, PillSink, Ui, CapsLockSource, ILiveSource, MicCameraSource

### Community 72 - "CommandBarView"
Cohesion: 0.17
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "CreatureBrain"
Cohesion: 0.20
Nodes (6): M14, CreatureBrain, MusicPlaying, SleepAfter, CharacterTests, FakeClock

### Community 74 - "IModule"
Cohesion: 0.18
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 75 - "ClipEntry"
Cohesion: 0.13
Nodes (14): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+6 more)

### Community 76 - "Fact"
Cohesion: 0.20
Nodes (3): AgentPipeServer, AgentTests, StringsTests

### Community 77 - "ForegroundWatcher"
Cohesion: 0.13
Nodes (3): M6, ForegroundWatcher, LastExternal

### Community 78 - "AudioVolume.Bind"
Cohesion: 0.21
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 79 - "FrameworkElement"
Cohesion: 0.21
Nodes (7): AgentState, Done, Idle, Waiting, Working, Name, AgentPill

### Community 80 - "IdleHttp"
Cohesion: 0.13
Nodes (5): M10, IdleHttp, CommandModule, Id, LrclibHttp

### Community 81 - "TimeSpan"
Cohesion: 0.15
Nodes (10): DurationParser, TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning (+2 more)

### Community 82 - "OverlayWindow"
Cohesion: 0.12
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "VoiceModule.Released"
Cohesion: 0.16
Nodes (6): VoiceSelfTest, ITranscriber, LanguagePick, Transcript, WhisperEngine, IsLoaded

### Community 85 - "BluetoothDevices.ListAsync"
Cohesion: 0.18
Nodes (4): BluetoothDevices, IMMDevice, IMMDeviceCollection, IMMDeviceEnumerator

### Community 87 - "RainAlerts.Find"
Cohesion: 0.30
Nodes (3): RainAlert, RainAlerts, WeatherCodes

### Community 88 - "ILiveSink"
Cohesion: 0.21
Nodes (4): ILiveSink, Ui, FakeSink, Ui

### Community 92 - "Gesture"
Cohesion: 0.20
Nodes (4): Gesture, IsValid, SettingsPanelTests, GestureTests

### Community 94 - "Mood"
Cohesion: 0.24
Nodes (6): CharacterSnapshots, Mood, Awake, Dancing, Happy, Sleeping

### Community 96 - "Calculator.TryEvaluate"
Cohesion: 0.21
Nodes (3): Calculator, Result, CurrencyQuery

### Community 97 - "PillQueue"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 99 - "MediaModule.Stop"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 101 - "AiTests"
Cohesion: 0.32
Nodes (5): AiConfig, IsReady, AnthropicProvider, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 104 - "NotesFormattingTests.Formatting_survives_save_and_load"
Cohesion: 0.28
Nodes (3): Isle.Views.Notes, Isle.Modules.Notes, NotesFormattingTests

### Community 105 - "VoiceTests"
Cohesion: 0.23
Nodes (5): ChecksumException, ModelDownloader, BytesHandler, SyncProgress, VoiceTests

### Community 108 - "AppIndex"
Cohesion: 0.19
Nodes (10): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+2 more)

### Community 110 - "AudioVolume"
Cohesion: 0.10
Nodes (11): M5, AudioVolume, Available, Level, Muted, DeviceCallback, VolumeCallback, Audio (+3 more)

### Community 111 - "CharactersModule"
Cohesion: 0.16
Nodes (6): CharactersModule, Brain, Id, S, Selected, GalleryActions

### Community 112 - "DeviceState"
Cohesion: 0.29
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 113 - "system_windows_media_imaging"
Cohesion: 0.10
Nodes (7): Isle.Modules.Clipboard, Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media, Isle.Views.Shelf, Isle.Modules.Shelf, Isle.Views.Clipboard

### Community 114 - "ControlServices"
Cohesion: 0.14
Nodes (8): ControlServices, Brightness, Radios, VpnEnvironment, Radios, BluetoothOn, Denied, WifiOn

### Community 116 - "Logger.Warn"
Cohesion: 0.16
Nodes (4): ClipboardOps, IsQuiet, InputUnion, KEYBDINPUT

### Community 118 - "ArraySampleProvider"
Cohesion: 0.24
Nodes (3): ArraySampleProvider, WaveFormat, AudioMath

### Community 121 - "App.StartModules"
Cohesion: 0.17
Nodes (4): ControlModule, Id, ShelfModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.27
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 123 - "NotesModule"
Cohesion: 0.27
Nodes (4): NotesModule, Id, Store, View

### Community 124 - "ModuleHost"
Cohesion: 0.23
Nodes (5): ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 125 - "ChatMessage"
Cohesion: 0.20
Nodes (7): ChatMessage, Attachments, Error, Note, Role, Stopped, Text

### Community 127 - "WhisperModel"
Cohesion: 0.22
Nodes (7): WhisperModel, IsDownloaded, SizeMb, Url, WhisperModels, All, Model

### Community 128 - "ClipStore"
Cohesion: 0.18
Nodes (6): ClipFile, Entries, Version, ClipStore, Entries, Limit

### Community 129 - "MediaModule"
Cohesion: 0.11
Nodes (8): MediaModule, Id, IsPlaying, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPlaying

### Community 130 - "ChatAttachment"
Cohesion: 0.13
Nodes (14): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+6 more)

### Community 132 - "Tool"
Cohesion: 0.29
Nodes (7): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Text

### Community 133 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 134 - "Conversation"
Cohesion: 0.15
Nodes (11): Conversation, Id, Messages, SystemPrompt, Title, Updated, ChatStore, All (+3 more)

### Community 135 - "DECISIONS"
Cohesion: 0.09
Nodes (15): DECISIONS, M0, M1, M11, M12, M13, M15, M3 (+7 more)

### Community 136 - "AiException"
Cohesion: 0.33
Nodes (3): AiException, Detail, Key

### Community 137 - "WeatherKind"
Cohesion: 0.22
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 138 - "Paths.File"
Cohesion: 0.22
Nodes (6): Paths, LogFile, Root, SettingsFile, Store, LocalPath

### Community 140 - "Logger.Info"
Cohesion: 0.12
Nodes (4): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Logger, RecordOptions, ScreenGrab

### Community 141 - "PlaybackMath.Choose"
Cohesion: 0.22
Nodes (3): M2, PlaybackMath, VERIFICATION

### Community 144 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 147 - "ARCHITECTURE"
Cohesion: 0.25
Nodes (5): ARCHITECTURE, Process shape, Styles / Views, Tests (`tests/Isle.Tests`), TestSetup

### Community 149 - "Isle.Tests"
Cohesion: 0.07
Nodes (4): Isle.Agents, Isle.Tests, Isle.Modules.Agents, CapabilityUse

### Community 151 - "Native"
Cohesion: 0.07
Nodes (11): EnumWindowsProc, INPUT, LASTINPUTINFO, LowLevelKeyboardProc, MonitorEnumProc, MONITORINFOEX, MOUSEINPUT, Native (+3 more)

### Community 153 - "Isle.Core"
Cohesion: 0.16
Nodes (7): Isle.Views.SettingsPanel, Isle.Core, Isle.Modules.AiChat, Isle.Modules.Hotkeys, Isle.Modules.Tray, Isle.Views.AiChat, PillPriority

### Community 155 - "MarqueeText"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 156 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 160 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 162 - "Dictionary"
Cohesion: 0.16
Nodes (5): CurrencyParser, SpeedMeter, BytesPerSecond, Item, SteamSource

### Community 163 - "Brightness"
Cohesion: 0.40
Nodes (3): Brightness, Level, Supported

### Community 165 - "SettingsView"
Cohesion: 0.09
Nodes (9): Settings panel (`Views/SettingsPanel`), Autostart, IsEnabled, HotkeyInfo, SettingsSnapshots, SettingsActions, SettingsView, S (+1 more)

### Community 167 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **467 isolated node(s):** `PasteTarget`, `IsHiddenByUser`, `Items`, `Version`, `Added` (+462 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 743 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **54 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `CommandItem` to `ShelfStore`, `RatesSnapshot`, `DispatcherTimer`, `MediaSession`, `Strings.T`, `Color`, `Recorder`, `MusicView`, `TimerBook`, `Equalizer`, `ReminderParser`, `NotesStore`, `Node`, `MediaState`, `WeatherModule`, `TimersModule`, `ControlView`, `WeatherModule.Start`, `Task`, `HookInstaller`, `CreatureDesign`, `Attachments`, `TextAction`, `VpnDetector.Detect`, `DownloadsSource`, `Markdown.Parse`, `AgentsModule`, `JsonObject`, `ToolsModule`, `AiPopupView`, `List`, `JsonObject`, `Strings.T`, `DateTimeOffset`, `TimersModule`, `VoiceModule`, `LiveEventsModule`, `CreatureBrain`, `Fact`, `FrameworkElement`, `IdleHttp`, `TimeSpan`, `VoiceModule.Released`, `HotkeyService`, `BluetoothDevices.ListAsync`, `ObsSource`, `RainAlerts.Find`, `ILiveSink`, `RegistryWatcher`, `Calculator.TryEvaluate`, `FrameworkElement`, `AiTests`, `Units`, `VoiceTests`, `OpenMeteoParser.Forecast`, `AppIndex`, `AudioVolume`, `CharactersModule`, `ControlServices`, `Lrc.Parse`, `Logger.Warn`, `ArraySampleProvider`, `KeyboardLayoutSource`, `ToolsTests`, `App.StartModules`, `NotesModule`, `Fuzzy.Score`, `WhisperModel`, `ClipStore`, `MediaModule`, `ChatAttachment`, `Conversation`, `DECISIONS`, `Logger.Info`, `PlaybackMath.Choose`, `OpenAiProvider`, `MicCapture`, `NotificationsSource`, `ARCHITECTURE`, `ObsAuth.Response`, `VpnIndicatorSource`, `Dictionary`, `Brightness`, `SettingsView`?**
  _High betweenness centrality (0.241) - this node is a cross-community bridge._
- **Are the 134 inferred relationships involving `Modules (`src/Isle/Modules`)` (e.g. with `ClipboardOps.CopySelectionAsync()` and `ClipboardOps.PasteTextAsync()`) actually correct?**
  _`Modules (`src/Isle/Modules`)` has 134 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PasteTarget`, `IsHiddenByUser`, `Items` to the rest of the system?**
  _467 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ShelfStore` be split into smaller, more focused modules?**
  _Cohesion score 0.1455026455026455 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `ShelfStore`, `Recorder.cs`, `IDisposable`, `ChatAttachment`, `4. Features (all of them)`, `Note`, `Spring`, `Isle.Tests`, `WeatherData.cs`, `Screens.All`, `system_windows`, `CommandItem`, `system_windows_threading`, `IModule`, `NotesFormattingTests.Formatting_survives_save_and_load`, `AppIndex`, `system_windows_media_imaging`, `MarkupWindow.cs`, `TimerPill.cs`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **Should `Recorder.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.11578947368421053 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `Action`, `FrameworkElement`, `Dictionary`, `Thickness`, `DispatcherTimer`, `SettingsView`, `LiveEventsModule`, `Screens.All`, `App`, `TrayModule`, `system_windows`, `Logger.Info`, `Window`, `HotkeyService`, `KeyboardLayoutSource`, `Native`, `AgentsModule`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._