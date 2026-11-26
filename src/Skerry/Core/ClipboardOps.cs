using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Skerry.Core;

/// <summary>
/// Clipboard + synthetic-keystroke helpers shared by "paste into the previous window" (clipboard history, voice, AI
/// text actions): snapshot/restore the user's clipboard and send Ctrl+C / Ctrl+V.
/// History ignores only our writes and the selection copy requested from the target process.
/// </summary>
public static class ClipboardOps
{
    internal sealed class CaptureGuard
    {
        readonly Queue<uint> _ownSequences = new();
        uint _baseline, _targetProcess;
        public uint SelectionSequence { get; private set; }
        public bool Writing { get; set; }

        public void BeginSelection(uint baseline, uint targetProcess)
        {
            _baseline = baseline;
            _targetProcess = targetProcess;
            SelectionSequence = 0;
        }

        public void EndSelection() => _targetProcess = 0;

        public void OwnWrite(uint sequence)
        {
            if (sequence == 0 || _ownSequences.Contains(sequence)) return;
            _ownSequences.Enqueue(sequence);
            // ponytail: retain 32 recent sequence IDs; increase only if clipboard notifications lag that far.
            if (_ownSequences.Count > 32) _ownSequences.Dequeue();
        }

        public bool Ignore(uint sequence, uint ownerProcess)
        {
            if (sequence == 0) return false;
            if (Writing) { OwnWrite(sequence); return true; }
            if (_ownSequences.Contains(sequence)) return true;
            if (_targetProcess == 0 || sequence == _baseline || ownerProcess != _targetProcess) return false;
            SelectionSequence = sequence;
            EndSelection();
            OwnWrite(sequence);
            return true;
        }
    }

    static readonly CaptureGuard Guard = new();

    internal static bool IgnoreCapture(uint sequence)
    {
        Native.GetWindowThreadProcessId(Native.GetClipboardOwner(), out var ownerProcess);
        return Guard.Ignore(sequence, ownerProcess);
    }

    internal static void Write(Action write)
    {
        var before = Native.GetClipboardSequenceNumber();
        Guard.Writing = true;
        try { write(); }
        finally
        {
            var after = Native.GetClipboardSequenceNumber();
            if (after != before) Guard.OwnWrite(after);
            Guard.Writing = false;
        }
    }

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
        try { Write(() => Clipboard.SetDataObject(previous, true)); return true; }
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
    /// was selected. Only the requested selection copy is excluded from history.
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
            Native.GetWindowThreadProcessId(target, out var targetProcess);
            Guard.BeginSelection(seq, targetProcess);
            if (!SendCtrl(Native.VK_C)) return "";
            if (!await WaitFor(() =>
            {
                IgnoreCapture(Native.GetClipboardSequenceNumber());
                return Guard.SelectionSequence != 0;
            }, TimeSpan.FromMilliseconds(700))) return "";
            var copiedSequence = Guard.SelectionSequence;
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
        finally { Guard.EndSelection(); Borrow.Release(); }
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
            ExcludeFromHistory(payload);
            Write(() => Clipboard.SetDataObject(payload, true));
            var sequence = Native.GetClipboardSequenceNumber();
            if (!Native.IsWindow(target)) return ManualResult(sequence);
            if (Native.GetForegroundWindow() != target) Native.SetForegroundWindow(target);
            if (!await WaitFor(() => Native.GetForegroundWindow() == target, TimeSpan.FromMilliseconds(300))) return ManualResult(sequence);
            if (!await WaitFor(() => !ModifiersHeld(), TimeSpan.FromMilliseconds(1000))) return ManualResult(sequence);
            if (!Native.IsWindow(target) || Native.GetForegroundWindow() != target ||
                Native.GetClipboardSequenceNumber() != sequence || !SendCtrl(Native.VK_V)) return ManualResult(sequence);
            await WaitFor(() => Native.GetClipboardSequenceNumber() != sequence, TimeSpan.FromMilliseconds(1500));
            return await WaitFor(() => Restore(previous, sequence), TimeSpan.FromMilliseconds(400), 80)
                ? PasteResult.Pasted : PasteResult.RestoreFailed;
        }
        finally { Borrow.Release(); }
    }
}
