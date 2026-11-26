using System.Windows;
using System.Windows.Media;
using Skerry.Modules.Tools;
using Skerry.Views.Tools;
using ZXing;
using ZXing.Common;

namespace Skerry.Tests;

public class ToolsTests
{
    [Theory]
    [InlineData(-10, -20, 50, 60, 0, 0, 40, 40)]
    [InlineData(80, 60, 90, 90, 80, 60, 20, 20)]
    [InlineData(110, 0, 20, 20, 0, 0, 0, 0)]
    public void Selection_clips_drag_to_source_bitmap(int x, int y, int width, int height, int cx, int cy, int cw, int ch)
    {
        FakeSink.Run(_ =>
        {
            Selection? result = null;
            var overlay = NewSelector(s => result = s);
            var type = overlay.GetType();
            type.GetMethod("SetSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(overlay, [new PxRect(x, y, width, height)]);
            var clipped = (PxRect)type.GetField("_current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(overlay)!;
            Assert.Equal(new PxRect(cx, cy, cw, ch), clipped);
            type.GetMethod("Complete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(overlay, [new PxRect(x, y, width, height)]);
            if (cw == 0) Assert.Null(result);
            else
            {
                Assert.NotNull(result);
                Assert.Equal(new PxRect(cx - 100, cy + 200, cw, ch), result.Rect);
                Assert.Equal(cw, result.Image.PixelWidth);
                Assert.Equal(ch, result.Image.PixelHeight);
            }
            type.GetMethod("CloseQuietly")!.Invoke(overlay, null);
        });
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void Selector_system_close_cancels_but_session_cleanup_does_not(bool quietly, int expected)
    {
        FakeSink.Run(_ =>
        {
            var cancelled = 0;
            var overlay = NewSelector(s => { Assert.Null(s); ++cancelled; });
            if (quietly) overlay.GetType().GetMethod("CloseQuietly")!.Invoke(overlay, null);
            else overlay.Close();
            Assert.Equal(expected, cancelled);
        });
    }

    static Window NewSelector(Action<Selection?> done)
    {
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(100, 80, 96, 96, PixelFormats.Bgra32, null, new byte[100 * 80 * 4], 400);
        var monitor = new Skerry.Core.ScreenInfo(IntPtr.Zero, -100, 200, 100, 80, true, 1, "synthetic");
        return (Window)Activator.CreateInstance(typeof(SelectionSession).GetNestedType("OverlayWindow", System.Reflection.BindingFlags.NonPublic)!,
            monitor, bitmap, SelectMode.Region, new List<PxRect>(), done)!;
    }

    [Fact]
    public void Screenshot_release_keeps_recording_capture_exclusion()
    {
        var island = (IslandWindow)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(IslandWindow));
        var count = typeof(IslandWindow).GetField("_captureExclusions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        island.ExcludeFromCapture(true); // recording
        island.ExcludeFromCapture(true); // screenshot
        island.ExcludeFromCapture(false);
        Assert.Equal(1, count.GetValue(island));
        island.ExcludeFromCapture(false);
        Assert.Equal(0, count.GetValue(island));
    }

    static byte[] QrPixels(string text, int size, out int w, out int h)
    {
        var matrix = new MultiFormatWriter().encode(text, BarcodeFormat.QR_CODE, size, size, new Dictionary<EncodeHintType, object> { [EncodeHintType.MARGIN] = 2, [EncodeHintType.CHARACTER_SET] = "UTF-8" });
        w = matrix.Width;
        h = matrix.Height;
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = matrix[x, y] ? (byte)0 : (byte)255;
                var i = (y * w + x) * 4;
                px[i] = px[i + 1] = px[i + 2] = v;
                px[i + 3] = 255;
            }
        return px;
    }

    [Theory]
    [InlineData("https://example.com/path?q=1")]
    [InlineData("WIFI:T:WPA;S:home;P:secret;;")]
    [InlineData("Привіт, світе")]
    public void Qr_roundtrip(string text)
    {
        var px = QrPixels(text, 240, out var w, out var h);
        Assert.Equal(text, QrReader.Decode(px, w, h));
    }

    [Fact]
    public void Qr_inverted_and_none()
    {
        var px = QrPixels("inverse", 200, out var w, out var h);
        for (var i = 0; i < px.Length; i++) if (i % 4 != 3) px[i] = (byte)(255 - px[i]);
        Assert.Equal("inverse", QrReader.Decode(px, w, h));
        var blank = Enumerable.Repeat((byte)255, 100 * 100 * 4).ToArray();
        Assert.Null(QrReader.Decode(blank, 100, 100));
    }

    [Theory]
    [InlineData("https://a.b/c", true)]
    [InlineData("http://x.org", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("file:///C:/Windows/notepad.exe", false)]
    [InlineData("just text", false)]
    public void Only_web_links_can_be_opened(string text, bool url) => Assert.Equal(url, QrReader.IsWebUrl(text));

    [Fact]
    public void Rect_from_points_and_intersection()
    {
        Assert.Equal(new PxRect(10, 20, 30, 40), PxRect.FromPoints(40, 60, 10, 20));
        var a = new PxRect(0, 0, 100, 100);
        Assert.Equal(new PxRect(50, 60, 50, 40), a.Intersect(new PxRect(50, 60, 200, 200)));
        Assert.True(a.Intersect(new PxRect(200, 200, 5, 5)).IsEmpty);
        Assert.True(a.Contains(0, 0));
        Assert.False(a.Contains(100, 50));
    }

    [Fact]
    public void Colour_hex() => Assert.Equal("#0A80FF", ColorText.Hex(Color.FromRgb(10, 128, 255)));

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Recording_save_waits_for_encoder_and_closed_output(int bytes, bool encoderFails)
    {
        var path = Path.GetTempFileName();
        try
        {
            var encoder = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var closed = false;
            var save = Recorder.FinishFileAsync(encoder.Task, path, () =>
            {
                File.WriteAllBytes(path, new byte[bytes]);
                closed = true;
            });
            Assert.False(save.IsCompleted);
            Assert.False(closed);
            if (encoderFails) encoder.SetException(new InvalidOperationException("encoder failed"));
            else encoder.SetResult();

            if (encoderFails) await Assert.ThrowsAsync<InvalidOperationException>(() => save);
            else if (bytes == 0) await Assert.ThrowsAsync<IOException>(() => save);
            else await save;
            Assert.True(closed);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Arrow_head_points_at_the_end()
    {
        var g = typeof(MarkupWindow).GetMethod("ArrowGeometry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [new Point(0, 0), new Point(100, 0), 4.0]) as Geometry;
        Assert.NotNull(g);
        var b = g!.Bounds;
        Assert.Equal(0, b.Left, 3);
        Assert.Equal(100, b.Right, 3);
        Assert.True(b.Height > 8); // the head is wider than the line
    }
}
