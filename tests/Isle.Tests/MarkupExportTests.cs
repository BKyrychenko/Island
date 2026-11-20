using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Isle.Core;
using Isle.Modules.Tools;
using Isle.Views.Tools;

namespace Isle.Tests;

public class MarkupExportTests
{
    [Theory]
    [InlineData("markup.save", "render", false)]
    [InlineData("markup.save", "save", false)]
    [InlineData("Ctrl+S", "save", false)]
    [InlineData("markup.copy", "image", false)]
    [InlineData("Ctrl+C", "image", false)]
    [InlineData("markup.copyPath", "save", false)]
    [InlineData("markup.copyPath", "path", false)]
    [InlineData("markup.copyPath", "path", true)]
    public void Failed_export_keeps_annotations_and_can_retry(string command, string failure, bool undoBeforeRetry)
    {
        FakeSink.Run(_ =>
        {
            var path = System.IO.Path.GetTempFileName();
            try
            {
                var image = BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgra32, null, new byte[16 * 16 * 4], 64);
                var selection = new Selection(image, new PxRect(0, 0, 16, 16), new ScreenInfo(IntPtr.Zero, 0, 0, 800, 600, true, 1, "synthetic"));
                var fail = true;
                var fileWrites = 0;
                string? copiedPath = null;
                BitmapSource? copiedImage = null;
                var editor = new MarkupWindow(selection,
                    value =>
                    {
                        if (fail && failure == "path") throw new InvalidOperationException("clipboard busy");
                        copiedPath = value;
                    },
                    value =>
                    {
                        if (fail && failure == "image") throw new InvalidOperationException("clipboard busy");
                        copiedImage = value;
                    },
                    value =>
                    {
                        if (fail && failure == "save") throw new IOException("disk full");
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(value));
                        using var stream = File.Create(path);
                        encoder.Save(stream);
                        fileWrites++;
                        return path;
                    });
                var closed = false;
                editor.Closed += (_, _) => closed = true;
                var root = (StackPanel)((Border)editor.Content).Child;
                var canvas = (Canvas)((Viewbox)((Border)root.Children[0]).Child).Child;
                var right = (StackPanel)((DockPanel)root.Children[1]).Children[1];
                var status = (TextBlock)root.Children[2];
                var mark = new Rectangle { Width = 8, Height = 8, Fill = Brushes.Red };
                typeof(MarkupWindow).GetMethod("Push", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [mark]);

                void Invoke()
                {
                    if (command.StartsWith("Ctrl+")) Assert.True(editor.HandleKey(command == "Ctrl+C" ? Key.C : Key.S, ModifierKeys.Control));
                    else right.Children.OfType<Button>().Single(b => Equals(b.Content, Strings.T(command))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }

                if (failure == "render") canvas.Width = 0;
                Invoke();
                Assert.False(closed);
                Assert.Contains(mark, canvas.Children.Cast<UIElement>());
                Assert.Null(copiedPath);
                Assert.Null(copiedImage);
                Assert.Equal(Visibility.Visible, status.Visibility);
                Assert.Equal(Strings.T(failure switch { "image" => "markup.copyFailed", "path" => "markup.copyPathFailed", _ => "markup.saveFailed" }), status.Text);

                fail = false;
                canvas.Width = 16;
                if (undoBeforeRetry) Assert.True(editor.HandleKey(Key.Z, ModifierKeys.Control));
                Invoke();
                Assert.True(closed);
                Assert.Equal(command == "markup.copyPath" ? path : null, copiedPath);
                if (command is "markup.copy" or "Ctrl+C") Assert.Equal(0, fileWrites);
                else Assert.Equal(undoBeforeRetry ? 2 : 1, fileWrites);
                using var saved = File.OpenRead(path);
                var exported = copiedImage ?? new PngBitmapDecoder(saved, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
                var pixel = new byte[4];
                new FormatConvertedBitmap(exported, PixelFormats.Bgra32, null, 0).CopyPixels(new Int32Rect(2, 2, 1, 1), pixel, 4, 0);
                Assert.Equal(undoBeforeRetry ? new byte[4] : new byte[] { 0, 0, 255, 255 }, pixel);
            }
            finally { File.Delete(path); }
        });
    }
}
