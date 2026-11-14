# Graph Report - island  (2026-11-14)

## Corpus Check
- 77 files · ~48,722 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 1290 nodes · 3245 edges · 70 communities (48 shown, 22 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 436 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8dcb8b31`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Fact
- Memory.cs
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- TrayIcon.Create
- TimersView
- Action
- Logger.Error
- Isle.Tests.csproj
- HotkeyService
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
- DateTimeOffset
- ReminderBook
- MediaModule
- MediaState
- FakeMediaSource
- NotesView
- TimersSnapshots.Register
- ForegroundWatcher
- Modules (`src/Isle/Modules`)
- Modules (`src/Isle/Modules`)
- system_windows
- App.StartModules
- MusicView.cs
- TimerPill.cs
- TabBar
- ReminderParser.Parse
- Screens.All
- App
- IslandWindow
- Isle.Core
- RepeatRule.Next
- TrayModule
- Note
- LyricsTracker
- RepeatKind
- Func
- Equalizer
- Paths
- MediaModule.Start
- ARCHITECTURE
- Strings.cs
- RECT
- WinEventDelegate
- Reminder
- Grid
- Strings
- DECISIONS
- LyricsModule
- PROGRESS
- Memory
- MonitorEnumProc

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 64 edges
2. `NotesView` - 47 edges
3. `Isle.Core` - 44 edges
4. `Native` - 44 edges
5. `IslandWindow` - 42 edges
6. `TimersController` - 40 edges
7. `MusicView` - 37 edges
8. `MediaState` - 37 edges
9. `Modules (`src/Isle/Modules`)` - 35 edges
10. `NotesStore` - 33 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Modules (`src/Isle/Modules`)` --references--> `NotesModule`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Notes/NotesModule.cs
- `Modules (`src/Isle/Modules`)` --references--> `HotkeyService`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Hotkeys/HotkeyService.cs

## Import Cycles
- None detected.

## Communities (70 total, 22 thin omitted)

### Community 0 - "Native"
Cohesion: 0.16
Nodes (3): Native, NOTIFYICONDATA, POINT

### Community 1 - "Fact"
Cohesion: 0.06
Nodes (25): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+17 more)

### Community 3 - "ModuleHost"
Cohesion: 0.22
Nodes (6): Process shape, ModuleHost, Ids, Fake, Id, ModuleHostTests

### Community 4 - "IslandWindow"
Cohesion: 0.08
Nodes (11): ITabView, IslandWindow, CurrentTab, IdleView, TabList, IsExpanded, IdlePillView, DesiredPillWidth (+3 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.07
Nodes (29): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+21 more)

### Community 8 - "Action"
Cohesion: 0.16
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 9 - "Logger.Error"
Cohesion: 0.08
Nodes (9): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Autostart, IsEnabled, JsonStore, Logger, MediaSession, Current, PlaybackMath (+1 more)

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.18
Nodes (7): Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0-windows10.0.19041.0, Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "HotkeyService"
Cohesion: 0.09
Nodes (9): Entry, Gesture, IsValid, HotkeyInfo, HotkeyService, PlaybackTests, ExcludedAppTests, GestureTests (+1 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerBook"
Cohesion: 0.11
Nodes (17): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+9 more)

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimersController"
Cohesion: 0.11
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 22 - "TimerItem"
Cohesion: 0.19
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.18
Nodes (4): ReminderParser, Rule, State, HasDate

### Community 26 - "DateTimeOffset"
Cohesion: 0.06
Nodes (15): Lrc, LrcLine, DiskEntry, ILyricsHttp, LrclibHttp, LyricsResult, LyricsService, LyricsStatus (+7 more)

### Community 27 - "ReminderBook"
Cohesion: 0.16
Nodes (7): ReminderBook, Doc, NextDue, Reminders, Showing, Upcoming, ReminderBookTests

### Community 28 - "MediaModule"
Cohesion: 0.15
Nodes (8): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MusicPill, PillWidth

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.20
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.14
Nodes (8): TimersSnapshots, ReminderBanner, Ring, Brush, Progress, Track, TimerDoneBanner, TimerPill

### Community 34 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.19
Nodes (7): Modules (`src/Isle/Modules`), NotesSnapshots, NotesStore, Doc, Folders, Notes, NotesStoreTests

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.31
Nodes (3): TimersModule, Controller, Id

