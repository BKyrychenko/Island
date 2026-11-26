using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Skerry.Views.Voice;

/// <summary>
/// Hold-to-talk pill: red dot + mic, a scrolling level waveform and the elapsed time while recording;
/// mic + "Transcribing…" afterwards. <see cref="PushLevel"/> shifts in one bar (called ~20×/s while recording).
/// </summary>
public sealed class VoicePill : Grid
{
    public const int BarCount = 18;
    readonly ScaleTransform[] _bars = new ScaleTransform[BarCount];
    readonly float[] _levels = new float[BarCount];
    readonly StackPanel _wave = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Height = 18, Margin = new Thickness(10, 0, 0, 0) };
    readonly TextBlock _text = new() { FontSize = 12.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(10, 0, 0, 0) };
    readonly TextBlock _time = new() { FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
    readonly Ellipse _dot = new() { Width = 8, Height = 8, Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center };

    public VoicePill()
    {
        _dot.SetResourceReference(Shape.FillProperty, "DangerBrush");
        _time.SetResourceReference(TextBlock.ForegroundProperty, "FgDimBrush");
        var icon = new Icon { Width = 15, Height = 15, VerticalAlignment = VerticalAlignment.Center };
        icon.SetResourceReference(Icon.DataProperty, "Icon.Mic");
        icon.SetResourceReference(Icon.ForegroundProperty, "FgBrush");
        for (var i = 0; i < BarCount; i++)
        {
            var bar = new Rectangle { Width = 3, Height = 18, RadiusX = 1.5, RadiusY = 1.5, Margin = new Thickness(i == 0 ? 0 : 2, 0, 0, 0), RenderTransformOrigin = new Point(0.5, 0.5) };
            bar.SetResourceReference(Shape.FillProperty, "AccentBrush");
            bar.RenderTransform = _bars[i] = new ScaleTransform(1, 0.12);
            _wave.Children.Add(bar);
        }
        var row = new DockPanel { Margin = new Thickness(14, 0, 14, 0), LastChildFill = true };
        DockPanel.SetDock(_dot, Dock.Left);
        DockPanel.SetDock(icon, Dock.Left);
        DockPanel.SetDock(_time, Dock.Right);
        DockPanel.SetDock(_wave, Dock.Left);
        row.Children.Add(_dot);
        row.Children.Add(icon);
        row.Children.Add(_time);
        row.Children.Add(_wave);
        row.Children.Add(_text);
        Children.Add(row);
        SetRecording();
    }

    public void SetRecording()
    {
        _dot.Visibility = _wave.Visibility = _time.Visibility = Visibility.Visible;
        _text.Visibility = Visibility.Collapsed;
    }

    public void SetBusy(string text)
    {
        _dot.Visibility = _wave.Visibility = _time.Visibility = Visibility.Collapsed;
        _text.Visibility = Visibility.Visible;
        _text.Text = text;
    }

    public void SetElapsed(string text) => _time.Text = text;

    public void PushLevel(float level)
    {
        Array.Copy(_levels, 1, _levels, 0, BarCount - 1);
        _levels[^1] = Math.Clamp(level, 0, 1);
        for (var i = 0; i < BarCount; i++) _bars[i].ScaleY = 0.12 + 0.88 * _levels[i];
    }

    /// <summary>For snapshots: a fixed speech-like waveform.</summary>
    public void Demo()
    {
        float[] demo = [0.1f, 0.2f, 0.45f, 0.7f, 0.55f, 0.8f, 0.95f, 0.6f, 0.4f, 0.65f, 0.85f, 0.5f, 0.3f, 0.55f, 0.75f, 0.4f, 0.2f, 0.1f];
        foreach (var l in demo) PushLevel(l);
        SetElapsed("0:04");
    }
}
