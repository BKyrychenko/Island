using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using ZXing;
using ZXing.Common;

namespace Isle.Modules.Tools;

/// <summary>Barcode/QR decoding with ZXing.Net (pure managed, no native parts).</summary>
public static class QrReader
{
    public static string? Decode(BitmapSource image)
    {
        var bgra = image.Format == PixelFormats.Bgra32 ? image : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        return Decode(pixels, bgra.PixelWidth, bgra.PixelHeight);
    }

    public static string? Decode(byte[] bgra, int width, int height)
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions { TryHarder = true, TryInverted = true },
        };
        var result = reader.Decode(new RGBLuminanceSource(bgra, width, height, RGBLuminanceSource.BitmapFormat.BGRA32));
        return result?.Text;
    }

    public static bool IsWebUrl(string text) =>
        Uri.TryCreate(text.Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}

/// <summary>Windows.Media.Ocr with the installed recognizer languages; the engine is created per call (cheap) and not kept.</summary>
public static class OcrReader
{
    public static IReadOnlyList<(string Tag, string Name)> Languages()
    {
        try { return OcrEngine.AvailableRecognizerLanguages.Select(l => (l.LanguageTag, l.NativeName)).ToList(); }
        catch { return []; }
    }

    public static async Task<string?> RecognizeAsync(BitmapSource image, string? languageTag)
    {
        OcrEngine? engine = null;
        if (!string.IsNullOrEmpty(languageTag))
        {
            try { engine = OcrEngine.TryCreateFromLanguage(new Language(languageTag)); } catch { engine = null; }
        }
        engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null) return null;

        // small selections read better enlarged; huge ones must fit the engine limit
        var max = (double)OcrEngine.MaxImageDimension;
        var scale = Math.Min(image.PixelWidth, image.PixelHeight) < 300 ? 2.0 : 1.0;
        scale = Math.Min(scale, Math.Min(max / image.PixelWidth, max / image.PixelHeight));
        BitmapSource src = scale == 1.0 ? image : new TransformedBitmap(image, new ScaleTransform(scale, scale));
        var bgra = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        using var sb = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bgra.PixelWidth, bgra.PixelHeight, BitmapAlphaMode.Premultiplied);
        sb.CopyFromBuffer(pixels.AsBuffer());
        var result = await engine.RecognizeAsync(sb);
        var text = string.Join(Environment.NewLine, result.Lines.Select(l => l.Text)).Trim();
        return text;
    }
}
