# Graph Report - island  (2026-11-15)

## Corpus Check
- 154 files · ~112,820 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 3032 nodes · 8589 edges · 167 communities (110 shown, 57 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 1140 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `cb6377ec`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- IntPtr
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
- Logger.Error
- TempDir
- Task
- LyricsModule
- AttachmentTests
- system_text
- TabBar
- PreserveSig
- Screens.All
- App
- IslandWindow
- system_windows
- Settings
- Attachments
- TextAction
- IEnumerable
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
- IDisposable
- VoiceModule
- Isle.Modules.Media
- IAudioEndpointVolume
- LiveEventsModule.Create
- CommandBarView
- Theme
- ArraySampleProvider
- ClipEntry
- Fact
- ForegroundWatcher
- TrayModule
- CoreTests.cs
- IdleHttp
- TimeSpan
- OverlayWindow
- VoiceModule.Released
- HotkeyService
- IMMDevice
- ObsSource
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- ILiveSink
- IMMNotificationClient
- IndicatorPill
- ClipStore
- Gesture
- RegistryWatcher
- TrayIcon.Create
- CommandModule
- Regex
- PillItem
- FrameworkElement
- Strings
- Strings.cs
- AiTests
- Units
- Modules (`src/Isle/Modules`)
- NotesView.SelectedParagraphs
- VoiceTests
- Parser
- LyricsTracker
- AppIndex
- ClipPrivacy.IsExcludedApp
- EDataFlow
- ShelfView
- DeviceState
- DispatcherTimer
- ControlServices
- Lrc.Parse
- Logger.Warn
- system_windows_media
- AudioVolume
- SteamSource
- ToolsTests
- App.StartModules
- TimerPill.cs
- Autostart
- ModuleHost
- ChatMessage
- Fuzzy.Score
- LiveMiscTests
- Func
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
- Isle.Modules.Commands
- OpenAiProvider
- MicCapture
- IdlePillView
- Isle.Core
- ShellIcons.For
- Spring
- Isle.Tests
- NotesModule
- Native
- WhisperModel
- WeatherView.cs
- NotificationsSource
- ARCHITECTURE
- JsonStore.Save
- PROGRESS
- Secret
- ClipSnapshots.Register
- LyricsStatus
- CurrencyParser.Parse
- Brightness
- LanguagePick
- ExcludedAppTests.Matches_process_names
- RECT

## God Nodes (most connected - your core abstractions)
1. `Isle.Core` - 97 edges
2. `Native` - 88 edges
3. `Modules (`src/Isle/Modules`)` - 79 edges
4. `Thickness` - 73 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 50 edges
7. `AiModule` - 47 edges
8. `NotesView` - 47 edges
9. `ControlView` - 46 edges
10. `IslandWindow` - 46 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Last action` --references--> `VoiceTests`  [INFERRED]
  PROGRESS.md → tests/Isle.Tests/VoiceTests.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `Modules (`src/Isle/Modules`)` --references--> `MicCapture`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Voice/MicCapture.cs
- `Modules (`src/Isle/Modules`)` --references--> `AudioMath`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Voice/MicCapture.cs

## Import Cycles
- None detected.

## Communities (167 total, 57 thin omitted)

### Community 1 - "ShelfStore"
Cohesion: 0.13
Nodes (12): Isle.Modules.Shelf, ShelfFile, Items, Version, ShelfItem, Added, Id, Path (+4 more)

### Community 2 - "SystemControls.cs"
Cohesion: 0.09
Nodes (5): Isle.Views.Live, Isle.Modules.ControlCenter, Isle.Modules.Hotkeys, Isle.Modules.LiveEvents, Isle.Views.Control

### Community 3 - "RatesSnapshot"
Cohesion: 0.19
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 4 - "IslandWindow"
Cohesion: 0.14
Nodes (5): IslandWindow, CurrentTab, IdleView, TabList, IsExpanded

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "TimersController"
Cohesion: 0.11
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 9 - "MediaSession"
Cohesion: 0.12
Nodes (6): M2, Logger, MediaSession, Current, PlaybackMath, VERIFICATION

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.10
Nodes (16): net10.0-windows10.0.19041.0, Anthropic (13.8.0), Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3) (+8 more)

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "Recorder"
Cohesion: 0.06
Nodes (6): IDirect3DDxgiInterfaceAccess, Recorder, Elapsed, FilePath, RegionDesc, RecordOptions

### Community 18 - "WeatherReport"
Cohesion: 0.07
Nodes (26): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+18 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimerBook"
Cohesion: 0.10
Nodes (16): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+8 more)

### Community 22 - "MediaModule"
Cohesion: 0.21
Nodes (5): ArtImage, Equalizer, Playing, MusicPill, PillWidth

### Community 23 - "ReminderParser"
Cohesion: 0.06
Nodes (21): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly (+13 more)

### Community 26 - "NotesStore"
Cohesion: 0.22
Nodes (5): NotesSnapshots, NotesStore, Doc, Folders, Notes

### Community 27 - "Node"
Cohesion: 0.16
Nodes (6): Node, Steam, SteamDownload, Progress, Vdf, SteamParserTests

### Community 28 - "Theory"
Cohesion: 0.22
Nodes (4): CalculatorTests, UnitTests, PlaybackTests, DurationParserTests

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (17): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+9 more)

### Community 30 - "WeatherModule"
Cohesion: 0.11
Nodes (8): RainAlert, WeatherModule, Fahrenheit, HasPlace, Id, S, WeatherSnapshots, WeatherPill

### Community 32 - "TimersModule"
Cohesion: 0.13
Nodes (11): Modules (`src/Isle/Modules`), M3, TimerFinished, TimersSnapshots, ReminderBanner, Ring, Brush, Progress (+3 more)

### Community 33 - "MarkupWindow"
Cohesion: 0.18
Nodes (4): MarkupWindow, Stroke, PickButton, IsChecked

### Community 34 - "ControlView"
Cohesion: 0.10
Nodes (8): BluetoothDevices, BtDevice, WindowsTheme, IsDark, VpnEntry, CanToggle, ControlView, Tile

### Community 35 - "Logger.Error"
Cohesion: 0.15
Nodes (7): Icon, Data, Filled, Foreground, Stroke, Thickness, ToolsView

### Community 36 - "TempDir"
Cohesion: 0.21
Nodes (6): Core (`src/Isle/Core`), Isle.Modules.Tray, SettingsTests, TempDir, Path, NotesStoreTests

### Community 37 - "Task"
Cohesion: 0.19
Nodes (4): Place, IWeatherSource, OpenMeteoSource, WeatherActions

### Community 38 - "LyricsModule"
Cohesion: 0.33
Nodes (3): LyricsModule, Id, Service

### Community 41 - "TabBar"
Cohesion: 0.31
Nodes (4): TabBar, AvailableWidth, Hidden, SelectedId

### Community 42 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 43 - "Screens.All"
Cohesion: 0.26
Nodes (3): ScreenInfo, Screens, Screen

### Community 44 - "App"
Cohesion: 0.12
Nodes (6): Shell, Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.21
Nodes (3): IslandWindow, IsHiddenByUser, PasteTarget

### Community 47 - "Settings"
Cohesion: 0.11
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 49 - "TextAction"
Cohesion: 0.15
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 50 - "IEnumerable"
Cohesion: 0.08
Nodes (19): M8, CommandSnapshots, AppsProvider, AskAi, CalculatorProvider, Cmd, CommandItem, Confirm (+11 more)

### Community 51 - "VpnDetector.Detect"
Cohesion: 0.08
Nodes (16): EmptyVpnEnvironment, LocalAppData, ProgramFiles, Client, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment (+8 more)

### Community 52 - "CoreAudio.cs"
Cohesion: 0.21
Nodes (3): IPropertyStore, PropertyKey, PropVariant

### Community 53 - "DownloadsSource"
Cohesion: 0.19
Nodes (3): DownloadDetector, DownloadsSource, DownloadDetectorTests

### Community 54 - "Markdown.Parse"
Cohesion: 0.09
Nodes (18): Markdown, MdBlock, MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph (+10 more)

### Community 55 - "IslandWindow"
Cohesion: 0.13
Nodes (6): IslandWindow, FileDropTarget, Handle, Hotkeys, Pill, Watcher

### Community 57 - "TimerItem"
Cohesion: 0.22
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 60 - "List"
Cohesion: 0.12
Nodes (15): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+7 more)

### Community 61 - "Grid"
Cohesion: 0.43
Nodes (3): MarqueeText, Inner, Text

### Community 63 - "DateTimeOffset"
Cohesion: 0.16
Nodes (7): DiskEntry, LyricsResult, LyricsService, TrackQuery, CacheKey, FakeHttp, LyricsServiceTests

### Community 64 - "FrameworkElement"
Cohesion: 0.18
Nodes (4): TabInfo, IslandWindow, IslandWindow, ToolsSnapshots

### Community 65 - "TimersModule"
Cohesion: 0.22
Nodes (3): TimersModule, Controller, Id

### Community 66 - "IDisposable"
Cohesion: 0.19
Nodes (3): EventBus, Unsubscriber, CurrencyParser

### Community 67 - "VoiceModule"
Cohesion: 0.13
Nodes (8): Phase, Idle, Recording, Transcribing, VoiceModule, Id, VoiceSnapshots, VoicePill

### Community 68 - "Isle.Modules.Media"
Cohesion: 0.24
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 71 - "LiveEventsModule.Create"
Cohesion: 0.13
Nodes (6): LiveEventsModule, Id, CapsLockSource, ILiveSource, KeyboardLayoutSource, MicCameraSource

### Community 72 - "CommandBarView"
Cohesion: 0.16
Nodes (6): CommandBarView, Results, SelectedIndex, Text, CommandBarTests, Delayed

### Community 73 - "Theme"
Cohesion: 0.31
Nodes (3): Theme, Accent, IsDark

### Community 75 - "ClipEntry"
Cohesion: 0.11
Nodes (15): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+7 more)

### Community 80 - "IdleHttp"
Cohesion: 0.23
Nodes (4): M10, IdleHttp, ILyricsHttp, LrclibHttp

### Community 82 - "OverlayWindow"
Cohesion: 0.11
Nodes (9): PxRect, IsEmpty, OverlayWindow, Monitor, Selection, SelectionSession, SelectMode, Color (+1 more)

### Community 83 - "VoiceModule.Released"
Cohesion: 0.16
Nodes (6): AudioMath, VoiceSelfTest, ITranscriber, Transcript, WhisperEngine, IsLoaded

### Community 84 - "HotkeyService"
Cohesion: 0.25
Nodes (3): Entry, HotkeyInfo, HotkeyService

### Community 85 - "IMMDevice"
Cohesion: 0.16
Nodes (5): IKsControl, IMMDevice, IMMDeviceCollection, IMMDeviceEnumerator, KsProperty

### Community 87 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 88 - "ILiveSink"
Cohesion: 0.18
Nodes (6): PillSink, Ui, ILiveSink, Ui, FakeSink, Ui

### Community 89 - "IMMNotificationClient"
Cohesion: 0.16
Nodes (7): M5, DeviceCallback, VolumeCallback, Audio, AudioVolumeNotificationData, IAudioEndpointVolumeCallback, IMMNotificationClient

### Community 91 - "ClipStore"
Cohesion: 0.20
Nodes (6): ClipFile, Entries, Version, ClipStore, Entries, Limit

### Community 92 - "Gesture"
Cohesion: 0.25
Nodes (3): Gesture, IsValid, GestureTests

### Community 94 - "TrayIcon.Create"
Cohesion: 0.25
Nodes (3): TrayIcon, ICONINFO, TrayIconTests

### Community 96 - "Regex"
Cohesion: 0.16
Nodes (3): Calculator, Result, TranscriptText

### Community 97 - "PillItem"
Cohesion: 0.11
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 99 - "Strings"
Cohesion: 0.20
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Strings, English, Instance, Language, this[], Ukrainian

### Community 101 - "AiTests"
Cohesion: 0.44
Nodes (5): AiConfig, IsReady, AnthropicProvider, AiTests, FakeHandler

### Community 102 - "Units"
Cohesion: 0.19
Nodes (7): Kind, Data, Length, Mass, Temp, U, Units

### Community 103 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.10
Nodes (6): AiModule, Busy, Current, Id, S, AiSnapshots

### Community 104 - "NotesView.SelectedParagraphs"
Cohesion: 0.24
Nodes (3): Isle.Views.Notes, Isle.Modules.Notes, NotesFormattingTests

### Community 105 - "VoiceTests"
Cohesion: 0.23
Nodes (5): ChecksumException, ModelDownloader, BytesHandler, SyncProgress, VoiceTests

### Community 107 - "LyricsTracker"
Cohesion: 0.19
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 108 - "AppIndex"
Cohesion: 0.19
Nodes (10): AppIndex, Entries, EntryKind, App, File, Recent, IndexEntry, IndexFile (+2 more)

### Community 110 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 112 - "DeviceState"
Cohesion: 0.25
Nodes (6): DeviceState, Active, All, Disabled, NotPresent, Unplugged

### Community 114 - "ControlServices"
Cohesion: 0.11
Nodes (11): ControlModule, Id, ControlServices, Brightness, Radios, VpnEnvironment, ControlSnapshots, Radios (+3 more)

### Community 115 - "Lrc.Parse"
Cohesion: 0.20
Nodes (3): Lrc, LrcLine, LrcTests

### Community 116 - "Logger.Warn"
Cohesion: 0.19
Nodes (4): ClipboardOps, IsQuiet, InputUnion, KEYBDINPUT

### Community 117 - "system_windows_media"
Cohesion: 0.11
Nodes (5): Isle.Modules.Clipboard, Isle.Modules.Tools, Isle.Views.Shelf, Isle.Views.Clipboard, Isle.Views.Tools

### Community 118 - "AudioVolume"
Cohesion: 0.17
Nodes (4): AudioVolume, Available, Level, Muted

### Community 119 - "SteamSource"
Cohesion: 0.23
Nodes (4): SpeedMeter, BytesPerSecond, Item, SteamSource

### Community 120 - "ToolsTests"
Cohesion: 0.20
Nodes (3): QrReader, ColorText, ToolsTests

### Community 121 - "App.StartModules"
Cohesion: 0.12
Nodes (4): ClipboardModule, Id, ShelfModule, Id

### Community 122 - "TimerPill.cs"
Cohesion: 0.31
Nodes (5): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests, TimerBookTests

### Community 124 - "ModuleHost"
Cohesion: 0.24
Nodes (5): ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 125 - "ChatMessage"
Cohesion: 0.15
Nodes (7): ChatMessage, Attachments, Error, Note, Role, Stopped, Text

### Community 127 - "LiveMiscTests"
Cohesion: 0.18
Nodes (3): ConsentStore, ObsAuth, LiveMiscTests

### Community 128 - "Func"
Cohesion: 0.16
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 129 - "MediaModule"
Cohesion: 0.17
Nodes (7): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, TrackBanner

### Community 130 - "ChatAttachment"
Cohesion: 0.13
Nodes (14): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+6 more)

### Community 131 - "FakeMediaSource"
Cohesion: 0.17
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 132 - "IslandWindow"
Cohesion: 0.25
Nodes (8): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect, Text

### Community 133 - "Note"
Cohesion: 0.13
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 134 - "Conversation"
Cohesion: 0.18
Nodes (11): Conversation, Id, Messages, SystemPrompt, Title, Updated, ChatStore, All (+3 more)

### Community 135 - "DECISIONS"
Cohesion: 0.14
Nodes (11): DECISIONS, M0, M1, M11, M12, M4, M6, M7 (+3 more)

### Community 136 - "AiException"
Cohesion: 0.24
Nodes (3): AiException, Detail, Key

### Community 137 - "WeatherKind"
Cohesion: 0.18
Nodes (10): WeatherCodes, WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain (+2 more)

### Community 138 - "Paths.File"
Cohesion: 0.20
Nodes (6): Paths, LogFile, Root, SettingsFile, Store, LocalPath

### Community 144 - "IdlePillView"
Cohesion: 0.31
Nodes (5): IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 145 - "Isle.Core"
Cohesion: 0.21
Nodes (4): Isle.Core, Isle.Modules.AiChat, Isle.Views.AiChat, PillPriority

### Community 150 - "NotesModule"
Cohesion: 0.27
Nodes (4): NotesModule, Id, Store, View

### Community 151 - "Native"
Cohesion: 0.07
Nodes (10): EnumWindowsProc, INPUT, LowLevelKeyboardProc, MonitorEnumProc, MONITORINFOEX, MOUSEINPUT, Native, NOTIFYICONDATA (+2 more)

### Community 152 - "WhisperModel"
Cohesion: 0.22
Nodes (7): WhisperModel, IsDownloaded, SizeMb, Url, WhisperModels, All, Model

### Community 153 - "WeatherView.cs"
Cohesion: 0.33
Nodes (3): Isle.Views.Voice, Isle.Modules.Weather, Isle.Views.Weather

### Community 155 - "ARCHITECTURE"
Cohesion: 0.25
Nodes (5): ARCHITECTURE, Process shape, Styles / Views, Tests (`tests/Isle.Tests`), TestSetup

### Community 157 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 160 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 162 - "Brightness"
Cohesion: 0.40
Nodes (3): Brightness, Level, Supported

### Community 166 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **406 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `net10.0-windows10.0.19041.0`, `Anthropic (13.8.0)` (+401 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 656 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **57 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `TimersModule` to `MediaModule`, `ChatAttachment`, `FakeMediaSource`, `Conversation`, `TimersController`, `MediaSession`, `WeatherKind`, `Color`, `OpenAiProvider`, `MicCapture`, `WeatherReport`, `TimerBook`, `NotesModule`, `ReminderParser`, `WhisperModel`, `NotesStore`, `ARCHITECTURE`, `MediaState`, `WeatherModule`, `ControlView`, `Brightness`, `LanguagePick`, `Task`, `Attachments`, `TextAction`, `IEnumerable`, `VpnDetector.Detect`, `Markdown.Parse`, `AiPopupView`, `List`, `Strings.T`, `DateTimeOffset`, `TimersModule`, `VoiceModule`, `IdleHttp`, `TimeSpan`, `VoiceModule.Released`, `HotkeyService`, `TrayIcon.Create`, `Regex`, `FrameworkElement`, `AiTests`, `Modules (`src/Isle/Modules`)`, `VoiceTests`, `LyricsTracker`, `ControlServices`, `Lrc.Parse`, `Logger.Warn`, `AudioVolume`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _406 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `IntPtr` be split into smaller, more focused modules?**
  _Cohesion score 0.09725158562367865 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Func`, `ShelfStore`, `SystemControls.cs`, `ChatAttachment`, `RatesSnapshot`, `Note`, `Paths.File`, `Isle.Modules.Commands`, `Spring`, `Isle.Tests`, `WeatherView.cs`, `TempDir`, `Task`, `system_text`, `Screens.All`, `system_windows`, `IEnumerable`, `Recorder.cs`, `IDisposable`, `Isle.Modules.Media`, `Strings`, `Strings.cs`, `NotesView.SelectedParagraphs`, `AppIndex`, `system_windows_media`, `TimerPill.cs`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **Should `ShelfStore` be split into smaller, more focused modules?**
  _Cohesion score 0.1310344827586207 - nodes in this community are weakly interconnected._
- **Why does `NotesView` connect `NotesView` to `FrameworkElement`, `TimersModule`, `FrameworkElement`, `Logger.Error`, `IDisposable`, `ControlView`, `Strings.T`, `IslandWindow`, `system_windows`, `ShelfView`, `DispatcherTimer`, `NotesStore`?**
  _High betweenness centrality (0.033) - this node is a cross-community bridge._
- **Should `SystemControls.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.08907563025210084 - nodes in this community are weakly interconnected._