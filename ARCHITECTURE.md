# ARCHITECTURE

Hand-written module map. One paragraph per module: what it owns, public surface, lifetime, data files.
Keep it short and current (update at the end of every milestone).

## Process shape
One WPF process (`src/Isle`, `Isle.exe`). `App.OnStartup` installs exception handlers, takes the single-instance
mutex, applies language and render mode, shows `IslandWindow`, and only after the first frame
(`DispatcherPriority.ApplicationIdle`) registers modules with `ModuleHost` and calls `Sync()`.
Static access points: `App.Host` (modules), `App.Island` (the window), `Settings.Current`.

## Shell
- **App** (`App.xaml.cs`) — start-up/shutdown, single instance (`Local\Isle.SingleInstance.*` mutex), global exception
  logging, `ToggleIsland()`, `Quit()`, repositions the island on resume from sleep.
- **IslandWindow** (`IslandWindow.xaml(.cs)`) — the only always-present window. Fixed size (`BaseWidth×BaseHeight` × UI
  scale), transparent, topmost, `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`, optional `WS_EX_TRANSPARENT` (click-through
  setting). `Reposition()` puts it top-centre of the selected monitor in physical pixels; called on display/DPI/setting
  changes. UI scale is a `LayoutTransform` on the root grid.
- **Native** (`Native.cs`) — P/Invoke declarations only.

## Core (`src/Isle/Core`)
- **Paths** — data root `%AppData%\Isle` (override: `ISLE_DATA_DIR` or `Paths.Root`), file/dir helpers.
- **Logger** — `log.txt`, rotates to `log.txt.1` at 1 MB, never throws. No secrets/user content.
- **JsonStore** — `Load<T>` / `Save<T>` / `WriteAtomic` (temp + replace) / `BackupCorrupt` (`*.bak-<timestamp>`). Every store uses it.
- **Settings** — `settings.json`; typed fields + `Modules` (id → enabled), `Hotkeys`, `Options` (free-form per-module
  strings). `Settings.Current`, `Save(changedName)` raises `Settings.Changed`. `Migrate()` upgrades old versions.
- **Strings** — EN/UK dictionaries (`Strings.Data.cs`), `Strings.T(key)`, XAML `{core:Loc key}` (live language switch).
- **EventBus** — typed publish/subscribe between modules and views.
- **ModuleHost / IModule** — modules are registered as factories, constructed and started only when enabled
  (`Sync()`), stopped/disposed when disabled; a throwing module becomes `Failed` without affecting others.
- **Screens** — monitor enumeration (physical pixels, DPI scale); `Selected(index)`.
- **Autostart** — `HKCU\...\Run` value `Isle`.
- **Memory** — `TrimSoon()`: GC + `EmptyWorkingSet` after start-up and after heavy features.

## Modules (`src/Isle/Modules`)
- **Tray** (`tray`) — notification-area icon (`Shell_NotifyIcon`, hidden message window), native menu:
  Show/Hide, Settings, Start with Windows, Quit. Icon drawn by `TrayIcon.Render`. Re-adds itself on `TaskbarCreated`.

## Tests (`tests/Isle.Tests`)
xUnit; `TestSetup` redirects `Paths.Root` to a temp folder. Pure logic only; UI is checked through snapshots (from M1).
