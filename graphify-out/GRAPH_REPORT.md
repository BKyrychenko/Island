# Graph Report - island  (2026-11-14)

## Corpus Check
- 49 files · ~23,357 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 645 nodes · 1306 edges · 26 communities (19 shown, 7 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 121 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5affb53e`
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
- HotkeyService
- Theme
- Isle
- Window
- VERIFICATION.md
- PillItem
- TabBar
- ResourceDictionary
- IdlePillView
- Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)
- Strings.cs
- CLAUDE.md
- Icons.xaml

## God Nodes (most connected - your core abstractions)
1. `IslandWindow` - 97 edges
2. `Native` - 42 edges
3. `Settings` - 30 edges
4. `Isle.Core` - 29 edges
5. `Window` - 24 edges
6. `ModuleHost` - 22 edges
7. `PillItem` - 21 edges
8. `HotkeyService` - 21 edges
9. `TabBar` - 21 edges
10. `4. Features (all of them)` - 19 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `M1` --references--> `Icon`  [INFERRED]
  DECISIONS.md → src/Isle/Views/Icon.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `Process shape` --references--> `IslandWindow`  [INFERRED]
  ARCHITECTURE.md → src/Isle/IslandWindow.Content.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Settings`  [INFERRED]
  SPEC.md → src/Isle/Core/Settings.cs

## Import Cycles
- None detected.

## Communities (26 total, 7 thin omitted)

### Community 0 - "Native"
Cohesion: 0.06
Nodes (14): ForegroundWatcher, LastExternal, TrayModule, Id, ICONINFO, MonitorEnumProc, MONITORINFOEX, Native (+6 more)

### Community 1 - "Settings"
Cohesion: 0.07
Nodes (22): Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys, Language (+14 more)

### Community 2 - "Isle.Core"
Cohesion: 0.06
Nodes (12): Isle.Core, Isle, Isle.Views, Isle.Modules.Tray, Isle.Tests, Paths, LogFile, Root (+4 more)

### Community 3 - "ModuleHost"
Cohesion: 0.12
Nodes (13): Process shape, Entry, IModule, Id, ModuleHost, Ids, ModuleState, Failed (+5 more)

### Community 4 - "IslandWindow"
Cohesion: 0.05
Nodes (13): Shell, ITabView, TabInfo, IslandWindow, CurrentTab, TabList, IsHiddenByUser, PasteTarget (+5 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.11
Nodes (19): 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key), 4.14 Agents tab — Claude Code and Codex integration, 4.15 Characters ("tenants"), 4.16 Voice to text, 4.17 Tray, settings, autostart, hotkeys (+11 more)

### Community 6 - "ARCHITECTURE"
Cohesion: 0.09
Nodes (12): ARCHITECTURE, Core (`src/Isle/Core`), Styles / Views, Tests (`tests/Isle.Tests`), DECISIONS, M0, M1, Performance measurements (+4 more)

### Community 7 - "Strings"
Cohesion: 0.22
Nodes (6): Strings, English, Instance, Language, this[], Ukrainian

### Community 8 - "EventBus"
Cohesion: 0.15
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 9 - "Logger.Error"
Cohesion: 0.08
Nodes (8): Application, App, Host, Island, Autostart, IsEnabled, JsonStore, Logger

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.18
Nodes (7): Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0-windows10.0.19041.0, Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "HotkeyService"
Cohesion: 0.10
Nodes (9): Modules (`src/Isle/Modules`), Isle.Modules.Hotkeys, Entry, Gesture, IsValid, HotkeyInfo, HotkeyService, ExcludedAppTests (+1 more)

### Community 12 - "Theme"
Cohesion: 0.15
Nodes (9): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS, Theme, Accent (+1 more)

### Community 14 - "Window"
Cohesion: 0.08
Nodes (34): Body, EmptyText, MoreList, MoreOverlay, Outline, Panel, PanelScale, PillLayer (+26 more)

### Community 18 - "PillItem"
Cohesion: 0.11
Nodes (18): PillItem, Click, Compact, CompactWidth, Height, Id, IsBanner, Peek (+10 more)

### Community 19 - "TabBar"
Cohesion: 0.10
Nodes (9): Icon, Data, Filled, Foreground, Stroke, TabBar, AvailableWidth, Hidden (+1 more)

### Community 20 - "ResourceDictionary"
Cohesion: 0.15
Nodes (16): Bd, Hover, On, PART_ContentHost, PART_Indicator, PART_Track, ResourceDictionary, Thumb (+8 more)

### Community 21 - "IdlePillView"
Cohesion: 0.21
Nodes (6): IdleView, IdlePillView, DesiredPillWidth, LeftSlot, Now, RightSlot

### Community 22 - "Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)"
Cohesion: 0.17
Nodes (11): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 5. Quality rules, 6. Performance verification, 7. Testing, 8. Milestones (in order; commit after each; update PROGRESS.md and the graph) (+3 more)

## Knowledge Gaps
- **89 isolated node(s):** `Host`, `Island`, `LastExternal`, `Id`, `Priority` (+84 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 215 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IslandWindow` connect `IslandWindow` to `Native`, `Settings`, `Isle.Core`, `ModuleHost`, `ARCHITECTURE`, `EventBus`, `Logger.Error`, `HotkeyService`, `Window`, `PillItem`, `IdlePillView`?**
  _High betweenness centrality (0.394) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `IslandWindow` (e.g. with `Process shape` and `App.OnStartup()`) actually correct?**
  _`IslandWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `LastExternal` to the rest of the system?**
  _89 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.06450617283950617 - nodes in this community are weakly interconnected._
- **Why does `Isle.Core` connect `Isle.Core` to `Settings`, `ModuleHost`, `Strings`, `EventBus`, `Logger.Error`, `HotkeyService`, `PillItem`, `Strings.cs`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Settings` (e.g. with `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` and `SettingsTests.Roundtrip_keeps_values()`) actually correct?**
  _`Settings` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Should `Settings` be split into smaller, more focused modules?**
  _Cohesion score 0.07058823529411765 - nodes in this community are weakly interconnected._