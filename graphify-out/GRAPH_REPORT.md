# Graph Report - island  (2026-11-14)

## Corpus Check
- 31 files · ~11,533 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 2, .manifest 1)

## Summary
- 348 nodes · 617 edges · 18 communities (14 shown, 4 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 49 edges (avg confidence: 0.88)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e1499f2a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Native
- Settings
- Isle.Core
- ModuleHost
- App
- 4. Features (all of them)
- ARCHITECTURE
- Strings
- EventBus
- Logger.Error
- Isle.Tests.csproj
- Screens.All
- PROGRESS
- Isle
- RECT
- VERIFICATION.md

## God Nodes (most connected - your core abstractions)
1. `Settings` - 30 edges
2. `Native` - 30 edges
3. `ModuleHost` - 23 edges
4. `4. Features (all of them)` - 19 edges
5. `Isle.Core` - 16 edges
6. `TrayModule` - 15 edges
7. `App` - 14 edges
8. `Strings` - 14 edges
9. `IslandWindow` - 14 edges
10. `Project: "Isle" — a full-featured Dynamic-Island-style overlay for Windows (personal use)` - 11 edges

## Surprising Connections (you probably didn't know these)
- `2. Hard requirements (non-negotiable)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `ModuleHost`  [INFERRED]
  SPEC.md → src/Isle/Core/ModuleHost.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Settings`  [INFERRED]
  SPEC.md → src/Isle/Core/Settings.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Strings`  [INFERRED]
  SPEC.md → src/Isle/Core/Strings.Data.cs
- `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` --references--> `Logger`  [INFERRED]
  SPEC.md → src/Isle/Core/Logger.cs

## Import Cycles
- None detected.

## Communities (18 total, 4 thin omitted)

### Community 0 - "Native"
Cohesion: 0.11
Nodes (7): TrayModule, Id, ICONINFO, MonitorEnumProc, Native, NOTIFYICONDATA, POINT

### Community 1 - "Settings"
Cohesion: 0.08
Nodes (21): Core (`src/Isle/Core`), Settings, AutoHideEdge, ClickThrough, Current, ExcludedApps, HideInFullscreen, Hotkeys (+13 more)

### Community 2 - "Isle.Core"
Cohesion: 0.07
Nodes (10): Isle.Core, Isle, Isle.Modules.Tray, Isle.Tests, Autostart, IsEnabled, Paths, LogFile (+2 more)

### Community 3 - "ModuleHost"
Cohesion: 0.11
Nodes (13): Process shape, Entry, IModule, Id, ModuleHost, Ids, ModuleState, Failed (+5 more)

### Community 4 - "App"
Cohesion: 0.07
Nodes (14): Shell, Application, App, Host, Island, Pill, Root, UiScale (+6 more)

### Community 5 - "4. Features (all of them)"
Cohesion: 0.07
Nodes (29): 0. How to work on this (read first), 1. What we are building, 2. Hard requirements (non-negotiable), 3. Tech stack and layout, 4.10 Command bar (default hotkey Alt+Space), 4.11 Screen tools (Tools tab + hotkeys), 4.12 Weather tab, 4.13 AI chat tab (bring-your-own key) (+21 more)

### Community 6 - "ARCHITECTURE"
Cohesion: 0.10
Nodes (10): ARCHITECTURE, Modules (`src/Isle/Modules`), Tests (`tests/Isle.Tests`), DECISIONS, M0, Performance measurements, Memory, TrayIcon (+2 more)

### Community 7 - "Strings"
Cohesion: 0.10
Nodes (8): Strings, LocExtension, Key, English, Instance, Language, this[], Ukrainian

### Community 8 - "EventBus"
Cohesion: 0.16
Nodes (4): EventBus, Unsubscriber, EventBusTests, Ping

### Community 9 - "Logger.Error"
Cohesion: 0.18
Nodes (3): 8. Milestones (in order; commit after each; update PROGRESS.md and the graph), JsonStore, Logger

### Community 10 - "Isle.Tests.csproj"
Cohesion: 0.18
Nodes (7): Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0-windows10.0.19041.0, Microsoft.NET.Sdk, net10.0-windows10.0.19041.0, Microsoft.NET.Sdk

### Community 11 - "Screens.All"
Cohesion: 0.36
Nodes (3): ScreenInfo, Screens, MONITORINFOEX

### Community 12 - "PROGRESS"
Cohesion: 0.29
Nodes (6): How to check quickly, Known blockers, Last action, Milestones, Next step, PROGRESS

### Community 14 - "RECT"
Cohesion: 0.67
Nodes (3): RECT, Height, Width

## Knowledge Gaps
- **46 isolated node(s):** `Host`, `Island`, `IsEnabled`, `Id`, `Stopped` (+41 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 138 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Isle.Core` connect `Isle.Core` to `EventBus`, `Screens.All`, `ModuleHost`, `Strings`?**
  _High betweenness centrality (0.178) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Settings` (e.g. with `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)` and `SettingsTests.Roundtrip_keeps_values()`) actually correct?**
  _`Settings` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Host`, `Island`, `IsEnabled` to the rest of the system?**
  _46 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Native` be split into smaller, more focused modules?**
  _Cohesion score 0.11378353376503238 - nodes in this community are weakly interconnected._
- **Why does `ModuleHost` connect `ModuleHost` to `EventBus`, `Logger.Error`, `App`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `ModuleHost` (e.g. with `Process shape` and `8. Milestones (in order; commit after each; update PROGRESS.md and the graph)`) actually correct?**
  _`ModuleHost` has 4 INFERRED edges - model-reasoned connections that need verification._
- **Should `Settings` be split into smaller, more focused modules?**
  _Cohesion score 0.08013937282229965 - nodes in this community are weakly interconnected._