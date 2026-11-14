# DECISIONS

One line each: decision — why.

## M0
- Repo root is the solution root (`Isle.sln` next to `SPEC.md`) — the working folder already is the dedicated project folder.
- `Isle.sln` in classic `.sln` format (not `.slnx`) — matches the spec's file name and older tooling.
- Tray icon via raw `Shell_NotifyIcon` + native popup menu, not WinForms `NotifyIcon` — avoids loading WinForms (RAM budget).
- Tray icon pixels are generated in code (`TrayIcon.Render`) — original artwork, no asset files needed.
- Window is a WPF layered window (`AllowsTransparency`) — per-pixel alpha gives click-through outside the shape for free.
- **Software rendering by default** (`RenderOptions.ProcessRenderMode = SoftwareOnly`, option `render.hardware` to switch) —
  measured on this machine (i5-12450H, Intel Xe): hardware mode loads the Intel driver (`igd12dxva64` + `igc64`) and idles at
  157 MB working set / 109 MB private; software mode idles at 76 MB / 32 MB. The window is small (640×300 DIP) and layered
  windows need a GPU→CPU read-back anyway, so software rasterisation is the cheaper path. Smoothness to be measured in M1.
- `Memory.TrimSoon()` (full GC + `EmptyWorkingSet`) runs once after start-up and will run after heavy features finish —
  spec requires memory to be released after heavy use. Measured idle after trim: 8 MB working set, 31 MB private, 0 ms CPU / 30 s.
- GC: workstation, non-concurrent — no background GC thread, lower idle footprint.
- `TreatWarningsAsErrors` on for the app project — spec §5.
- Settings: typed core fields + a free-form `Options` string dictionary for per-module options — avoids touching the
  settings class and migration for every small module option.
- No NuGet packages in the app so far. Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk (standard test stack).
- `logs/` is git-ignored — it holds the owner's run logs and my verification screenshots (`logs/shots`).
- graphify: the preinstalled CLI shim was broken and pip could not build it on Python 3.15, so it was reinstalled with `uv tool install graphifyy --python 3.12`. Graph is rebuilt with `graphify update .` (AST only, no LLM, seconds); only `GRAPH_REPORT.md` is committed, the rest of `graphify-out/` is regenerated locally. `.graphifyignore` excludes bin/obj/dist/logs.

## Performance measurements
| When | State | Working set | Private | CPU |
|---|---|---|---|---|
| M0 | idle pill, tray only, software rendering, after trim | 8 MB | 31 MB | 0 ms over 30 s |
| M0 | same, before trim | 76 MB | 32 MB | — |
| M0 | hardware rendering (rejected default) | 157 MB | 109 MB | — |
