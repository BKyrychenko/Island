using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Skerry.Core;
using Skerry.Modules.Lyrics;
using Skerry.Modules.Media;
using MediaState = Skerry.Modules.Media.MediaState;

namespace Skerry.Views.Music;

/// <summary>
/// Music tab: art, title (marquee on hover), artist, source app, transport, progress with seek, and a synced lyrics
/// view. Refreshes ~4×/s only while shown and playing; everything else is event-driven.
/// </summary>
public sealed class MusicView : Grid, ITabView, IDisposable
{
    readonly IMediaSource _media;
    readonly LyricsTracker _lyrics;
    readonly Func<DateTimeOffset> _now;

    readonly Grid _player = new();
    readonly StackPanel _empty = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    readonly ArtImage _art = new(104, 14) { VerticalAlignment = VerticalAlignment.Top };
    readonly MarqueeText _title = new();
    readonly TextBlock _artist = new(), _source = new(), _elapsed = new(), _total = new();
    readonly Slider _progress = new() { Minimum = 0, Maximum = 1, VerticalAlignment = VerticalAlignment.Center };
    Button _prev = null!, _play = null!, _next = null!;
    readonly Icon _playIcon = new() { Width = 18, Height = 18, Filled = true };
    readonly ToggleButton _lyricsToggle = new();
    readonly Grid _info = new();
    readonly Grid _lyricsPanel = new() { Visibility = Visibility.Collapsed, ClipToBounds = true };
    readonly StackPanel _lyricLines = new() { VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock _lyricsMessage = new() { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    readonly Button _retryLyrics = new() { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0), Visibility = Visibility.Collapsed };
    readonly ScrollViewer _plain = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
    readonly TextBlock _plainText = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, LineHeight = 19 };

    DispatcherTimer? _timer;
    bool _shown, _dragging, _updatingProgress;
    int _lyricIndex = -2;
    static bool _lyricsOpen; // remembered while the app runs

    /// <summary>Lyrics view open by default for new views (snapshot hook; the toggle also updates it).</summary>
    internal static bool LyricsOpen { set => _lyricsOpen = value; }

    public MusicView(IMediaSource media, LyricsTracker lyrics, Func<DateTimeOffset>? now = null)
    {
        _media = media;
        _lyrics = lyrics;
        _now = now ?? (() => DateTimeOffset.Now);

        BuildEmpty();
        BuildPlayer();
        Children.Add(_player);
        Children.Add(_empty);

        _media.Changed += OnMediaChanged;
        _lyrics.Updated += OnLyricsUpdated;
        Unloaded += (_, _) => _timer?.Stop();
        _lyricsToggle.IsChecked = _lyricsOpen;
        ApplyLyricsMode();
        OnMediaChanged();
    }