### Community 38 - "App.StartModules"
Cohesion: 0.21
Nodes (3): NotesModule, Id, Store

### Community 39 - "MusicView.cs"
Cohesion: 0.21
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 40 - "TimerPill.cs"
Cohesion: 0.42
Nodes (3): Isle.Views.Timers, Isle.Modules.Reminders, Isle.Modules.Timers

### Community 41 - "TabBar"
Cohesion: 0.11
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 43 - "Screens.All"
Cohesion: 0.29
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.12
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 45 - "IslandWindow"
Cohesion: 0.08
Nodes (8): Shell, IslandWindow, IsHiddenByUser, PasteTarget, Handle, Hotkeys, Pill, Watcher

### Community 46 - "Isle.Core"
Cohesion: 0.12
Nodes (6): Isle.Views.Notes, Isle.Core, Isle.Modules.Notes, Isle.Modules.Hotkeys, Isle.Tests, PillPriority

### Community 47 - "RepeatRule.Next"
Cohesion: 0.20
Nodes (7): RepeatRule, DayOfMonth, Days, Interval, Kind, Repeats, RepeatRuleTests

### Community 49 - "Note"
Cohesion: 0.12
Nodes (14): Note, Created, Folder, Id, Pinned, Plain, Preview, Title (+6 more)

### Community 50 - "LyricsTracker"
Cohesion: 0.16
Nodes (7): LyricsTracker, ForCurrent, Loading, Result, TrackKey, TrackQuery, CacheKey

### Community 51 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 52 - "Func"
Cohesion: 0.17
Nodes (7): Entry, IModule, Id, ModuleState, Failed, Running, Stopped

### Community 53 - "Equalizer"
Cohesion: 0.23
Nodes (3): ArtImage, Equalizer, Playing

### Community 54 - "Paths"
Cohesion: 0.33
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 56 - "ARCHITECTURE"
Cohesion: 0.20
Nodes (6): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), M0, TestSetup

### Community 58 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

### Community 60 - "Reminder"
Cohesion: 0.22
Nodes (9): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderDoc (+1 more)

### Community 61 - "Grid"
Cohesion: 0.31
Nodes (4): MarqueeText, Inner, Text, TrackBanner

### Community 62 - "Strings"
Cohesion: 0.22
Nodes (6): Strings, English, Instance, Language, this[], Ukrainian

### Community 63 - "DECISIONS"
Cohesion: 0.25
Nodes (7): DECISIONS, M1, M2, M3, M4, Performance measurements, TimerFinished

### Community 64 - "LyricsModule"
Cohesion: 0.29
Nodes (3): LyricsModule, Id, Service

### Community 65 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

## Knowledge Gaps
- **203 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `PasteTarget`, `IsHiddenByUser` (+198 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 349 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **22 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `TrayIcon.Create`, `Logger.Error`, `HotkeyService`, `Color`, `TimerBook`, `TimersController`, `ReminderParser`, `DateTimeOffset`, `ReminderBook`, `MediaModule`, `FakeMediaSource`, `TimersSnapshots.Register`, `Modules (`src/Isle/Modules`)`, `App.StartModules`, `RepeatRule.Next`, `LyricsTracker`, `MediaModule.Start`, `ARCHITECTURE`, `DECISIONS`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `NotesView` (e.g. with `NotesModule.Start()` and `NotesSnapshots.Tab()`) actually correct?**
  _`NotesView` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _203 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Fact` be split into smaller, more focused modules?**
  _Cohesion score 0.06292966684294024 - nodes in this community are weakly interconnected._
- **Why does `IslandWindow` connect `IslandWindow` to `Native`, `ForegroundWatcher`, `Modules (`src/Isle/Modules`)`, `IslandWindow`, `system_windows`, `App.StartModules`, `MusicView.cs`, `Action`, `TimersView`, `HotkeyService`, `App`, `Screens.All`, `Window`, `TrayModule`, `MediaModule.Start`, `MediaModule`, `NotesView`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._
- **Should `IslandWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.07729468599033816 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Fact`, `Memory.cs`, `App.xaml.cs`, `system_windows`, `LyricsService.cs`, `MusicView.cs`, `Action`, `TimerPill.cs`, `Screens.All`, `Func`, `Strings.cs`, `Strings`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._