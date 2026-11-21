using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Isle.Tests;

/// <summary>
/// Real keyboard focus needs the test window to own Win32 focus, which whatever is in the foreground on the machine
/// (an editor, a browser, another test window) can take at any moment. Activate the window and retry briefly instead
/// of asserting on the first attempt.
/// </summary>
static class FocusHelp
{
    public static bool Take(HwndSource source, UIElement element, int attempts = 40)
    {
        for (var i = 0; i < attempts; i++)
        {
            SetForegroundWindow(source.Handle);
            SetFocus(source.Handle);
            if (element.Focus() && ReferenceEquals(Keyboard.FocusedElement, element)) return true;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            Thread.Sleep(25);
        }
        return false;
    }

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern IntPtr SetFocus(IntPtr hwnd);
}
