# Isle

A small, fast, always-on-top "island" pill for Windows 10/11. Collapsed, it shows what is going on (music, timers,
downloads, recording, mic/camera in use, agent requests…). Hover it and it opens into a panel with tabs and tools.
Personal project: everything included, no accounts, no telemetry, no ads.

## What it does
- **Pill** — live activity with a priority queue: finished timer/reminder → agent waiting for you → recording →
  mic/camera in use → running timer → download/Steam progress → music → idle (optional clock, temperature, mascot).
- **Music** — any app that reports to Windows media controls (Spotify, browsers, VLC…): art, progress, seek, controls,
  album-colour accent, synced lyrics from LRCLIB.
- **Timers & reminders** — presets, free input (`90`, `1:30`, `1h30m`), stopwatch, reminders in plain English or
  Ukrainian (`remind me in 2 hours to call mom`, `щодня о 22:00 пити воду`), repeat rules, snooze.
- **Notes** — formatted notes (bold/italic/underline, lists, checkboxes, headings), folders, pins, search, autosave.
- **Control** — Wi-Fi, Bluetooth (+ connect headphones), dark mode, volume, brightness, VPN profiles/clients.
- **Clipboard** — text + image history (text encrypted), search, pins; click an entry to paste it where you were.
- **Shelf** — drag files onto the island to park them, drag them out again later.
- **Command bar** (Ctrl+Alt+Space) — apps and files, calculator and unit conversion, currency (NBU rates), timers and
  reminders, system commands (with confirmation), web search, `? question` for AI.
- **Screen tools** — screenshot with markup, text capture (OCR), QR reading, colour picker, screen recording to MP4.
- **Weather** — Open-Meteo forecast (24 h + 7 days), temperature in the pill, rain alerts.
- **AI** — chat and text actions through a local Codex or Claude Code sign-in.
- **Voice to text** — hold Win+Alt+V, speak, release: offline Whisper types it into the window you were in.
- **Agents** — Claude Code / Codex cards with permission requests, a shortcut to the session's console or Windows Terminal tab, subagent progress, and a preview of the latest reported file edit.
- **Characters** — Pebb, Sprig and Kubo, small creatures that breathe, watch the cursor, dance to music and nap.

## Build and run
Requires the .NET 10 SDK, `rustc` 1.85 or later on `PATH` with the Windows x64 MSVC target, and Visual Studio C++ Build Tools with a Windows SDK.

```powershell
.\scripts\build.ps1                                # build, test and publish
.\dist\Isle\Isle.Launcher.exe                        # needs the .NET 10 Desktop Runtime
.\dist\Isle-selfcontained\Isle.Launcher.exe          # bundles the WPF runtime
```

`Isle.Launcher.exe` starts the adjacent `Isle.exe`. The WPF app owns the UI, modules and settings.
Use `-OutputDirectory dist\hybrid` to publish separately while an existing copy is running.
Copy the whole `dist\Isle` (or `dist\Isle-selfcontained`) folder somewhere permanent before you enable autostart —
the `runtimes` folder next to the exe is needed for voice to text.

- **Settings**: the gear in the panel, or right-click the tray icon → Settings. Theme, size, language (English /
  Українська), monitor, auto-hide, click-through, excluded apps, modules on/off, per-module options, hotkeys.
- **Start with Windows**: Settings → General → Start with Windows (or the tray menu).
- **Quit**: tray icon → Quit, or Alt+F4 with the island focused. Isle waits for an active recording to finish saving.
- **Updates**: pull/rebuild and run `scripts/build.ps1` again.

## Hotkeys (all can be changed in Settings → Hotkeys)
| Action | Default |
|---|---|
| Command bar | Ctrl+Alt+Space |
| Show / hide island | Win+Alt+Space |
| New note | Win+Alt+N |
| AI text actions | Win+Alt+A |
| Hold to talk (voice to text) | Win+Alt+V |
| Screenshot | Win+Alt+S |
| Start / stop recording | Win+Alt+R (Xbox Game Bar often owns this one — rebind it) |
| Colour picker | Win+Alt+C |
| Read QR / text from screen | Win+Alt+Q |

## Data and privacy

In the WPF AI tab, choose **Codex (ChatGPT)** or **Claude Code** under the gear. Install the corresponding Windows CLI
and sign in there. Isle sends the conversation context and selected attachments through that CLI in a background
process. Codex is the default and streams text on compatible CLI configurations. Its compatibility mode displays the
completed response. The selected account's plan limits apply.

Everything lives in `%AppData%\Isle\` (settings, notes, clipboard history, shelf, reminders, chats, caches, the Whisper
model, `log.txt`). The OBS password and clipboard text are encrypted with Windows DPAPI for your user
account. The log never contains clipboard content or messages. Network is used only by features you turn on:
lyrics (LRCLIB), weather (Open-Meteo, optional IP location via geojs.io), currency (National Bank of Ukraine), AI
(your installed CLI when you send a request or test the connection), and the one-time Whisper model download
(Hugging Face, checksum verified).

Agent hooks are never installed silently: Settings → Options → Agents → Install / remove shows exactly what will be
changed, backs up the config file, and Remove takes out only Isle's entries.
Use **Update** for an existing hook setup and start a new agent session to receive subagent and file-edit events.
Windows Terminal tabs need to accept application title changes for exact tab selection. File previews use reported Claude file-tool edits and Codex `apply_patch` results.

## Debug flags
- `Isle.Launcher.exe --snapshot <state|all> <out.png|dir> [--theme Dark|Light] [--lang en|uk] [--scale 1.5]` renders UI states to PNG.
- `Isle.Launcher.exe --transcribe <in.wav> <out.txt> [--lang en|uk|nl|auto]` runs the voice engine on a file.
- `ISLE_DATA_DIR=<folder>` uses another data folder (handy for trying things without touching your data).

See `docs/VERIFICATION.md` for what was tested and the few things to check by hand, `docs/ARCHITECTURE.md` for the code map and
`docs/DECISIONS.md` for why things are the way they are.
