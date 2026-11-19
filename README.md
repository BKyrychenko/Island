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
- **AI** — chat and text actions with your own Anthropic or OpenAI-compatible key (keys stay encrypted on this PC).
- **Voice to text** — hold Win+Alt+V, speak, release: offline Whisper types it into the window you were in.
- **Agents** — Claude Code / Codex sessions as cards, Allow / Deny permission requests from the island.
- **Characters** — Pebb, Sprig and Kubo, small creatures that breathe, watch the cursor, dance to music and nap.

## Build and run
Requires the .NET 10 SDK.

```powershell
.\scripts\build.ps1                        # build + test + publish into dist\
dist\Isle\Isle.exe                 # needs the .NET 10 Desktop Runtime
dist\Isle-selfcontained\Isle.exe   # no runtime needed (bigger)
```

Copy the whole `dist\Isle` (or `dist\Isle-selfcontained`) folder somewhere permanent before you enable autostart —
the `runtimes` folder next to the exe is needed for voice to text.

- **Settings**: the gear in the panel, or right-click the tray icon → Settings. Theme, size, language (English /
  Українська), monitor, auto-hide, click-through, excluded apps, modules on/off, per-module options, hotkeys.
- **Start with Windows**: Settings → General → Start with Windows (or the tray menu).
- **Quit**: tray icon → Quit.
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
Everything lives in `%AppData%\Isle\` (settings, notes, clipboard history, shelf, reminders, chats, caches, the Whisper
model, `log.txt`). API keys, the OBS password and clipboard text are encrypted with Windows DPAPI for your user
account. The log never contains clipboard content, messages or keys. Network is used only by features you turn on:
lyrics (LRCLIB), weather (Open-Meteo, optional IP location via geojs.io), currency (National Bank of Ukraine), AI
(your provider), and the one-time Whisper model download (Hugging Face, checksum verified).

Agent hooks are never installed silently: Settings → Options → Agents → Install / remove shows exactly what will be
changed, backs up the config file, and Remove takes out only Isle's entries.

## Debug flags
- `Isle.exe --snapshot <state|all> <out.png|dir> [--theme Dark|Light] [--lang en|uk] [--scale 1.5]` renders UI states to PNG.
- `Isle.exe --transcribe <in.wav> <out.txt> [--lang en|uk|nl|auto]` runs the voice engine on a file.
- `ISLE_DATA_DIR=<folder>` uses another data folder (handy for trying things without touching your data).

See `docs/VERIFICATION.md` for what was tested and the few things to check by hand, `docs/ARCHITECTURE.md` for the code map and
`docs/DECISIONS.md` for why things are the way they are.
