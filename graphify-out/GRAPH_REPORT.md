# Graph Report - island  (2026-11-14)

## Corpus Check
- 62 files · ~32,848 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 920 nodes · 2104 edges · 40 communities (29 shown, 11 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 222 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3050ff35`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Settings
- Isle.Core
- ModuleHost
- IslandWindow
- 4. Features (all of them)
- ARCHITECTURE
- Strings
- EventBus
- Logger.Error
- Isle.Tests.csproj
- Lrc.Parse
- Color
- Isle
- Window
- MusicView
- PillItem
- TabBar
- ResourceDictionary
- Equalizer
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- Strings.cs
- CLAUDE.md
- Icons.xaml
- LyricsResult
- Fact
- MediaModule
- MediaState
- FakeMediaSource
- IslandWindow
- LyricsTracker
- LyricsService
- Task
- Spring
- MusicPill
- LyricsService.cs
- LyricsModule
- PROGRESS

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 86 edges
2. `Native` - 42 edges
3. `MediaState` - 37 edges
4. `MusicView` - 37 edges
5. `Isle.Core` - 35 edges
6. `MediaSession` - 33 edges
7. `LyricsService` - 32 edges
8. `Settings` - 30 edges
9. `MediaModule` - 28 edges
10. `PillItem` - 25 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Modules (`src/Isle/Modules`)` --references--> `Lrc`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Lyrics/Lrc.cs
- `Modules (`src/Isle/Modules`)` --references--> `LyricsTracker`  [INFERRED]
  ARCHITECTURE.md → src/Isle/Modules/Lyrics/LyricsModule.cs

## Import Cycles
- None detected.

## Communities (40 total, 11 thin omitted)

### Community 0 - "Native"
Cohesion: 0.06
Nodes (17): ForegroundWatcher, LastExternal, ScreenInfo, Screens, Screen, TrayModule, Id, ICONINFO (+9 more)

### Community 1 - "Settings"
Cohesion: 0.09
Nodes (16): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+8 more)

### Community 2 - "Isle.Core"
Cohesion: 0.06
Nodes (14): Isle.Views.Music, Isle.Modules.Lyrics, Isle.Core, Isle, Isle.Views, Isle.Modules.Hotkeys, Isle.Modules.Media, Isle.Modules.Tray (+6 more)

### Community 3 - "ModuleHost"
Cohesion: 0.05
Nodes (23): ARCHITECTURE, Process shape, Shell, Styles / Views, Tests (`tests/Isle.Tests`), Application, App, Host (+15 more)

### Community 4 - "IslandWindow"
Cohesion: 0.05
Nodes (17): ITabView, IslandWindow, CurrentTab, IdleView, TabList, IsHiddenByUser, PasteTarget, IsExpanded (+9 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "ARCHITECTURE"
Cohesion: 0.13
Nodes (9): Core (`src/Isle/Core`), DECISIONS, M0, M1, M2, Performance measurements, Memory, TrayIcon (+1 more)

### Community 7 - "Strings"
Cohesion: 0.22
Nodes (6): Strings, English, Instance, Language, this[], Ukrainian

### Community 8 - "EventBus"
Cohesion: 0.10
Nodes (8): Modules (`src/Isle/Modules`), EventBus, Unsubscriber, Entry, HotkeyInfo, HotkeyService, EventBusTests, Ping

### Community 9 - "Logger.Error"
Cohesion: 0.10
Nodes (7): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), Autostart, IsEnabled, JsonStore, Logger, MediaSession, Current

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.18
Nodes (7): Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0-windows10.0.19041.0, Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "Lrc.Parse"
Cohesion: 0.06
Nodes (10): Gesture, IsValid, Lrc, LrcLine, PlaybackMath, LrcTests, PlaybackTests, ExcludedAppTests (+2 more)

### Community 12 - "Color"
Cohesion: 0.13
Nodes (5): Theme, Accent, IsDark, ArtColors, ArtColorTests

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 18 - "PillItem"
Cohesion: 0.13
Nodes (17): PillItem, Click, Compact, CompactWidth, Height, Id, IsBanner, Peek (+9 more)

### Community 19 - "TabBar"
Cohesion: 0.09
Nodes (12): Icon, Data, Filled, Foreground, Stroke, MarqueeText, Inner, Text (+4 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "Equalizer"
Cohesion: 0.21
Nodes (4): ArtImage, Equalizer, Playing, TrackBanner

### Community 22 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.18
Nodes (10): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 9. Definition of Done (+2 more)

### Community 26 - "LyricsResult"
Cohesion: 0.18
Nodes (4): DiskEntry, LyricsResult, TrackQuery, CacheKey

### Community 27 - "Fact"
Cohesion: 0.20
Nodes (4): SettingsTests, StringsTests, TempDir, Path

### Community 28 - "MediaModule"
Cohesion: 0.17
Nodes (6): MediaModule, Id, Lyrics, LyricsAlwaysFetch, LyricsInPill, Source

### Community 29 - "MediaState"
Cohesion: 0.12
Nodes (16): MediaState, Accent, Album, Art, Artist, CanNext, CanPrev, CanSeek (+8 more)

### Community 30 - "FakeMediaSource"
Cohesion: 0.18
Nodes (4): FakeMediaSource, Current, IMediaSource, Current

### Community 31 - "IslandWindow"
Cohesion: 0.27
Nodes (3): TabInfo, IslandWindow, MediaSnapshots

### Community 32 - "LyricsTracker"
Cohesion: 0.19
Nodes (5): LyricsTracker, ForCurrent, Loading, Result, TrackKey

### Community 33 - "LyricsService"
Cohesion: 0.29
Nodes (5): Service, ILyricsHttp, LyricsService, FakeHttp, LyricsServiceTests

### Community 37 - "LyricsService.cs"
Cohesion: 0.22
Nodes (6): LyricsStatus, Error, Instrumental, NotFound, Plain, Synced

### Community 39 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

## Knowledge Gaps
- **127 isolated node(s):** `Host`, `Island`, `IsSnapshot`, `IsDark`, `Accent` (+122 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 265 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IslandWindow` connect `IslandWindow` to `Native`, `Isle.Core`, `Spring`, `EventBus`, `Lrc.Parse`, `Window`, `PillItem`, `TabBar`, `MediaModule`, `IslandWindow`?**
  _High betweenness centrality (0.254) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `MusicView` (e.g. with `MediaModule.Start()` and `MediaSnapshots.Tab()`) actually correct?**
  _`MusicView` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `IsSnapshot` to the rest of the system?**
  _127 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.05851619644723093 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Native`, `ModuleHost`, `Spring`, `LyricsService.cs`, `Strings`, `EventBus`, `Strings.cs`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **Should `Settings` be split into smaller, more focused modules?**
  _Cohesion score 0.09090909090909091 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `IslandWindow`?**
  _High betweenness centrality (0.069) - this node is a cross-community bridge._