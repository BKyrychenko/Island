# Graph Report - island  (2026-11-14)

## Corpus Check
- 72 files · ~43,937 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 1182 nodes · 2892 edges · 60 communities (35 shown, 25 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 346 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `33c36590`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Fact
- system_windows_threading
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- IntPtr
- TimersView
- EventBus
- MediaSession
- Isle.Tests.csproj
- Lrc.Parse
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
- TimersSnapshots.Register
- ForegroundWatcher
- Modules (`src/Isle/Modules`)
- system_windows
- App.StartModules
- MusicView.cs
- TimerPill.cs
- Logger.Error
- ReminderParser.Parse
- Screens.All
- App
- Isle.Core
- RepeatRule.Next
- TrayModule
- IslandWindow.ShowTab
- RepeatRule
- RepeatKind
- Paths
- Autostart
- RECT
- WinEventDelegate

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 103 edges
2. `Native` - 43 edges
3. `Isle.Core` - 40 edges
4. `TimersController` - 40 edges
5. `MusicView` - 37 edges
6. `MediaState` - 37 edges
7. `MediaSession` - 33 edges
8. `TimerBook` - 32 edges
9. `LyricsService` - 32 edges
10. `PillItem` - 31 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Process shape` --references--> `IslandWindow`  [INFERRED]
  ARCHITECTURE.md → src/Isle/IslandWindow.Content.cs
- `Modules (`src/Isle/Modules`)` --references--> `RepeatRule`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Reminders/Reminder.cs
- `Modules (`src/Isle/Modules`)` --references--> `ReminderBook`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Reminders/Reminder.cs

## Import Cycles
- None detected.

## Communities (60 total, 25 thin omitted)

### Community 0 - "Native"
Cohesion: 0.14
Nodes (3): MonitorEnumProc, Native, POINT

### Community 1 - "Fact"
Cohesion: 0.06
Nodes (26): Core (`src/Isle/Core`), Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys (+18 more)

### Community 2 - "system_windows_threading"
Cohesion: 0.14
Nodes (3): Isle, Isle.Modules.Hotkeys, Isle.Modules.Tray

### Community 3 - "ModuleHost"
Cohesion: 0.09
Nodes (17): ARCHITECTURE, Process shape, Styles / Views, Tests (`tests/Isle.Tests`), Entry, IModule, Id, ModuleHost (+9 more)

### Community 4 - "IslandWindow"
Cohesion: 0.10
Nodes (11): IslandWindow, CurrentTab, IdleView, TabList, IsHiddenByUser, PasteTarget, IsExpanded, Handle (+3 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.04
Nodes (37): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+29 more)

### Community 6 - "IntPtr"
Cohesion: 0.17
Nodes (4): M0, TrayIcon, ICONINFO, NOTIFYICONDATA

### Community 8 - "EventBus"
Cohesion: 0.08
Nodes (13): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, EventBus, Unsubscriber (+5 more)

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.18
Nodes (7): Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0-windows10.0.19041.0, Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "Lrc.Parse"
Cohesion: 0.06
Nodes (12): Gesture, IsValid, Lrc, LrcLine, PlaybackMath, DurationParser, LrcTests, PlaybackTests (+4 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 17 - "TimerBook"
Cohesion: 0.10
Nodes (16): StopwatchState, Accumulated, Active, IsRunning, RunningSince, TimerBook, Doc, Finished (+8 more)

### Community 18 - "PillItem"
Cohesion: 0.12
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, HoverExpands, Id, IsBanner (+10 more)

### Community 19 - "MusicView"
Cohesion: 0.05
Nodes (19): DECISIONS, M1, M2, Performance measurements, TabInfo, Icon, Data, Filled (+11 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "TimersController"
Cohesion: 0.12
Nodes (11): SubmitResult, Full, Invalid, Reminder, Timer, TimersController, Clock, NextWake (+3 more)

### Community 22 - "TimerItem"
Cohesion: 0.12
Nodes (9): TimerItem, Duration, EndsAt, FinishedAt, Id, IsPaused, IsRunning, Label (+1 more)

### Community 23 - "ReminderParser"
Cohesion: 0.18
Nodes (3): ReminderParser, Rule, Repeats

### Community 26 - "DateTimeOffset"
Cohesion: 0.06
Nodes (21): LyricsModule, Id, Service, LyricsTracker, ForCurrent, Loading, Result, TrackKey (+13 more)

### Community 27 - "ReminderBook"
Cohesion: 0.14
Nodes (15): Reminder, Due, FiredAt, Id, Repeat, Source, Text, ReminderBook (+7 more)

### Community 28 - "MediaModule"
Cohesion: 0.06
Nodes (19): Memory, MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source, MediaSnapshots (+11 more)

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.16
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 32 - "TimersSnapshots.Register"
Cohesion: 0.19
Nodes (8): TimersSnapshots, Ring, Brush, Progress, Thickness, Track, TimerDoneBanner, TimerPill

### Community 35 - "Modules (`src/Isle/Modules`)"
Cohesion: 0.20
Nodes (7): Modules (`src/Isle/Modules`), M3, TimerFinished, TimersModule, Controller, Id, ReminderBanner

### Community 39 - "MusicView.cs"
Cohesion: 0.29
Nodes (3): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Modules.Media

### Community 40 - "TimerPill.cs"
Cohesion: 0.22
Nodes (6): Isle.Views.Timers, Isle.Tests, Isle.Modules.Reminders, Isle.Modules.Timers, ReminderBookTests, TimerBookTests

### Community 41 - "Logger.Error"
Cohesion: 0.23
Nodes (3): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), JsonStore, Logger

### Community 43 - "Screens.All"
Cohesion: 0.23
Nodes (4): ScreenInfo, Screens, Screen, MONITORINFOEX

### Community 44 - "App"
Cohesion: 0.15
Nodes (5): Application, App, Host, Island, IsSnapshot

### Community 50 - "RepeatRule"
Cohesion: 0.25
Nodes (7): RepeatRule, DayOfMonth, Days, Interval, Kind, State, HasDate

### Community 51 - "RepeatKind"
Cohesion: 0.25
Nodes (8): RepeatKind, Daily, EveryNDays, EveryNHours, EveryNMinutes, Monthly, None, Weekly

### Community 54 - "Paths"
Cohesion: 0.33
Nodes (4): Paths, LogFile, Root, SettingsFile

### Community 58 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **188 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `Id`, `Priority` (+183 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 325 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **25 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IslandWindow` connect `IslandWindow` to `Native`, `system_windows_threading`, `ModuleHost`, `IntPtr`, `TimersView`, `EventBus`, `Lrc.Parse`, `Window`, `TimerBook`, `PillItem`, `MediaModule`, `App.OnStartup`, `TimersSnapshots.Register`, `ForegroundWatcher`, `Modules (`src/Isle/Modules`)`, `system_windows`, `App.StartModules`, `MusicView.cs`, `Screens.All`, `App`, `IslandWindow.Expand`, `IslandWindow.ShowTab`, `EventArgs`, `IslandWindow.ApplyShape`, `IslandWindow.HideMoreOverlay`?**
  _High betweenness centrality (0.256) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `IslandWindow` (e.g. with `Process shape` and `App.OnStartup()`) actually correct?**
  _`IslandWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _188 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.14408602150537633 - nodes in this community are weakly interconnected._
- **Why does `Modules (`src/Isle/Modules`)` connect `Modules (`src/Isle/Modules`)` to `TimersSnapshots.Register`, `ModuleHost`, `IntPtr`, `EventBus`, `MediaSession`, `Lrc.Parse`, `Color`, `TimerBook`, `RepeatRule`, `TimersController`, `ReminderParser`, `DateTimeOffset`, `ReminderBook`, `MediaModule`, `FakeMediaSource`?**
  _High betweenness centrality (0.094) - this node is a cross-community bridge._
- **Should `Fact` be split into smaller, more focused modules?**
  _Cohesion score 0.05920745920745921 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Fact`, `system_windows_threading`, `ModuleHost`, `system_windows`, `LyricsService.cs`, `4. Features (all of them)`, `MusicView.cs`, `EventBus`, `TimerPill.cs`, `Screens.All`, `MediaSession.cs`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._