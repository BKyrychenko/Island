using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Isle.Core;
using Isle.Modules.ControlCenter;
using Windows.Devices.Radios;

namespace Isle.Views.Control;

/// <summary>
/// Control tab: four tiles (Wi-Fi, Bluetooth with a devices list, Windows dark mode, VPN list), master volume with mute,
/// and screen brightness (hidden when the display does not support it). Refreshes when shown and on device events.
/// </summary>
public sealed class ControlView : Grid, ITabView, IDisposable
{
    readonly ControlServices _s;
    readonly Tile _wifi, _bt, _dark, _vpn;
    readonly Slider _volume = new() { Minimum = 0, Maximum = 100, VerticalAlignment = VerticalAlignment.Center };
    readonly Slider _bright = new() { Minimum = 0, Maximum = 100, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock _volumeText = new(), _brightText = new();
    readonly Button _muteButton;
    readonly Icon _muteIcon = new() { Width = 16, Height = 16 };
    readonly FrameworkElement _brightRow;
    readonly TextBlock _message = new() { FontSize = 11, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(2, 6, 0, 0) };
    readonly Border _overlay = new() { Visibility = Visibility.Collapsed, CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), Padding = new Thickness(6), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 0, 0) };
    readonly StackPanel _overlayList = new();
    readonly TextBlock _overlayTitle = new() { FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 2, 0, 6) };
    List<VpnEntry> _vpns = [];
    List<BtDevice> _btDevices = [];
    string _overlayKind = "";
    bool _updatingSliders;
    readonly HashSet<string> _busy = [];

    public ControlView(ControlServices services)
    {
        _s = services;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var tiles = new UniformGrid { Columns = 4, Rows = 1, Height = 58 };
        _wifi = new Tile("Icon.Wifi", "cc.wifi", ToggleWifi);
        _bt = new Tile("Icon.Bluetooth", "cc.bluetooth", ToggleBluetooth, () => ShowOverlay("bt"));
        _dark = new Tile("Icon.Moon", "cc.dark", ToggleDark);
        _vpn = new Tile("Icon.Shield", "cc.vpn", () => ShowOverlay("vpn"));
        foreach (var t in new[] { _wifi, _bt, _dark, _vpn }) tiles.Children.Add(t);
        Children.Add(tiles);

        // volume
        _muteButton = new Button { Width = 30, Height = 30, Content = _muteIcon };
        _muteButton.SetResourceReference(StyleProperty, "IconButton");
        _muteButton.SetBinding(ToolTipProperty, Ui.Loc("cc.mute"));
        _muteIcon.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        _muteButton.Click += (_, _) => { if (_s.Audio.Available) _s.Audio.SetMuted(!_s.Audio.Muted); };
        _volume.ValueChanged += (_, e) => { if (!_updatingSliders) { _s.Audio.SetLevel((float)(e.NewValue / 100)); ShowVolume(); } };
        var volumeRow = SliderRow(_muteButton, _volume, _volumeText);
        volumeRow.Margin = new Thickness(0, 10, 0, 0);
        SetRow(volumeRow, 1);
        Children.Add(volumeRow);

        // brightness
        var sun = new Icon { Width = 16, Height = 16 };
        sun.SetResourceReference(Icon.DataProperty, "Icon.Sun");
        sun.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        var sunHost = new Border { Width = 30, Height = 30, Child = sun };
        _bright.ValueChanged += (_, e) => { if (!_updatingSliders) { _ = _s.Brightness.SetAsync((int)Math.Round(e.NewValue)); _brightText.Text = $"{(int)Math.Round(e.NewValue)}%"; } };
        _brightRow = SliderRow(sunHost, _bright, _brightText);
        _brightRow.Margin = new Thickness(0, 2, 0, 0);
        _brightRow.Visibility = Visibility.Collapsed;
        SetRow(_brightRow, 2);
        Children.Add(_brightRow);

        _message.SetResourceReference(TextBlock.ForegroundProperty, "WarnBrush");
        SetRow(_message, 3);
        Children.Add(_message);

        // overlay (Bluetooth devices / VPN list)
        _overlay.SetResourceReference(Border.BackgroundProperty, "PanelBgBrush");
        _overlay.SetResourceReference(Border.BorderBrushProperty, "StrokeBrush");
        var overlayPanel = new DockPanel();
        var close = new Button { Width = 24, Height = 24 };
        close.SetResourceReference(StyleProperty, "IconButton");
        var closeIcon = new Icon { Width = 12, Height = 12 };
        closeIcon.SetResourceReference(Icon.DataProperty, "Icon.Close");
        closeIcon.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        close.Content = closeIcon;
        close.Click += (_, _) => HideOverlay();
        var header = new DockPanel();
        DockPanel.SetDock(close, Dock.Right);
        header.Children.Add(close);
        header.Children.Add(_overlayTitle);
        DockPanel.SetDock(header, Dock.Top);
        overlayPanel.Children.Add(header);
        overlayPanel.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _overlayList, MaxHeight = 118 });
        _overlay.Child = overlayPanel;
        SetRowSpan(_overlay, 4);
        Children.Add(_overlay);

        _s.Radios.Changed += ShowRadios;
        Strings.LanguageChanged += ShowAll;
        ShowAll();
    }

    static Grid SliderRow(FrameworkElement lead, Slider slider, TextBlock value)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        lead.HorizontalAlignment = HorizontalAlignment.Left;
        g.Children.Add(lead);
        SetColumn(slider, 1);
        g.Children.Add(slider);
        value.SetResourceReference(StyleProperty, "TextDim");
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.Typography.NumeralAlignment = FontNumeralAlignment.Tabular;
        SetColumn(value, 2);
        g.Children.Add(value);
        return g;
    }

    // ---- refresh ----

    public async void OnShown()
    {
        if (App.IsSnapshot) return; // snapshots show fixed state, never the real machine
        try
        {
            _s.Audio.Changed -= ShowVolume;
            _s.Audio.Changed += ShowVolume;
            ShowAll();
            await _s.Radios.InitAsync();
            if (await _s.Brightness.RefreshAsync() || _brightRow.Visibility == Visibility.Visible) ShowBrightness();
            RefreshVpn();
        }
        catch (Exception ex) { Logger.Error("control refresh", ex); }
    }

    public void OnHidden()
    {

        _s.Audio.Changed -= ShowVolume;
        HideOverlay();
    }

    void ShowAll()
    {
        ShowRadios();
        ShowVolume();
        _dark.Set(WindowsTheme.IsDark, Strings.T(WindowsTheme.IsDark ? "cc.on" : "cc.off"));
        ShowVpnTile();
    }

    void ShowRadios()
    {
        var r = _s.Radios;
        _wifi.Set(r.WifiOn == true, r.WifiOn is null ? Strings.T(r.Denied ? "cc.denied" : "cc.unavailable") : Strings.T(r.WifiOn.Value ? "cc.on" : "cc.off"), enabled: r.WifiOn is not null);
        var connected = _btDevices.Where(d => d.Connected).Select(d => d.Name).FirstOrDefault();
        _bt.Set(r.BluetoothOn == true,
            r.BluetoothOn is null ? Strings.T(r.Denied ? "cc.denied" : "cc.unavailable") : connected ?? Strings.T(r.BluetoothOn.Value ? "cc.on" : "cc.off"),
            enabled: r.BluetoothOn is not null);
    }

    void ShowVolume()
    {
        var a = _s.Audio;
        _updatingSliders = true;
        _volume.Value = Math.Round(a.Level * 100);
        _updatingSliders = false;
        _volume.IsEnabled = a.Available;
        _volumeText.Text = a.Available ? (a.Muted ? Strings.T("cc.muted") : $"{(int)Math.Round(a.Level * 100)}%") : "—";
        _muteIcon.SetResourceReference(Icon.DataProperty, a.Muted || a.Level <= 0.001 ? "Icon.VolumeMute" : "Icon.Volume");
        _volume.Opacity = a.Muted ? 0.5 : 1;
    }

    void ShowBrightness()
    {
        var b = _s.Brightness;
        _brightRow.Visibility = b.Supported ? Visibility.Visible : Visibility.Collapsed;
        _updatingSliders = true;
        _bright.Value = b.Level;
        _updatingSliders = false;
        _brightText.Text = $"{b.Level}%";
    }

    public void RefreshVpn()
    {
        _vpns = VpnDetector.Detect(_s.VpnEnvironment);
        ShowVpnTile();
        if (_overlayKind == "vpn" && _overlay.IsVisible) FillVpnOverlay();
    }

    void ShowVpnTile()
    {
        var on = _vpns.FirstOrDefault(v => v.Connected);
        _vpn.Set(on is not null, on?.Name ?? Strings.T(_vpns.Count == 0 ? "cc.vpnNone" : "cc.off"));
    }

    // ---- actions ----

    async void ToggleWifi()
    {
        if (_s.Radios.WifiOn is not { } on) return;
        if (!await _s.Radios.SetAsync(RadioKind.WiFi, !on)) ShowMessage(Strings.T("cc.radioRefused"));
    }

    async void ToggleBluetooth()
    {
        if (_s.Radios.BluetoothOn is not { } on) return;
        if (!await _s.Radios.SetAsync(RadioKind.Bluetooth, !on)) ShowMessage(Strings.T("cc.radioRefused"));
    }

    void ToggleDark()
    {
        var dark = !WindowsTheme.IsDark;
        if (WindowsTheme.SetDark(dark)) _dark.Set(dark, Strings.T(dark ? "cc.on" : "cc.off"));
    }

    void ShowMessage(string text) => _message.Text = text;

    // ---- overlay ----

    async void ShowOverlay(string kind)
    {
        if (_overlay.IsVisible && _overlayKind == kind) { HideOverlay(); return; }
        _overlayKind = kind;
        _overlay.Visibility = Visibility.Visible;
        if (kind == "vpn")
        {
            _overlayTitle.Text = Strings.T("cc.vpn");
            FillVpnOverlay();
        }
        else
        {
            _overlayTitle.Text = Strings.T("cc.btDevices");
            _overlayList.Children.Clear();
            _overlayList.Children.Add(Dim(Strings.T("cc.loading")));
            _btDevices = await BluetoothDevices.ListAsync();
            if (_overlayKind == "bt") FillBtOverlay();
            ShowRadios();
        }
    }

    void HideOverlay()
    {
        _overlay.Visibility = Visibility.Collapsed;
        _overlayKind = "";
    }

    static TextBlock Dim(string text)
    {
        var t = new TextBlock { Text = text, Margin = new Thickness(6, 2, 0, 4), TextWrapping = TextWrapping.Wrap };
        t.SetResourceReference(StyleProperty, "TextDim");
        return t;
    }

    FrameworkElement OverlayRow(string icon, string name, string state, string? action, Action? click, bool busy)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4), Height = 34 };
        var i = new Icon { Width = 16, Height = 16, Margin = new Thickness(6, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        i.SetResourceReference(Icon.DataProperty, icon);
        i.SetResourceReference(Icon.ForegroundProperty, "FgDimBrush");
        DockPanel.SetDock(i, Dock.Left);
        row.Children.Add(i);
        if (action is not null && click is not null)
        {
            var b = new Button { Content = busy ? "…" : action, MinHeight = 26, Padding = new Thickness(10, 2, 10, 2), IsEnabled = !busy, VerticalAlignment = VerticalAlignment.Center };
            b.SetResourceReference(StyleProperty, "FlatButton");
            b.Click += (_, _) => click();
            DockPanel.SetDock(b, Dock.Right);
            row.Children.Add(b);
        }
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        var st = new TextBlock { Text = state, FontSize = 11 };
        st.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        texts.Children.Add(st);
        row.Children.Add(texts);
        return row;
    }

    void FillBtOverlay()
    {
        _overlayList.Children.Clear();
        if (_btDevices.Count == 0) { _overlayList.Children.Add(Dim(Strings.T("cc.btNone"))); return; }
        foreach (var d in _btDevices)
        {
            var dev = d;
            var busy = _busy.Contains(d.Id);
            var state = Strings.T(busy ? (d.Connected ? "cc.disconnecting" : "cc.connecting") : d.Connected ? "cc.connected" : "cc.notConnected");
            _overlayList.Children.Add(OverlayRow(d.IsAudio ? "Icon.Headphones" : "Icon.Bluetooth", d.Name, state,
                d.IsAudio ? Strings.T(d.Connected ? "cc.disconnect" : "cc.connect") : null,
                d.IsAudio ? () => ToggleBtDevice(dev) : null, busy));
        }
    }

    async void ToggleBtDevice(BtDevice d)
    {
        if (!_busy.Add(d.Id)) return;
        FillBtOverlay();
        var ok = await BluetoothDevices.SetConnectedAsync(d, !d.Connected);
        if (!ok) ShowMessage(Strings.T("cc.btFailed"));
        // the connection settles a moment later; re-read twice
        await Task.Delay(2500);
        _btDevices = await BluetoothDevices.ListAsync();
        _busy.Remove(d.Id);
        if (_overlayKind == "bt") FillBtOverlay();
        ShowRadios();
    }

    void FillVpnOverlay()
    {
        _overlayList.Children.Clear();
        if (_vpns.Count == 0) { _overlayList.Children.Add(Dim(Strings.T("cc.vpnNoneLong"))); return; }
        foreach (var v in _vpns)
        {
            var vpn = v;
            var busy = _busy.Contains("vpn:" + v.Name);
            string? action = v.CanToggle ? Strings.T(v.Connected ? "cc.disconnect" : "cc.connect") : v.OpenPath is not null ? Strings.T("cc.open") : null;
            Action? click = v.CanToggle ? () => ToggleVpn(vpn) : v.OpenPath is not null ? () => Open(vpn.OpenPath!) : null;
            _overlayList.Children.Add(OverlayRow("Icon.Shield", v.Name, Strings.T(busy ? "cc.working" : v.Connected ? "cc.connected" : "cc.notConnected"), action, click, busy));
        }
    }

    async void ToggleVpn(VpnEntry v)
    {
        var key = "vpn:" + v.Name;
        if (!_busy.Add(key)) return;
        FillVpnOverlay();
        var (ok, admin, _) = await VpnDetector.RunAsync(v.Cli!, v.Connected ? v.DisconnectArgs! : v.ConnectArgs!);
        _busy.Remove(key);
        if (!ok) ShowMessage(Strings.T(admin ? "cc.vpnAdmin" : "cc.vpnFailed", v.Name));
        else ShowMessage("");
        RefreshVpn();
        FillVpnOverlay();
    }

    static void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Logger.Error("open vpn client", ex); }
    }

    /// <summary>Snapshot hook: fixed state without touching the machine.</summary>
    internal void SetForSnapshot(bool wifi, bool bt, bool dark, float volume, int? brightness, List<VpnEntry> vpns, List<BtDevice>? devices = null, string? overlay = null)
    {
        _wifi.Set(wifi, Strings.T(wifi ? "cc.on" : "cc.off"));
        _bt.Set(bt, devices?.FirstOrDefault(d => d.Connected)?.Name ?? Strings.T(bt ? "cc.on" : "cc.off"));
        _dark.Set(dark, Strings.T(dark ? "cc.on" : "cc.off"));
        _vpns = vpns;
        ShowVpnTile();
        _updatingSliders = true;
        _volume.Value = volume * 100;
        _volume.IsEnabled = true;
        _volumeText.Text = $"{(int)(volume * 100)}%";
        _muteIcon.SetResourceReference(Icon.DataProperty, "Icon.Volume");
        if (brightness is { } b) { _brightRow.Visibility = Visibility.Visible; _bright.Value = b; _brightText.Text = $"{b}%"; }
        _updatingSliders = false;
        if (overlay == "vpn") { _overlayKind = "vpn"; _overlay.Visibility = Visibility.Visible; _overlayTitle.Text = Strings.T("cc.vpn"); FillVpnOverlay(); }
        if (overlay == "bt" && devices is not null) { _btDevices = devices; _overlayKind = "bt"; _overlay.Visibility = Visibility.Visible; _overlayTitle.Text = Strings.T("cc.btDevices"); FillBtOverlay(); }
    }

    public void Dispose()
    {
        _s.Radios.Changed -= ShowRadios;
        _s.Audio.Changed -= ShowVolume;
        Strings.LanguageChanged -= ShowAll;
    }

    /// <summary>A square-ish toggle tile: icon, label, state; accent background when on. Optional chevron for a sub-list.</summary>
    sealed class Tile : Grid
    {
        readonly Button _main = new() { Margin = new Thickness(3, 0, 3, 0), Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        readonly Icon _icon = new() { Width = 17, Height = 17, HorizontalAlignment = HorizontalAlignment.Left };
        readonly TextBlock _label = new() { FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        readonly TextBlock _state = new() { FontSize = 10.5, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.75 };
        bool _on;

        public Tile(string icon, string labelKey, Action click, Action? more = null)
        {
            _main.SetResourceReference(StyleProperty, "FlatButton");
            _icon.SetResourceReference(Icon.DataProperty, icon);
            _label.SetBinding(TextBlock.TextProperty, Ui.Loc(labelKey));
            var content = new StackPanel { Margin = new Thickness(10, 6, 10, 6), HorizontalAlignment = HorizontalAlignment.Stretch };
            content.Children.Add(_icon);
            content.Children.Add(_label);
            content.Children.Add(_state);
            _main.Content = content;
            _main.Click += (_, _) => { try { click(); } catch (Exception ex) { Logger.Error("tile", ex); } };
            Children.Add(_main);
            if (more is not null)
            {
                var chevron = new Button { Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 6, 0) };
                chevron.SetResourceReference(StyleProperty, "IconButton");
                var ci = new Icon { Width = 12, Height = 12 };
                ci.SetResourceReference(Icon.DataProperty, "Icon.ChevronDown");
                chevron.Content = ci;
                chevron.SetBinding(ToolTipProperty, Ui.Loc("cc.btDevices"));
                chevron.Click += (_, _) => more();
                Children.Add(chevron);
            }
            Apply();
        }

        public void Set(bool on, string state, bool enabled = true)
        {
            _on = on;
            _state.Text = state;
            _main.IsEnabled = enabled;
            Apply();
        }

        void Apply()
        {
            _main.SetResourceReference(BackgroundProperty, _on ? "AccentBrush" : "SurfaceBrush");
            var fg = _on ? "AccentFgBrush" : "FgBrush";
            _icon.SetResourceReference(Icon.ForegroundProperty, fg);
            _label.SetResourceReference(TextBlock.ForegroundProperty, fg);
            _state.SetResourceReference(TextBlock.ForegroundProperty, fg);
            foreach (var c in Children.OfType<Button>().Where(b => !ReferenceEquals(b, _main)))
                if (c.Content is Icon i) i.SetResourceReference(Icon.ForegroundProperty, fg);
        }
    }
}
