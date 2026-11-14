using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Isle.Core;

namespace Isle;

// Pointer logic (hover-intent, leave delay, edge auto-hide), keyboard focus hand-over, and the rules that hide
// the island (fullscreen app, excluded app, user).
public partial class IslandWindow
{
    const int HoverIntentMs = 120, LeaveDelayMs = 250, EdgeHideDelayMs = 1500;

    DispatcherTimer? _hoverTimer;
    long _insideSince, _outsideSince;
    bool _sticky;           // opened by hotkey/API: stay open until the cursor has visited once
    bool _hasFocus;         // island took keyboard focus for a text box
    IntPtr _previousForeground;
    readonly HashSet<string> _holdOpen = [];
    readonly HashSet<string> _hideReasons = [];
    string _lastSavedTab = "";

    void InitHover()
    {
        // The 30 ms cursor timer only runs while the pointer is engaged with the island (or it is expanded).
        _hoverTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(30) };
        _hoverTimer.Tick += OnHoverTick;
        MouseEnter += (_, _) => EngagePointer();
        MouseMove += (_, _) => { if (!_hoverTimer.IsEnabled) EngagePointer(); };
        PreviewMouseLeftButtonDown += OnPreviewMouseDown;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Collapse(); e.Handled = true; }
        };
        Deactivated += (_, _) => OnDeactivated();
        PillLayer.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            var click = Pill.Primary?.Click;
            if (click is not null) Safe(click, "pill click");
            else Expand();
        };
        Loaded += (_, _) => UpdateEdgeHide();
    }

    void EngagePointer()
    {
        if (_edgeHidden) SetEdgeHidden(false);
        if (_hoverTimer is { IsEnabled: false })
        {
            _insideSince = _outsideSince = 0;
            _hoverTimer.Start();
        }
    }

    void OnHoverTick(object? sender, EventArgs e)
    {
        if (!IsVisible) { _hoverTimer!.Stop(); return; }
        var now = Environment.TickCount64;
        var inside = IsCursorInside();
        if (inside)
        {
            _outsideSince = 0;
            if (_insideSince == 0) _insideSince = now;
            _sticky = false;
            if (!_expanded && now - _insideSince >= HoverIntentMs && Pill.Primary?.HoverExpands != false) Expand();
            return;
        }

        _insideSince = 0;
        if (_outsideSince == 0) _outsideSince = now;
        if (_expanded)
        {
            if (now - _outsideSince >= LeaveDelayMs && !_sticky && !_hasFocus && _holdOpen.Count == 0) Collapse();
            return;
        }
        if (Settings.Current.AutoHideEdge && !_edgeHidden)
        {
            if (now - _outsideSince >= EdgeHideDelayMs && Pill.Primary?.Peek != true) SetEdgeHidden(true);
            else return; // keep ticking until it is time to hide
        }
        _hoverTimer!.Stop();
    }

    /// <summary>Cursor position in stage coordinates (DIPs at 100% scale), or null if unavailable.</summary>
    Point? CursorOnStage()
    {
        if (_source is null || !Native.GetCursorPos(out var pt)) return null;
        try
        {
            var p = PointFromScreen(new Point(pt.X, pt.Y));
            var s = UiScale.ScaleX;
            return new Point(p.X / s, p.Y / s - StageShift.Y);
        }
        catch { return null; }
    }

    bool IsCursorInside()
    {
        if (CursorOnStage() is not { } p) return false;
        double w = _expanded ? ExpandedWidth : _pillW, h = _expanded ? ExpandedHeight : _pillH;
        double m = _expanded ? 10 : 3;
        // the gap above the island counts as inside, so pushing the cursor to the screen edge keeps it open
        return p.X >= BaseWidth / 2 - w / 2 - m && p.X <= BaseWidth / 2 + w / 2 + m && p.Y >= -4 && p.Y <= TopGap + h + m;
    }

    // ---- expand / collapse ----

    /// <summary>Opens the panel. <paramref name="sticky"/>: keep it open until the cursor has entered once (hotkeys).</summary>
    public void Expand(string? tabId = null, bool sticky = false)
    {
        var tab = tabId ?? _requestedTab ?? Pill.Primary?.TabId;
        _requestedTab = null;
        if (tab is not null) SelectTab(tab, animate: _expanded);
        if (_expanded) { _sticky |= sticky; return; }

        _expanded = true;
        _sticky = sticky;
        if (_edgeHidden) _edgeHidden = false;
        NotifyCurrentView(shown: true);
        if (_hwnd != IntPtr.Zero) Native.RegisterHotKey(_hwnd, EscHotkeyId, 0, 0x1B);
        SetTargets();
        if (!_snapshotMode && _hoverTimer is { IsEnabled: false })
        {
            _insideSince = _outsideSince = 0;
            _hoverTimer.Start();
        }
        ExpandedChanged?.Invoke(true);
    }

    public void Collapse()
    {
        if (!_expanded) return;
        _expanded = false;
        _sticky = false;
        HideMoreOverlay();
        NotifyCurrentView(shown: false);
        ReleaseFocus(restore: true);
        if (_hwnd != IntPtr.Zero) Native.UnregisterHotKey(_hwnd, EscHotkeyId);
        SetTargets();
        if (_currentTab is not null && _currentTab != _lastSavedTab && !_snapshotMode)
        {
            _lastSavedTab = _currentTab;
            Settings.Current.LastTab = _currentTab;
            JsonStore.Save(Paths.SettingsFile, Settings.Current); // quiet save: no Changed broadcast needed
        }
        ExpandedChanged?.Invoke(false);
    }

    public void Toggle()
    {
        if (_expanded) Collapse();
        else Expand(sticky: true);
    }

    /// <summary>Keeps the panel open regardless of the pointer until <see cref="ReleaseOpen"/> (drag, dialogs, busy work).</summary>
    public void HoldOpen(string key) => _holdOpen.Add(key);

    public void ReleaseOpen(string key)
    {
        if (_holdOpen.Remove(key) && _expanded && _hoverTimer is { IsEnabled: false }) _hoverTimer.Start();
    }

    // ---- keyboard focus ----

    void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Text inputs need real keyboard focus; everything else works without activating the island.
        for (var d = e.OriginalSource as DependencyObject; d is not null; d = GetParent(d))
        {
            if (d is TextBoxBase or PasswordBox) { AcquireFocus(); return; }
        }
        if (MoreOverlay.IsVisible && !MoreOverlay.IsMouseOver) HideMoreOverlay();
    }

    static DependencyObject? GetParent(DependencyObject d) =>
        d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);

    /// <summary>Activates the island so a text box can receive typing; remembers who had focus before.</summary>
    public void AcquireFocus()
    {
        if (_hasFocus || _hwnd == IntPtr.Zero) return;
        var fg = Native.GetForegroundWindow();
        if (fg != _hwnd) _previousForeground = fg;
        _hasFocus = true;
        Native.SetForegroundWindow(_hwnd);
        Activate();
    }

    void ReleaseFocus(bool restore)
    {
        if (!_hasFocus) return;
        _hasFocus = false;
        Keyboard.ClearFocus();
        if (restore && _previousForeground != IntPtr.Zero && Native.IsWindow(_previousForeground) && Native.GetForegroundWindow() == _hwnd)
            Native.SetForegroundWindow(_previousForeground);
    }

    void OnDeactivated()
    {
        if (!_hasFocus) return;
        _hasFocus = false;      // the user clicked another window: focus is already gone, do not pull it back
        Keyboard.ClearFocus();
        if (_expanded && !IsCursorInside() && _holdOpen.Count == 0) Collapse();
    }

    /// <summary>The window to paste into: whoever was in front before the island (or right now).</summary>
    public IntPtr PasteTarget => _hasFocus && _previousForeground != IntPtr.Zero ? _previousForeground : Watcher.LastExternal;

    // ---- edge auto-hide ----

    void SetEdgeHidden(bool hidden)
    {
        if (_edgeHidden == hidden) return;
        _edgeHidden = hidden;
        SetTargets();
    }

    /// <summary>Re-evaluates auto-hide after a settings or pill change.</summary>
    void UpdateEdgeHide()
    {
        if (_snapshotMode) return;
        if (!Settings.Current.AutoHideEdge || Pill.Primary?.Peek == true)
        {
            SetEdgeHidden(false);
            if (Settings.Current.AutoHideEdge) EngagePointer(); // re-hide after the peek ends
            return;
        }
        if (!_expanded && !_edgeHidden) EngagePointer(); // the timer hides it after the delay
    }

    // ---- hide rules ----

    /// <summary>Hides/shows the whole island for a named reason ("user", "fullscreen", "excluded"). Shown only when no reason is left.</summary>
    public void SetHidden(string reason, bool hidden)
    {
        var changed = hidden ? _hideReasons.Add(reason) : _hideReasons.Remove(reason);
        if (!changed) return;
        if (_hideReasons.Count > 0)
        {
            if (IsVisible)
            {
                Collapse();
                _hoverTimer?.Stop();
                Hide();
            }
        }
        else if (!IsVisible)
        {
            Show();
            Reposition();
            UpdateEdgeHide();
        }
    }

    public bool IsHiddenByUser => _hideReasons.Contains("user");

    void OnForegroundChanged(IntPtr hwnd)
    {
        CheckForeground();
        // apps often go fullscreen a moment after they come to the front
        Dispatcher.BeginInvoke(CheckForeground, DispatcherPriority.ApplicationIdle);
    }

    void CheckForeground()
    {
        if (_snapshotMode || _hwnd == IntPtr.Zero) return;
        try
        {
            var s = Settings.Current;
            var fg = Native.GetForegroundWindow();
            SetHidden("fullscreen", s.HideInFullscreen && ForegroundWatcher.IsFullscreen(fg, Screen));
            var excluded = false;
            if (s.ExcludedApps.Count > 0 && fg != IntPtr.Zero && !ForegroundWatcher.IsOwn(fg))
            {
                var name = ForegroundWatcher.ProcessName(fg);
                excluded = name.Length > 0 && s.ExcludedApps.Any(a => IsSameApp(a, name));
            }
            SetHidden("excluded", excluded);
        }
        catch (Exception ex) { Logger.Error("foreground check", ex); }
    }

    internal static bool IsSameApp(string listed, string processName)
    {
        listed = listed.Trim();
        if (listed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) listed = listed[..^4];
        return listed.Length > 0 && string.Equals(listed, processName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A 2-second check for windows that go fullscreen without a foreground change (F11, video). Only while the setting is on.</summary>
    void UpdateHousekeeping()
    {
        if (_snapshotMode) return;
        var needed = Settings.Current.HideInFullscreen;
        if (needed && _housekeeping is null)
        {
            _housekeeping = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(2) };
            _housekeeping.Tick += (_, _) => CheckForeground();
        }
        if (needed) _housekeeping!.Start();
        else _housekeeping?.Stop();
    }

    static void Safe(Action action, string what)
    {
        try { action(); }
        catch (Exception ex) { Logger.Error(what, ex); }
    }
}
