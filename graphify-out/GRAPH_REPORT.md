# Graph Report - Island  (2026-11-19)

## Corpus Check
- 180 files · ~151,413 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 5 file(s) not represented in the graph (top: (none) 3, .flag 1, .manifest 1)

## Summary
- 3507 nodes · 10602 edges · 165 communities (122 shown, 43 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 1630 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b81a8df7`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Color
- Native
- IslandWindow
- AiModule
- system_windows
- ControlView
- Settings
- Theory
- SettingsView
- TimerBook
- Recorder
- WeatherReport
- DispatcherTimer
- Fact
- Action
- AiChatView
- OverlayWindow
- DateTimeOffset
- List
- NotesView
- MarkupWindow
- App.StartModules
- system_text
- AudioVolume
- Task
- IModule
- system_windows_media
- AgentBook
- CommandBarView
- HookInstaller
- LiveEventsModule.Create
- AiTests
- ClipboardOps.PasteDataAsync
- Logger.Warn
- PreserveSig
- PillItem
- LyricsService.cs
- Logger.Error
- VoiceModule
- Window
- ToolsModule
- Strings.T
- AgentsModule
- ReminderBook
- CreatureDesign
- RepeatRule
- ForegroundWatcher
- MediaState
- ReminderParser
- Sources.cs
- CharactersModule
- NotesStore
- ShelfStore
- CommandModule.Create
- TimersSnapshots.Register
- Func
- AgentsView
- LyricsTracker
- TimersController
- CreatureBrain
- Guid
- FrameworkElement
- RatesSnapshot
- IDisposable
- Isle.csproj
- TextAction
- Isle.Core
- IntPtr
- JsonObject
- VoiceTests
- Isle.Tests
- Modules (`src/Isle/Modules`)
- TimersView
- DownloadsSource
- Attachments
- App
- TabBar
- Markdown.Parse
- TempDir
- CreatureView
- ShelfView
- MarkdownView
- ClipStoreTests
- 4. Features (all of them)
- Regex
- Parser
- ClipEntry
- MarkupWindow.cs
- ClipboardService
- Gesture
- MusicView
- AiPopupView
- Icon
- AiException
- ScreenInfo
- ITabView.OnHidden
- IndicatorPill
- NotesSnapshots.Register
- Units
- TimeSpan
- ChatAttachment
- DeviceState
- MicCameraSource
- ClipStore
- TimersModule
- IMMNotificationClient
- Lrc.Parse
- Note
- ResourceDictionary
- IdleHttp
- Memory.TrimSoon
- HotkeyService
- ClipboardView
- IVpnEnvironment
- DurationParser
- Button
- IdlePillView
- Mood
- VpnDetector.Detect
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- Node
- ToolsTests
- MicCapture
- ScreenGrab.WindowRects
- DECISIONS
- Strings
- Strings.cs
- TrayModule
- ArraySampleProvider
- Spring
- EDataFlow
- CoreAudio.cs
- WhisperModel
- WeatherKind
- VpnDetector
- Radios
- Paths.File
- LiveMiscTests
- PROGRESS
- CurrencyParser.Parse
- Fuzzy.Score
- Steam.Libraries
- RepeatKind
- Tool
- Steam.FromManifest
- Isle
- ClipSnapshots.Register
- PlaybackMath.Choose
- Low
- Secret
- LyricsStatus
- SOFTWARE-AUDIT.md
- High
- EventBusTests.Publish_reaches_subscribers_until_disposed_and_survives_throwing_handlers
- PasteResult
- INPUT
- KnownFolders.SHGetKnownFolderPath
- RECT
- CLAUDE.md
- Icons.xaml

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 155 edges
2. `Modules (`src/Isle/Modules`)` - 135 edges
3. `Isle.Core` - 109 edges
4. `Native` - 90 edges
5. `AiChatView` - 55 edges
6. `Recorder` - 52 edges
7. `PillItem` - 51 edges
8. `SettingsView` - 48 edges
9. `NotesView` - 47 edges
10. `AiModule` - 46 edges

## Surprising Connections (you probably didn't know these)
- `M6. Streaming AI answers are O(n²) in string concatenation and re-render the whole message` --references--> `ChatMessage`  [INFERRED]
  docs/SOFTWARE-AUDIT.md → src/Isle/Modules/AiChat/AiModels.cs
- `4.6 Control Center tab` --references--> `IAudioEndpointVolume`  [INFERRED]
  docs/SPEC.md → src/Isle/Modules/ControlCenter/CoreAudio.cs
- `M1` --references--> `Icon`  [INFERRED]
  docs/DECISIONS.md → src/Isle/Views/Icon.cs
- `Modules (`src/Isle/Modules`)` --references--> `HookProtocol`  [INFERRED]
  docs/ARCHITECTURE.md → src/Isle.Hook/HookProtocol.cs
- `M6` --references--> `ForegroundWatcher`  [INFERRED]
  docs/DECISIONS.md → src/Isle/Core/ForegroundWatcher.cs

## Import Cycles
- None detected.

## Communities (165 total, 43 thin omitted)

### Community 0 - "Color"
Cohesion: 0.06
Nodes (31): C1. Global bare-Esc hotkey while expanded — FIXED 3a90695, C2. Hover logic re-expands under a resting cursor and ignores mouse buttons and drags — FIXED 76ee049, C3. Contrast system: white on accent, faint text, light-theme controls — FIXED 9521ebb, Critical, H1. Clipboard borrow/paste-back loses data and pastes blindly, H2. Agent permission prompt: the command you approve can be truncated — FIXED e995dfa, H3. Timer/reminder is invisible when a fullscreen app is in front — FIXED 76ee049, H4. Hotkey defaults collide and failures are silent — FIXED 9521ebb (+23 more)

### Community 1 - "Native"
Cohesion: 0.08
Nodes (6): Native, EnumWindowsProc, LASTINPUTINFO, MonitorEnumProc, POINT, WinEventDelegate

### Community 2 - "IslandWindow"
Cohesion: 0.07
Nodes (12): IslandWindow, CurrentTab, TabList, FileDropTarget, IsHiddenByUser, PasteTarget, IsExpanded, Handle (+4 more)

### Community 3 - "AiModule"
Cohesion: 0.08
Nodes (25): ChatMessage, Attachments, Error, Note, Role, Stopped, Text, Conversation (+17 more)

### Community 4 - "system_windows"
Cohesion: 0.10
Nodes (9): Isle.Views.SettingsPanel, Isle.Modules.Characters, Isle, Isle.Modules.AiChat, Isle.Views, Isle.Modules.Hotkeys, Isle.Views.AiChat, Isle.Views.Characters (+1 more)

### Community 5 - "ControlView"
Cohesion: 0.09
Nodes (10): BluetoothDevices, BtDevice, ControlSnapshots, Brightness, Level, Supported, VpnEntry, CanToggle (+2 more)

### Community 6 - "Settings"
Cohesion: 0.08
Nodes (23): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+15 more)

### Community 7 - "Theory"
Cohesion: 0.08
Nodes (7): LanguagePick, CalculatorTests, UnitTests, DownloadDetectorTests, ObsRetryTests, PlaybackTests, ExcludedAppTests

### Community 8 - "SettingsView"
Cohesion: 0.11
Nodes (7): Settings panel (`Views/SettingsPanel`), HotkeyInfo, SettingsSnapshots, SettingsActions, SettingsView, S, Section

### Community 9 - "TimerBook"
Cohesion: 0.07
Nodes (26): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+18 more)

### Community 10 - "Recorder"
Cohesion: 0.06
Nodes (6): M3. `CancellationTokenSource` / `MMDevice` / wait-handle leaks on repeated use, Recorder, Elapsed, FilePath, IDirect3DDxgiInterfaceAccess, RegionDesc

### Community 11 - "WeatherReport"
Cohesion: 0.09
Nodes (24): CurrentWeather, Code, Feels, Humidity, IsDay, Precip, Temp, Time (+16 more)

### Community 12 - "DispatcherTimer"
Cohesion: 0.09
Nodes (14): MediaModule, Id, IsPlaying, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPlaying (+6 more)

### Community 13 - "Fact"
Cohesion: 0.16
Nodes (4): AgentPipeServer, AgentTests, StringsTests, TrayIconTests

### Community 14 - "Action"
Cohesion: 0.15
Nodes (6): TabInfo, CommandSnapshots, Entry, ToolsSnapshots, WeatherSnapshots, AgentsActions

### Community 16 - "OverlayWindow"
Cohesion: 0.10
Nodes (9): PxRect, IsEmpty, Selection, SelectionSession, OverlayWindow, Monitor, SelectMode, Color (+1 more)

### Community 17 - "DateTimeOffset"
Cohesion: 0.12
Nodes (9): LyricsModule, Id, Service, ILyricsHttp, LyricsResult, LyricsService, DiskEntry, LyricsServiceTests (+1 more)

### Community 18 - "List"
Cohesion: 0.13
Nodes (7): Place, IWeatherSource, OpenMeteoSource, WeatherService, LastFailed, WeatherActions, FakeSource

### Community 20 - "MarkupWindow"
Cohesion: 0.13
Nodes (6): Thumb, MarkupWindow, Stroke, PickButton, IsChecked, Ellipse

### Community 21 - "App.StartModules"
Cohesion: 0.08
Nodes (9): CommandModule, Id, ControlModule, Id, NotesModule, Id, Store, ShelfModule (+1 more)

### Community 22 - "system_text"
Cohesion: 0.10
Nodes (3): Isle.Agents, Isle.Views.Agents, Isle.Modules.Agents

### Community 23 - "AudioVolume"
Cohesion: 0.08
Nodes (14): M5, M7. Core Audio callbacks are marshalled onto the UI thread, AudioVolume, Available, Level, Muted, VolumeCallback, ControlServices (+6 more)

### Community 25 - "IModule"
Cohesion: 0.12
Nodes (12): IModule, Id, ModuleHost, Entry, Ids, ModuleState, Failed, Running (+4 more)

### Community 26 - "system_windows_media"
Cohesion: 0.12
Nodes (7): Isle.Modules.Clipboard, Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media, Isle.Views.Shelf, Isle.Modules.Shelf, Isle.Views.Clipboard

### Community 27 - "AgentBook"
Cohesion: 0.10
Nodes (11): C3. Permission prompts are approved from a truncated, sometimes wrong, summary — FIXED e995dfa, Critical, AgentBook, AllPending, Count, Sessions, Urgent, AgentQuestion (+3 more)

### Community 28 - "CommandBarView"
Cohesion: 0.13
Nodes (17): CommandItem, Confirm, Confirmation, IconKey, IconPath, Run, Score, Subtitle (+9 more)

### Community 29 - "HookInstaller"
Cohesion: 0.14
Nodes (6): HookInstaller, Entry, InstalledExe, HookPlan, HookTarget, Detected

### Community 30 - "LiveEventsModule.Create"
Cohesion: 0.11
Nodes (8): C1. Global low-level keyboard hook shares a thread with blocking work — FIXED 2fcdb2a, CapsLockSource, ILiveSource, KeyboardLayoutSource, NotificationsSource, Unavailable, SteamSource, VpnIndicatorSource

### Community 31 - "AiTests"
Cohesion: 0.19
Nodes (12): Dependency weight, Efficiency: measured-by-reading, Ranked cuts, Rendering model, AiConfig, IsReady, IChatProvider, AnthropicProvider (+4 more)

### Community 32 - "ClipboardOps.PasteDataAsync"
Cohesion: 0.14
Nodes (4): ClipboardOps, IsQuiet, InputUnion, KEYBDINPUT

### Community 33 - "Logger.Warn"
Cohesion: 0.09
Nodes (19): M10. `AppIndex` rebuilds a full filesystem walk on (almost) every command-bar open, M1. The media session is matched by app id, so a restarted player stops updating — FIXED 76ee049, M2. `Dispatcher.Invoke` (blocking) from the OBS WebSocket read loop — FIXED 76ee049, M4. `WriteAtomic` is rename-atomic but not crash-durable, and has no retry — FIXED 76ee049, M5. Fullscreen housekeeping re-enumerates every monitor every 2 seconds — FIXED 76ee049, M6. Streaming AI answers are O(n²) in string concatenation and re-render the whole message, Medium, JsonStore (+11 more)

### Community 34 - "PreserveSig"
Cohesion: 0.12
Nodes (3): IConnector, IDeviceTopology, IPart

### Community 35 - "PillItem"
Cohesion: 0.11
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 36 - "LyricsService.cs"
Cohesion: 0.10
Nodes (7): Isle.Views.Live, Isle.Views.Voice, Isle.Modules.Weather, Isle.Modules.Commands, Isle.Views.Weather, Isle.Modules.Voice, Isle.Views.Commands

### Community 37 - "Logger.Error"
Cohesion: 0.13
Nodes (6): M8. `DispatcherUnhandledException` swallows everything and can self-amplify, Autostart, IsEnabled, Logger, MediaSession, Current

### Community 38 - "VoiceModule"
Cohesion: 0.11
Nodes (9): AudioMath, VoiceModule, Id, Phase, Idle, Recording, Transcribing, VoiceSnapshots (+1 more)

### Community 39 - "Window"
Cohesion: 0.09
Nodes (30): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+22 more)

### Community 40 - "ToolsModule"
Cohesion: 0.10
Nodes (5): OcrReader, RecordOptions, ToolsModule, Id, View

### Community 41 - "Strings.T"
Cohesion: 0.17
Nodes (3): WeatherCodes, WeatherFormat, WeatherView

### Community 42 - "AgentsModule"
Cohesion: 0.11
Nodes (13): AgentRequest, ApprovalInput, Id, IsQuestion, Questions, Reply, Session, Tool (+5 more)

### Community 43 - "ReminderBook"
Cohesion: 0.12
Nodes (16): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+8 more)

### Community 44 - "CreatureDesign"
Cohesion: 0.08
Nodes (18): Selected, CreatureDesign, Body, BodyBrush, CheekBrush, CheekDx, CheekY, Extra (+10 more)

### Community 45 - "RepeatRule"
Cohesion: 0.13
Nodes (8): RepeatRule, DayOfMonth, Days, Interval, Kind, ParsedReminder, ReminderParserTests, RepeatRuleTests

### Community 47 - "MediaState"
Cohesion: 0.09
Nodes (18): MediaSnapshots, MediaState, Accent, Album, Art, Artist, CanNext, CanPrev (+10 more)

### Community 48 - "ReminderParser"
Cohesion: 0.15
Nodes (5): ReminderParser, Rule, State, HasDate, Repeats

### Community 49 - "Sources.cs"
Cohesion: 0.11
Nodes (3): Isle.Modules.ControlCenter, Isle.Modules.LiveEvents, Isle.Views.Control

### Community 50 - "CharactersModule"
Cohesion: 0.12
Nodes (9): TimeoutSeconds, CharactersModule, Brain, Id, InPill, S, SleepMinutes, CharacterGallery (+1 more)

### Community 51 - "NotesStore"
Cohesion: 0.22
Nodes (6): NotesStore, Doc, Folders, HasUnsavedChanges, Notes, NotesStoreTests

### Community 52 - "ShelfStore"
Cohesion: 0.15
Nodes (11): ShelfFile, Items, Version, ShelfItem, Added, Id, Path, ShelfStore (+3 more)

### Community 53 - "CommandModule.Create"
Cohesion: 0.12
Nodes (8): M8, AppsProvider, AskAi, CurrencyProvider, SystemProvider, Cmd, TimerProvider, WebProvider

### Community 54 - "TimersSnapshots.Register"
Cohesion: 0.15
Nodes (6): M3. Ukrainian text and fixed widths — FIXED 9521ebb, TimersSnapshots, Ring, Thickness, TimerDoneBanner, TimerPill

### Community 55 - "Func"
Cohesion: 0.13
Nodes (7): M6, H1. `live` sources run unconditional periodic timers, including a full process enumeration — FIXED 76ee049, ILiveSink, Ui, ObsSource, FakeSink, Ui

### Community 56 - "AgentsView"
Cohesion: 0.19
Nodes (3): AgentsView, OverlayOpen, ScrollViewer

### Community 57 - "LyricsTracker"
Cohesion: 0.11
Nodes (11): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey, FakeMediaSource (+3 more)

### Community 58 - "TimersController"
Cohesion: 0.12
Nodes (12): TimersController, Clock, NextWake, Reminders, SubmitResult, Full, Invalid, Reminder (+4 more)

### Community 59 - "CreatureBrain"
Cohesion: 0.18
Nodes (8): M14, M3, CreatureBrain, MusicPlaying, SleepAfter, TimerFinished, CharacterTests, FakeClock

### Community 60 - "Guid"
Cohesion: 0.11
Nodes (3): M9, IAudioEndpointVolume, IGraphicsCaptureItemInterop

### Community 61 - "FrameworkElement"
Cohesion: 0.14
Nodes (16): AgentSession, Agent, Cwd, Key, LastAction, Pending, Project, Reported (+8 more)

### Community 62 - "RatesSnapshot"
Cohesion: 0.17
Nodes (9): CurrencyService, IRatesSource, NbuRatesSource, RatesSnapshot, Date, Fetched, Rates, CurrencyTests (+1 more)

### Community 63 - "IDisposable"
Cohesion: 0.13
Nodes (6): EventBus, Unsubscriber, LiveEventsModule, Id, PillSink, Ui

### Community 64 - "Isle.csproj"
Cohesion: 0.09
Nodes (17): net10.0, Microsoft.NET.Test.Sdk (17.14.1), NAudio.Wasapi (3.3.0), PdfPig (0.1.16), System.Management (11.0.0), System.Security.Cryptography.ProtectedData (11.0.0), Vortice.Direct3D11 (3.8.3), Whisper.net (1.9.1) (+9 more)

### Community 65 - "TextAction"
Cohesion: 0.13
Nodes (10): TextAction, Explain, Fix, Reply, Summarise, TranslateEn, TranslateNl, TranslateUk (+2 more)

### Community 66 - "Isle.Core"
Cohesion: 0.16
Nodes (6): Isle.Views.Timers, Isle.Core, Isle.Modules.Tray, Isle.Modules.Reminders, Isle.Modules.Timers, PillPriority

### Community 67 - "IntPtr"
Cohesion: 0.14
Nodes (6): M0, TrayIcon, ICONINFO, MOUSEINPUT, NOTIFYICONDATA, SHFILEINFO

### Community 68 - "JsonObject"
Cohesion: 0.16
Nodes (3): C2. The pipe name is predictable and can be claimed by any same-user process before Isle starts — FIXED 9e22d9b, HookProtocol, Endpoint

### Community 69 - "VoiceTests"
Cohesion: 0.20
Nodes (5): ModelDownloader, ChecksumException, VoiceTests, BytesHandler, SyncProgress

### Community 70 - "Isle.Tests"
Cohesion: 0.11
Nodes (4): Isle.Tests, Tests (`tests/Isle.Tests`), CapabilityUse, TestSetup

### Community 71 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.14
Nodes (9): Modules (`src/Isle/Modules`), WindowsTheme, IsDark, ObsAuth, VoiceSelfTest, ITranscriber, Transcript, WhisperEngine (+1 more)

### Community 73 - "DownloadsSource"
Cohesion: 0.21
Nodes (4): DownloadDetector, DownloadsSource, Item, Sampling

### Community 74 - "Attachments"
Cohesion: 0.16
Nodes (4): Attachments, DialogFilter, AttachmentTests, ClipboardBorrowTests

### Community 75 - "App"
Cohesion: 0.12
Nodes (9): ARCHITECTURE, Process shape, Shell, Styles / Views, Application, App, Host, Island (+1 more)

### Community 76 - "TabBar"
Cohesion: 0.15
Nodes (8): MarqueeText, Inner, Text, TabBar, AvailableWidth, Hidden, Mascot, SelectedId

### Community 77 - "Markdown.Parse"
Cohesion: 0.14
Nodes (9): Markdown, MdSpan, MdSpanKind, Bold, BoldItalic, Code, Italic, Text (+1 more)

### Community 78 - "TempDir"
Cohesion: 0.24
Nodes (4): Core (`src/Isle/Core`), SettingsTests, TempDir, Path

### Community 79 - "CreatureView"
Cohesion: 0.16
Nodes (4): StageShift, CreatureView, Design, TranslateTransform

### Community 81 - "MarkdownView"
Cohesion: 0.17
Nodes (10): MdBlock, MdBlockKind, Bullet, Code, Heading, Numbered, Paragraph, Quote (+2 more)

### Community 83 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (18): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+10 more)

### Community 86 - "Parser"
Cohesion: 0.27
Nodes (3): Parser, DidWork, Result

### Community 87 - "ClipEntry"
Cohesion: 0.11
Nodes (17): ClipEntry, Cipher, Id, Image, ImageHash, ImageHeight, ImageWidth, Kind (+9 more)

### Community 89 - "ClipboardService"
Cohesion: 0.18
Nodes (3): ClipboardModule, Id, ClipboardService

### Community 90 - "Gesture"
Cohesion: 0.20
Nodes (4): Gesture, IsValid, SettingsPanelTests, GestureTests

### Community 93 - "Icon"
Cohesion: 0.21
Nodes (9): PropVariant, Icon, Data, Filled, Foreground, Stroke, Brush, Progress (+1 more)

### Community 94 - "AiException"
Cohesion: 0.23
Nodes (3): AiException, Detail, Key

### Community 95 - "ScreenInfo"
Cohesion: 0.22
Nodes (4): ScreenInfo, Screens, Screen, ScreenSelectionTests

### Community 98 - "NotesSnapshots.Register"
Cohesion: 0.17
Nodes (4): Isle.Views.Notes, Isle.Modules.Notes, NotesSnapshots, NotesFormattingTests

### Community 99 - "Units"
Cohesion: 0.19
Nodes (7): Units, Kind, Data, Length, Mass, Temp, U

### Community 100 - "TimeSpan"
Cohesion: 0.19
Nodes (3): SpeedMeter, BytesPerSecond, LrcLine

### Community 101 - "ChatAttachment"
Cohesion: 0.14
Nodes (13): AiProviderKind, Anthropic, OpenAi, AttachmentKind, Image, Pdf, Text, ChatAttachment (+5 more)

### Community 102 - "DeviceState"
Cohesion: 0.15
Nodes (8): DeviceState, Active, All, Disabled, NotPresent, Unplugged, IMMDevice, IMMDeviceCollection

### Community 104 - "ClipStore"
Cohesion: 0.27
Nodes (4): L5. Small unbounded growth, ClipStore, Entries, Limit

### Community 105 - "TimersModule"
Cohesion: 0.21
Nodes (3): TimersModule, Controller, Id

### Community 106 - "IMMNotificationClient"
Cohesion: 0.19
Nodes (4): DeviceCallback, IMMNotificationClient, IPropertyStore, PropertyKey

### Community 108 - "Note"
Cohesion: 0.14
Nodes (13): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+5 more)

### Community 109 - "ResourceDictionary"
Cohesion: 0.22
Nodes (12): ToolTip, Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary (+4 more)

### Community 110 - "IdleHttp"
Cohesion: 0.26
Nodes (4): M10, M11. `IdleHttp` can dispose an `HttpClient` out from under an in-flight request, IdleHttp, LrclibHttp

### Community 111 - "Memory.TrimSoon"
Cohesion: 0.18
Nodes (7): M11, How to work on this, Isle — fix plan from the two audits, Tier 1 — do first (security and system-wide effects), Tier 2 — daily use (annoyance, data loss, battery), Tier 3 — quality, after Tier 1 and 2 are committed, Memory

### Community 114 - "IVpnEnvironment"
Cohesion: 0.18
Nodes (9): EmptyVpnEnvironment, LocalAppData, ProgramFiles, IVpnEnvironment, LocalAppData, ProgramFiles, MachineVpnEnvironment, LocalAppData (+1 more)

### Community 117 - "IdlePillView"
Cohesion: 0.29
Nodes (6): IdleView, IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 118 - "Mood"
Cohesion: 0.24
Nodes (6): CharacterSnapshots, Mood, Awake, Dancing, Happy, Sleeping

### Community 119 - "VpnDetector.Detect"
Cohesion: 0.26
Nodes (4): VpnDetectorTests, FakeEnv, LocalAppData, ProgramFiles

### Community 120 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), 9. Definition of Done (+2 more)

### Community 125 - "DECISIONS"
Cohesion: 0.20
Nodes (9): DECISIONS, M1, M12, M13, M15, M4, M7, Performance measurements (+1 more)

### Community 126 - "Strings"
Cohesion: 0.20
Nodes (7): 2. Hard requirements (non-negotiable), Strings, English, Instance, Language, this[], Ukrainian

### Community 131 - "EDataFlow"
Cohesion: 0.24
Nodes (8): EDataFlow, All, Capture, Render, ERole, Communications, Console, Multimedia

### Community 132 - "CoreAudio.cs"
Cohesion: 0.22
Nodes (4): AudioVolumeNotificationData, CoreAudioIds, IKsControl, KsProperty

### Community 133 - "WhisperModel"
Cohesion: 0.22
Nodes (7): WhisperModel, IsDownloaded, SizeMb, Url, WhisperModels, All, Model

### Community 134 - "WeatherKind"
Cohesion: 0.20
Nodes (9): WeatherKind, Clear, Cloudy, Drizzle, Fog, PartlyCloudy, Rain, Snow (+1 more)

### Community 135 - "VpnDetector"
Cohesion: 0.22
Nodes (3): H6. VPN and system commands launch executables by bare name — FIXED 76ee049, VpnDetector, Client

### Community 136 - "Radios"
Cohesion: 0.28
Nodes (4): Radios, BluetoothOn, Denied, WifiOn

### Community 137 - "Paths.File"
Cohesion: 0.25
Nodes (5): Paths, LogFile, Root, SettingsFile, LocalPath

### Community 139 - "PROGRESS"
Cohesion: 0.25
Nodes (7): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, WPF audit repairs

### Community 143 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 144 - "Tool"
Cohesion: 0.25
Nodes (8): Tool, Arrow, Ellipse, Highlighter, Pen, Pixelate, Rect, Text

### Community 145 - "Steam.FromManifest"
Cohesion: 0.38
Nodes (5): M9. Steam manifests are read on the UI thread with a spin-retry, 4.8 Live events (sources for the pill), Steam, SteamDownload, Progress

### Community 147 - "Isle"
Cohesion: 0.29
Nodes (6): Build and run, Data and privacy, Debug flags, Hotkeys (all can be changed in Settings → Hotkeys), Isle, What it does

### Community 149 - "PlaybackMath.Choose"
Cohesion: 0.33
Nodes (4): M2, VERIFICATION, WPF audit repairs, PlaybackMath

### Community 150 - "Low"
Cohesion: 0.33
Nodes (6): L1. Whisper model integrity is verified at download but not at load, L2. The hook helper lives in a user-writable directory and is invoked by Claude Code constantly, L3. Notes are stored in plaintext while clipboard text is encrypted, L4. `Settings.Current` lazy init is not thread-safe; `SetWindowLongPtr` has no `SetLastError`, L6. `OnDragOver` re-selects the tab on every drag-move event, Low

### Community 153 - "LyricsStatus"
Cohesion: 0.33
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 154 - "SOFTWARE-AUDIT.md"
Cohesion: 0.40
Nodes (4): If you only fix five things, Isle — software audit, Sources cited, Summary

### Community 155 - "High"
Cohesion: 0.40
Nodes (5): H2. One abandoned `*.crdownload` keeps a 1 Hz file poll alive for the life of the process — FIXED 76ee049, H3. A blocking, compacting gen2 GC + `EmptyWorkingSet` after every collapse — FIXED 76ee049, H4. Clipboard capture does megabyte-scale work on the UI thread for every copy on the machine, H5. With `chars.inPill` on, mouse movement anywhere drives a 30 fps software repaint all day, High

### Community 157 - "PasteResult"
Cohesion: 0.40
Nodes (5): PasteResult, Changed, Manual, Pasted, RestoreFailed

### Community 160 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **462 isolated node(s):** `net10.0`, `Microsoft.NET.Sdk`, `Host`, `Island`, `IsSnapshot` (+457 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 729 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **43 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `Color`, `IslandWindow`, `AiModule`, `ControlView`, `Settings`, `Theory`, `SettingsView`, `TimerBook`, `Recorder`, `WeatherReport`, `DispatcherTimer`, `Fact`, `AiChatView`, `DateTimeOffset`, `List`, `App.StartModules`, `AudioVolume`, `Task`, `AgentBook`, `CommandBarView`, `HookInstaller`, `LiveEventsModule.Create`, `AiTests`, `ClipboardOps.PasteDataAsync`, `Logger.Warn`, `Logger.Error`, `VoiceModule`, `ToolsModule`, `Strings.T`, `AgentsModule`, `ReminderBook`, `CreatureDesign`, `RepeatRule`, `MediaState`, `ReminderParser`, `CharactersModule`, `NotesStore`, `ShelfStore`, `CommandModule.Create`, `TimersSnapshots.Register`, `Func`, `LyricsTracker`, `TimersController`, `CreatureBrain`, `FrameworkElement`, `RatesSnapshot`, `IDisposable`, `TextAction`, `IntPtr`, `JsonObject`, `VoiceTests`, `DownloadsSource`, `Attachments`, `App`, `Markdown.Parse`, `MarkdownView`, `Regex`, `ClipboardService`, `AiPopupView`, `Units`, `TimeSpan`, `ChatAttachment`, `MicCameraSource`, `ClipStore`, `TimersModule`, `Lrc.Parse`, `HotkeyService`, `DurationParser`, `Node`, `ToolsTests`, `MicCapture`, `WhisperModel`, `VpnDetector`, `Radios`, `LiveMiscTests`, `CurrencyParser.Parse`, `Fuzzy.Score`, `Steam.FromManifest`, `PlaybackMath.Choose`?**
  _High betweenness centrality (0.183) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `IslandWindow` (e.g. with `Process shape` and `App.OnStartup()`) actually correct?**
  _`IslandWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `Microsoft.NET.Sdk`, `Host` to the rest of the system?**
  _462 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Color` be split into smaller, more focused modules?**
  _Cohesion score 0.05649717514124294 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `Spring`, `AiModule`, `system_windows`, `Settings`, `Theory`, `DispatcherTimer`, `Action`, `List`, `IslandWindow.OnHoverTick`, `ClipSnapshots.Register`, `App.StartModules`, `NotesView`, `IReadOnlyList`, `AgentBook`, `CommandBarView`, `LiveEventsModule.Create`, `ClipboardOps.PasteDataAsync`, `PillItem`, `VoiceModule`, `Window`, `ToolsModule`, `AgentsModule`, `ForegroundWatcher`, `MediaState`, `CharactersModule`, `TimersSnapshots.Register`, `FrameworkElement`, `IDisposable`, `IntPtr`, `App`, `ShelfView`, `ClipboardService`, `ScreenInfo`, `ITabView.OnHidden`, `IndicatorPill`, `NotesSnapshots.Register`, `TimeSpan`, `TimersModule`, `HotkeyService`, `IdlePillView`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **Are the 134 inferred relationships involving `Modules (`src/Isle/Modules`)` (e.g. with `ClipboardOps.CopySelectionAsync()` and `ClipboardOps.PasteTextAsync()`) actually correct?**
  _`Modules (`src/Isle/Modules`)` has 134 INFERRED edges - model-reasoned connections that need verification._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.07570621468926554 - nodes in this community are weakly interconnected._