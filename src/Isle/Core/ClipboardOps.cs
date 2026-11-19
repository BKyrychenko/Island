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

    static readonly SemaphoreSlim Borrow = new(1, 1);

    /// <summary>Eagerly preserve every exact format before changing clipboard ownership.</summary>
    public static DataObject Snapshot()
    {
        var sequence = Native.GetClipboardSequenceNumber();
        var source = Clipboard.GetDataObject();
        var copy = CopyFormats(source);
        if (Native.GetClipboardSequenceNumber() != sequence)
            throw new InvalidOperationException("Clipboard changed while preserving its formats.");
        return copy;
    }

    internal static DataObject CopyFormats(IDataObject? source)
    {
        var copy = new DataObject();
        if (source is null) return copy;
        foreach (var format in source.GetFormats(autoConvert: false))
        {
            var value = source.GetData(format, autoConvert: false)
                ?? throw new InvalidOperationException("Clipboard format could not be preserved.");
            if (value is Stream stream)
            {
                if (!stream.CanSeek) throw new InvalidOperationException("Clipboard stream cannot be preserved.");
                var position = stream.Position;
                try
                {
                    stream.Position = 0;
                    var bytes = new MemoryStream();
                    stream.CopyTo(bytes);
                    bytes.Position = 0;
                    value = bytes;
                }
                finally { stream.Position = position; }
            }
            copy.SetData(format, value, autoConvert: false);
        }
        return copy;
    }

    /// <summary>The temporary payload opts out of Windows history and cloud synchronization.</summary>
    internal static void ExcludeFromHistory(DataObject payload)
    {
        payload.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[4]));
        payload.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4]));
        payload.SetData("CanUploadToCloudClipboard", new MemoryStream(new byte[4]));
    }

    static bool Restore(DataObject previous, uint sequence)
    {
        if (Native.GetClipboardSequenceNumber() != sequence) return true;
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
    public static bool SendCtrl(ushort vk)
    {
        var inputs = new List<Native.INPUT>();
        foreach (var m in new[] { Native.VK_SHIFT, Native.VK_MENU, Native.VK_LWIN, Native.VK_RWIN })
            if ((Native.GetAsyncKeyState(m) & 0x8000) != 0) inputs.Add(Key(m, up: true));
        inputs.Add(Key(Native.VK_CONTROL, false));
        inputs.Add(Key(vk, false));
        inputs.Add(Key(vk, true));
        inputs.Add(Key(Native.VK_CONTROL, true));
        var sent = Native.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<Native.INPUT>());
        if (sent == inputs.Count) return true;
        Logger.Warn($"SendInput incomplete ({Marshal.GetLastWin32Error()})");
        Native.SendInput(2, [Key(vk, true), Key(Native.VK_CONTROL, true)], Marshal.SizeOf<Native.INPUT>());
        return false;

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
        await Borrow.WaitAsync();
        try
        {
            var target = Native.GetForegroundWindow();
            if (!await WaitFor(() => !ModifiersHeld(), TimeSpan.FromMilliseconds(1500)) ||
                !Native.IsWindow(target) || Native.GetForegroundWindow() != target) return "";
            var previous = Snapshot();
            var seq = Native.GetClipboardSequenceNumber();
            Quiet(TimeSpan.FromSeconds(3));
            if (!SendCtrl(Native.VK_C)) return "";
            if (!await WaitFor(() => Native.GetClipboardSequenceNumber() != seq, TimeSpan.FromMilliseconds(700))) return "";
            var copiedSequence = Native.GetClipboardSequenceNumber();
            var text = "";
            try
            {
                await WaitFor(() =>
                {
                    if (Native.GetClipboardSequenceNumber() != copiedSequence) return true;
                    try { text = Clipboard.ContainsText() ? Clipboard.GetText() : ""; return true; }
                    catch (COMException) { return false; }
                }, TimeSpan.FromMilliseconds(400));
            }
            finally
            {
                if (!await WaitFor(() => Restore(previous, copiedSequence), TimeSpan.FromMilliseconds(400), 80))
                    throw new InvalidOperationException("Clipboard could not be restored after copying the selection.");
            }
            return text;
        }
        finally { Borrow.Release(); }
    }

    public enum PasteResult { Pasted, Manual, Changed, RestoreFailed }

    public static string? PasteNotice(PasteResult result) => result switch
    {
        PasteResult.Manual => Strings.T("clipboard.pasteManual"),
        PasteResult.Changed => Strings.T("clipboard.pasteChanged"),
        PasteResult.RestoreFailed => Strings.T("clipboard.restoreFailed"),
        _ => null,
    };

    public static Task<PasteResult> PasteTextAsync(string text, IntPtr target)
    {
        var data = new DataObject();
        data.SetText(text);
        return PasteDataAsync(data, target);
    }

    /// <summary>Paste result identifies whether our payload is still available for a manual paste.</summary>
    static PasteResult ManualResult(uint sequence) => Native.GetClipboardSequenceNumber() == sequence
        ? PasteResult.Manual : PasteResult.Changed;

    public static async Task<PasteResult> PasteDataAsync(DataObject payload, IntPtr target)
    {
        await Borrow.WaitAsync();
        try
        {
            var previous = Snapshot();
            Quiet(TimeSpan.FromSeconds(4));
            ExcludeFromHistory(payload);
            Clipboard.SetDataObject(payload, true);
            var sequence = Native.GetClipboardSequenceNumber();
            if (!Native.IsWindow(target)) return ManualResult(sequence);
            if (Native.GetForegroundWindow() != target) Native.SetForegroundWindow(target);
            if (!await WaitFor(() => Native.GetForegroundWindow() == target, TimeSpan.FromMilliseconds(300))) return ManualResult(sequence);
            if (!await WaitFor(() => !ModifiersHeld(), TimeSpan.FromMilliseconds(1000))) return ManualResult(sequence);
            if (!Native.IsWindow(target) || Native.GetForegroundWindow() != target ||
                Native.GetClipboardSequenceNumber() != sequence || !SendCtrl(Native.VK_V)) return ManualResult(sequence);
            await WaitFor(() => Native.GetClipboardSequenceNumber() != sequence, TimeSpan.FromMilliseconds(1500));
            Quiet(TimeSpan.FromSeconds(1));
            return await WaitFor(() => Restore(previous, sequence), TimeSpan.FromMilliseconds(400), 80)
                ? PasteResult.Pasted : PasteResult.RestoreFailed;
        }
        finally { Borrow.Release(); }
    }
}
