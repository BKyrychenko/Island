using System.Windows.Media;
using Isle.Core;

namespace Isle.Tests;

public class ThemeContrastTests
{
    [Fact]
    public void Accent_text_and_button_labels_remain_readable_across_rgb_palette()
    {
        foreach (var red in new byte[] { 0, 51, 102, 153, 204, 255 })
        foreach (var green in new byte[] { 0, 51, 102, 153, 204, 255 })
        foreach (var blue in new byte[] { 0, 51, 102, 153, 204, 255 })
        {
            var fill = Color.FromRgb(red, green, blue);
            Assert.True(Theme.Contrast(Theme.ContrastForeground(fill), fill) >= 4.5);
            foreach (var background in new[] { Colors.Black, Color.FromRgb(247, 247, 248) })
                Assert.True(Theme.Contrast(Theme.ReadableText(fill, background), background) >= 4.5);
        }
    }
}
