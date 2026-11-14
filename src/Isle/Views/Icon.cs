using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Isle.Views;

/// <summary>
/// Draws one of Isle's line icons (24×24 design grid, see Styles/Icons.xaml) at any size with a consistent
/// stroke. <c>&lt;v:Icon Data="{StaticResource Icon.Music}" Width="16" Height="16"/&gt;</c>
/// </summary>
public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(Icon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(Icon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FilledProperty = DependencyProperty.Register(
        nameof(Filled), typeof(bool), typeof(Icon), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(double), typeof(Icon), new FrameworkPropertyMetadata(1.7, FrameworkPropertyMetadataOptions.AffectsRender));

    public Geometry? Data { get => (Geometry?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public bool Filled { get => (bool)GetValue(FilledProperty); set => SetValue(FilledProperty, value); }
    /// <summary>Stroke thickness in design-grid units.</summary>
    public double Stroke { get => (double)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public Icon()
    {
        Width = 16;
        Height = 16;
        IsHitTestVisible = false;
        SnapsToDevicePixels = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var data = Data;
        if (data is null) return;
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var k = size / 24.0;
        dc.PushTransform(new MatrixTransform(k, 0, 0, k, (ActualWidth - size) / 2, (ActualHeight - size) / 2));
        if (Filled)
        {
            dc.DrawGeometry(Foreground, null, data);
        }
        else
        {
            // keep the stroke readable at small sizes: never thinner than ~1.2 device-independent px
            var thickness = Math.Max(Stroke, 1.2 / k);
            var pen = new Pen(Foreground, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            dc.DrawGeometry(null, pen, data);
        }
        dc.Pop();
    }
}
