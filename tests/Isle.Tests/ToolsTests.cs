using System.Windows;
using System.Windows.Media;
using Isle.Modules.Tools;
using Isle.Views.Tools;
using ZXing;
using ZXing.Common;

namespace Isle.Tests;

public class ToolsTests
{
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
