using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Isle.Core;

/// <summary>
/// Clipboard + synthetic-keystroke helpers shared by "paste into the previous window" (clipboard history, voice, AI
/// text actions): snapshot/restore the user's clipboard, send Ctrl+C / Ctrl+V, and a short "quiet" window during which
/// the clipboard history ignores changes (our own copies of the user's selection must not land in the history).
/// </summary>
public static class ClipboardOps
{
    static long _quietUntil;

    /// <summary>The clipboard history skips changes until this moment (Environment.TickCount64).</summary>
    public static bool IsQuiet => Environment.TickCount64 < Interlocked.Read(ref _quietUntil);

    public static void Quiet(TimeSpan span) => Interlocked.Exchange(ref _quietUntil, Environment.TickCount64 + (long)span.TotalMilliseconds);

    /// <summary>Text / image / file list currently on the clipboard (null when empty or unreadable).</summary>
    public static DataObject? Snapshot()
    {
        try
        {
            var d = new DataObject();
            var any = false;
            if (Clipboard.ContainsText()) { d.SetText(Clipboard.GetText()); any = true; }
            if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } img) { d.SetImage(img); any = true; }
            if (Clipboard.ContainsFileDropList()) { d.SetFileDropList(Clipboard.GetFileDropList()); any = true; }
            return any ? d : null;
        }
        catch { return null; }
    }

    /// <summary>Puts a snapshot back (null = leave the clipboard as it is). Returns false when the clipboard was busy.</summary>
    public static bool Restore(DataObject? previous)
    {
        if (previous is null) return true;
        try { Clipboard.SetDataObject(previous, true); return true; }
        catch (Exception ex) { Logger.Warn($"clipboard restore: {ex.GetType().Name}"); return false; }
    }

    /// <summary>Physically held Shift/Alt/Win/Ctrl (e.g. the hotkey that is still pressed).</summary>
    public static bool ModifiersHeld()
    {
        foreach (var vk in new[] { Native.VK_SHIFT, Native.VK_MENU, Native.VK_LWIN, Native.VK_RWIN, Native.VK_CONTROL })
            if ((Native.GetAsyncKeyState(vk) & 0x8000) != 0) return true;
        return false;
    }

    /// <summary>Sends Ctrl+<paramref name="vk"/> to the foreground window, first releasing physically held Shift/Alt/Win.</summary>
    public static void SendCtrl(ushort vk)
    {
        var inputs = new List<Native.INPUT>();
        foreach (var m in new[] { Native.VK_SHIFT, Native.VK_MENU, Native.VK_LWIN, Native.VK_RWIN })
            if ((Native.GetAsyncKeyState(m) & 0x8000) != 0) inputs.Add(Key(m, up: true));
        inputs.Add(Key(Native.VK_CONTROL, false));
        inputs.Add(Key(vk, false));
        inputs.Add(Key(vk, true));
        inputs.Add(Key(Native.VK_CONTROL, true));
        if (Native.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<Native.INPUT>()) == 0)
            Logger.Warn($"SendInput failed ({Marshal.GetLastWin32Error()})");

        static Native.INPUT Key(ushort vk, bool up) => new()
        {
            type = Native.INPUT_KEYBOARD,
            u = new Native.InputUnion { ki = new Native.KEYBDINPUT { wVk = vk, dwFlags = up ? Native.KEYEVENTF_KEYUP : 0 } },
        };
    }

    /// <summary>Waits (on the UI dispatcher, no blocking) until <paramref name="done"/> is true or the timeout passes.</summary>
    public static Task<bool> WaitFor(Func<bool> done, TimeSpan timeout, int stepMs = 30)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (done()) { tcs.SetResult(true); return tcs.Task; }
        var start = Environment.TickCount64;
        var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(stepMs) };
        t.Tick += (_, _) =>
        {
            bool ok;
            try { ok = done(); } catch { ok = false; }
            if (ok || Environment.TickCount64 - start >= timeout.TotalMilliseconds)
            {
                t.Stop();
                tcs.TrySetResult(ok);
            }
        };
        t.Start();
        return tcs.Task;
    }

    /// <summary>
    /// Copies the current selection of the foreground app: waits for the hotkey's modifiers to be released, sends
    /// Ctrl+C, waits for the clipboard to change, reads the text and puts the user's clipboard back. "" if nothing
    /// was selected. The clipboard history is kept quiet meanwhile.
    /// </summary>
    public static async Task<string> CopySelectionAsync()
    {
        await WaitFor(() => !ModifiersHeld(), TimeSpan.FromMilliseconds(1500));
        Quiet(TimeSpan.FromSeconds(2));
        var previous = Snapshot();
        var seq = Native.GetClipboardSequenceNumber();
        SendCtrl(Native.VK_C);
        var changed = await WaitFor(() => Native.GetClipboardSequenceNumber() != seq, TimeSpan.FromMilliseconds(700));
        var text = "";
        if (changed)
        {
            // the owner may still be writing; a short retry loop handles "clipboard busy"
            await WaitFor(() =>
            {
                try { text = Clipboard.ContainsText() ? Clipboard.GetText() : ""; return true; }
                catch (COMException) { return false; }
            }, TimeSpan.FromMilliseconds(400));
            Quiet(TimeSpan.FromSeconds(1));
            if (!Restore(previous)) { await WaitFor(() => Restore(previous), TimeSpan.FromMilliseconds(400), 80); }
        }
        return text;
    }

    /// <summary>Puts <paramref name="text"/> on the clipboard, focuses <paramref name="target"/>, sends Ctrl+V, and
    /// restores the previous clipboard 600 ms later. Used by "Replace selection".</summary>
    public static async Task PasteTextAsync(string text, IntPtr target)
    {
        var previous = Snapshot();
        Quiet(TimeSpan.FromSeconds(2));
        try { Clipboard.SetText(text); }
        catch (Exception ex) { Logger.Warn($"paste set: {ex.GetType().Name}"); return; }
        if (target != IntPtr.Zero && Native.GetForegroundWindow() != target) Native.SetForegroundWindow(target);
        await Task.Delay(90);
        await WaitFor(() => !ModifiersHeld(), TimeSpan.FromMilliseconds(1000));
        SendCtrl(Native.VK_V);
        await Task.Delay(600);
        Quiet(TimeSpan.FromSeconds(1));
        Restore(previous);
    }
}