    void BuildEmpty()
    {
        var icon = new Icon { Width = 28, Height = 28, Margin = new Thickness(0, 0, 0, 8) };
        icon.SetResourceReference(Icon.DataProperty, "Icon.Music");
        icon.SetResourceReference(Icon.ForegroundProperty, "FgFaintBrush");
        var title = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center };
        title.SetResourceReference(StyleProperty, "TextTitle");
        title.SetBinding(TextBlock.TextProperty, Ui.Loc("music.nothing"));
        var hint = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        hint.SetResourceReference(StyleProperty, "TextDim");
        hint.SetBinding(TextBlock.TextProperty, Ui.Loc("music.nothingHint"));
        _empty.Children.Add(icon);
        _empty.Children.Add(title);
        _empty.Children.Add(hint);
    }

    void BuildPlayer()
    {
        _player.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
        _player.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        _player.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _player.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        // The compact seek target uses the spare space above the normal 18-DIP slider.
        _player.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 28 });
        _player.Margin = new Thickness(4, 4, 4, 0);

        _player.Children.Add(_art);

        // info column: title / artist / source, transport row at the bottom
        _title.Inner.FontSize = 15;
        _title.Inner.FontWeight = FontWeights.SemiBold;
        _title.Height = 22;
        _artist.SetResourceReference(StyleProperty, "Text");
        _artist.Foreground = null;
        _artist.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        _artist.Margin = new Thickness(0, 1, 0, 0);
        _source.SetResourceReference(StyleProperty, "TextCaption");
        _source.Margin = new Thickness(0, 3, 0, 0);
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        texts.Children.Add(_title);
        texts.Children.Add(_artist);
        texts.Children.Add(_source);

        var transport = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-6, 0, 0, 0) };
        _prev = TransportButton("Icon.Prev", 32, 15, "music.prev");
        _play = TransportButton("Icon.Play", 40, 18, "music.playPause");
        _play.Content = _playIcon;
        _playIcon.SetResourceReference(Icon.DataProperty, "Icon.Play");
        _next = TransportButton("Icon.Next", 32, 15, "music.next");
        _prev.Click += (_, _) => _media.Previous();
        _play.Click += (_, _) => _media.PlayPause();
        _next.Click += (_, _) => _media.Next();
        transport.Children.Add(_prev);
        transport.Children.Add(_play);
        transport.Children.Add(_next);

        _lyricsToggle.SetResourceReference(StyleProperty, "Chip");
        _lyricsToggle.SetBinding(ContentControl.ContentProperty, Ui.Loc("music.lyrics"));
        _lyricsToggle.HorizontalAlignment = HorizontalAlignment.Right;
        _lyricsToggle.VerticalAlignment = VerticalAlignment.Bottom;
        _lyricsToggle.Margin = new Thickness(0, 0, 0, 6);
        _lyricsToggle.Click += (_, _) => { _lyricsOpen = _lyricsToggle.IsChecked == true; ApplyLyricsMode(); };

        _info.Children.Add(texts);
        _info.Margin = new Thickness(0, 0, 0, 40);

        // lyrics panel (same area as the info column)
        _lyricsMessage.SetResourceReference(StyleProperty, "TextDim");
        _lyricsMessage.TextTrimming = TextTrimming.None;
        _retryLyrics.SetBinding(ContentControl.ContentProperty, Ui.Loc("music.lyricsRetry"));
        _retryLyrics.Click += (_, _) => _lyrics.Retry();
        var lyricsStatus = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        lyricsStatus.Children.Add(_lyricsMessage);
        lyricsStatus.Children.Add(_retryLyrics);
        _lyricsPanel.Children.Add(_lyricLines);
        _lyricsPanel.Children.Add(lyricsStatus);
        _plain.Content = _plainText;
        _plainText.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        _lyricsPanel.Children.Add(_plain);
        _lyricsPanel.Margin = new Thickness(0, 0, 0, 40);

        var right = new Grid();
        right.Children.Add(_info);
        right.Children.Add(_lyricsPanel);
        right.Children.Add(transport);
        right.Children.Add(_lyricsToggle);
        SetColumn(right, 2);
        _player.Children.Add(right);

        // progress row
        var progress = new Grid { VerticalAlignment = VerticalAlignment.Bottom };
        _progress.SetResourceReference(MinHeightProperty, "CompactTargetMin");
        progress.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        progress.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progress.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        foreach (var t in new[] { _elapsed, _total })
        {
            t.SetResourceReference(StyleProperty, "TextCaption");
            t.FontWeight = FontWeights.Normal;
            t.FontFamily = (FontFamily)Application.Current.FindResource("UiFont");
            t.Typography.NumeralAlignment = FontNumeralAlignment.Tabular;
        }
        _elapsed.HorizontalAlignment = HorizontalAlignment.Left;
        _total.HorizontalAlignment = HorizontalAlignment.Right;
        SetColumn(_progress, 1);
        SetColumn(_total, 2);
        progress.Children.Add(_elapsed);
        progress.Children.Add(_progress);
        progress.Children.Add(_total);
        SetRow(progress, 1);
        SetColumnSpan(progress, 3);
        _player.Children.Add(progress);

        // seek: click or drag on the bar, applied on release
        _progress.PreviewMouseLeftButtonDown += (_, _) => _dragging = true;
        _progress.PreviewMouseLeftButtonUp += (_, _) => Dispatcher.BeginInvoke(CommitSeek, DispatcherPriority.Input);
        _progress.LostMouseCapture += (_, _) => { if (_dragging) Dispatcher.BeginInvoke(CommitSeek, DispatcherPriority.Input); };
        _progress.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, Ui.Loc("music.progress"));
        _progress.ValueChanged += (_, _) =>
        {
            if (_updatingProgress) return;
            if (_dragging && _media.Current is { } c) _elapsed.Text = PlaybackMath.Format(c.Duration * _progress.Value);
            else Seek();
        };
    }

    Button TransportButton(string iconKey, double size, double iconSize, string tipKey)
    {
        var b = new Button { Width = size, Height = size };
        b.SetResourceReference(StyleProperty, "IconButton");
        var icon = new Icon { Width = iconSize, Height = iconSize, Filled = true };
        icon.SetResourceReference(Icon.DataProperty, iconKey);
        b.Content = icon;
        b.SetBinding(ToolTipProperty, Ui.Loc(tipKey));
        return b;
    }

    void CommitSeek()
    {
        if (!_dragging) return;
        _dragging = false;
        Seek();
        Tick();
    }

    void Seek()
    {
        if (_media.Current is { CanSeek: true } c) _media.Seek(c.Duration * _progress.Value);
    }

    // ---- state ----

    void OnMediaChanged()
    {
        var s = _media.Current;
        _empty.Visibility = s is null ? Visibility.Visible : Visibility.Collapsed;
        _player.Visibility = s is null ? Visibility.Collapsed : Visibility.Visible;
        if (s is null) { UpdateTimer(); return; }

        _art.Set(s);
        _title.Text = s.Title;
        _artist.Text = s.Artist;
        _artist.Visibility = s.Artist.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        _source.Text = s.Source.ToUpperInvariant();
        _playIcon.SetResourceReference(Icon.DataProperty, s.IsPlaying ? "Icon.Pause" : "Icon.Play");
        _prev.IsEnabled = s.CanPrev;
        _next.IsEnabled = s.CanNext;
        _progress.IsEnabled = s.CanSeek;
        _total.Text = s.Duration > TimeSpan.Zero ? PlaybackMath.Format(s.Duration) : "";
        if (_lyricsOpen && _shown) _lyrics.Ensure();
        _lyricIndex = -2;
        RenderLyrics();
        Tick();
        UpdateTimer();
    }

    void OnLyricsUpdated()
    {
        _lyricIndex = -2;
        RenderLyrics();
        Tick();
    }

    void ApplyLyricsMode()
    {
        _info.Visibility = _lyricsOpen ? Visibility.Collapsed : Visibility.Visible;
        _lyricsPanel.Visibility = _lyricsOpen ? Visibility.Visible : Visibility.Collapsed;
        if (_lyricsOpen && _shown) _lyrics.Ensure();
        _lyricIndex = -2;
        RenderLyrics();
        Tick();
    }

    void RenderLyrics()
    {
        if (!_lyricsOpen) return;
        var r = _lyrics.ForCurrent;
        string? message = null;
        var lyricsModuleOff = App.Host.StateOf("lyrics") != ModuleState.Running && App.Island is not null && !App.IsSnapshot;
        if (lyricsModuleOff) message = Strings.T("music.lyricsOff");
        else if (r is null) message = _lyrics.Loading || _media.Current is not null ? Strings.T("music.lyricsLoading") : "";
        else if (r.Status == LyricsStatus.NotFound) message = Strings.T("music.lyricsNotFound");
        else if (r.Status == LyricsStatus.Instrumental) message = Strings.T("music.lyricsInstrumental");
        else if (r.Status == LyricsStatus.Error) message = Strings.T("music.lyricsError");

        _lyricsMessage.Text = message ?? "";
        _lyricsMessage.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        _retryLyrics.Visibility = !lyricsModuleOff && r?.Status == LyricsStatus.Error ? Visibility.Visible : Visibility.Collapsed;
        var plain = message is null && r!.Status == LyricsStatus.Plain;
        _plain.Visibility = plain ? Visibility.Visible : Visibility.Collapsed;
        if (plain) _plainText.Text = r!.Plain;
        _lyricLines.Visibility = message is null && !plain ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Position, progress bar and lyric line from the locally extrapolated position.</summary>
    void Tick()
    {
        if (_media.Current is not { } s) return;
        var pos = s.PositionAt(_now());
        if (!_dragging)
        {
            _elapsed.Text = s.Duration > TimeSpan.Zero ? PlaybackMath.Format(pos) : "";
            _updatingProgress = true;
            try { _progress.Value = s.Duration > TimeSpan.Zero ? pos / s.Duration : 0; }
            finally { _updatingProgress = false; }
        }
        if (_lyricsOpen && _lyrics.ForCurrent is { Status: LyricsStatus.Synced } r) ShowLyricAt(r.Lines, pos);
    }

    void ShowLyricAt(IReadOnlyList<LrcLine> lines, TimeSpan pos)
    {
        var i = Lrc.IndexAt(lines, pos);
        if (i == _lyricIndex) return;
        _lyricIndex = i;
        _lyricLines.Children.Clear();
        // previous line, current line, two next lines
        for (var k = i - 1; k <= i + 2; k++)
        {
            var text = k >= 0 && k < lines.Count ? lines[k].Text : "";
            if (text.Length == 0 && k == i) text = "♪";
            var tb = new TextBlock
            {
                Text = text, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 2),
                FontSize = k == i ? 15 : 12.5, FontWeight = k == i ? FontWeights.SemiBold : FontWeights.Normal,
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, k == i ? "FgBrush" : "FgFaintBrush");
            if (k == i) tb.TextWrapping = TextWrapping.Wrap;
            if (k == i) tb.MaxHeight = 44;
            _lyricLines.Children.Add(tb);
        }
    }

    void UpdateTimer()
    {
        var needed = _shown && _media.Current is { IsPlaying: true };
        if (!needed) { _timer?.Stop(); return; }
        _timer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Tick(), Dispatcher);
        _timer.Start();
    }

    public void OnShown()
    {
        _shown = true;
        if (_lyricsOpen) _lyrics.Ensure();
        Tick();
        UpdateTimer();
    }

    public void OnHidden()
    {
        _shown = false;
        UpdateTimer();
    }

    public void Dispose()
    {
        _timer?.Stop();
        _media.Changed -= OnMediaChanged;
        _lyrics.Updated -= OnLyricsUpdated;
    }
}
