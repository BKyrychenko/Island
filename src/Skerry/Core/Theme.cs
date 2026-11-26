using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Skerry.Core;

/// <summary>
/// Dark / Light / System palette. Brushes live in Application.Resources under fixed keys and are used through
/// DynamicResource, so <see cref="Apply"/> restyles everything live. Accent can be overridden (album art).
/// </summary>
public static class Theme
{
    public static readonly Color DefaultAccentDark = Color.FromRgb(0x5A, 0xA9, 0xFF);
    public static readonly Color DefaultAccentLight = Color.FromRgb(0x2F, 0x7F, 0xE6);

    static Color? _accentOverride;
    static bool _highContrast;

    public static bool IsDark { get; private set; } = true;
    public static event Action? Changed;

    public static Color Accent => _accentOverride ?? (IsDark ? DefaultAccentDark : DefaultAccentLight);

    /// <summary>True when Windows is set to a light app theme.</summary>
    public static bool SystemUsesLightApps()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v != 0;
        }
        catch { return false; }
    }

    public static bool ResolveDark(string setting) => setting switch
    {
        "Dark" => true,
        "Light" => false,
        _ => !SystemUsesLightApps(),
    };

    /// <summary>Re-reads the setting (and Windows theme for "System") and swaps the palette. UI thread only.</summary>
    public static void Apply(string? setting = null, ResourceDictionary? target = null)
    {
        var res = target ?? Application.Current?.Resources;
        if (res is null) return;
        var dark = ResolveDark(setting ?? Settings.Current.Theme);
        var first = !res.Contains("BgBrush");
        if (!first && dark == IsDark && _highContrast == SystemParameters.HighContrast && target is null) return;
        _highContrast = SystemParameters.HighContrast;
        IsDark = dark;

        Color fg = dark ? Colors.White : Color.FromRgb(0x11, 0x11, 0x13);
        Color bg = dark ? Colors.Black : Color.FromRgb(0xF7, 0xF7, 0xF8);
        Set(res, "BgBrush", bg);
        Set(res, "FgBrush", fg);
        Set(res, "FgDimBrush", WithAlpha(fg, 0.60));
        Set(res, "FgFaintBrush", WithAlpha(fg, dark ? 0.58 : 0.64));
        Set(res, "SurfaceBrush", WithAlpha(fg, dark ? 0.10 : 0.06));
        Set(res, "SurfaceHoverBrush", WithAlpha(fg, dark ? 0.17 : 0.11));
        Set(res, "SurfacePressedBrush", WithAlpha(fg, dark ? 0.24 : 0.16));
        Set(res, "StrokeBrush", WithAlpha(fg, dark ? 0.14 : 0.40));
        Set(res, "PanelBgBrush", dark ? Color.FromRgb(0x16, 0x16, 0x18) : Color.FromRgb(0xFF, 0xFF, 0xFF)); // settings window / popups
        Set(res, "DangerBrush", dark ? Color.FromRgb(0xFF, 0x6B, 0x6B) : Color.FromRgb(0xC4, 0x2B, 0x2B));
        Set(res, "WarnBrush", dark ? Color.FromRgb(0xFF, 0xA9, 0x4D) : Color.FromRgb(0xA8, 0x5A, 0x00));
        Set(res, "OkBrush", dark ? Color.FromRgb(0x5B, 0xD7, 0x8A) : Color.FromRgb(0x16, 0x7A, 0x41));

        if (_highContrast)
        {
            foreach (var name in new[] { "BgBrush", "PanelBgBrush", "SurfaceBrush", "SurfaceHoverBrush", "SurfacePressedBrush" }) Set(res, name, SystemColors.WindowColor);
            foreach (var name in new[] { "FgBrush", "FgDimBrush", "FgFaintBrush", "StrokeBrush", "DangerBrush", "WarnBrush", "OkBrush" }) Set(res, name, SystemColors.WindowTextColor);
        }
        ApplyAccent(res);

        if (target is null) Changed?.Invoke();
    }

    /// <summary>Updates accent fills and their matching foregrounds together.</summary>
    public static void SetAccent(Color? color)
    {
        if (_accentOverride == color) return;
        _accentOverride = color;
        var res = Application.Current?.Resources;
        if (res is null) return;
        ApplyAccent(res);
    }

    static void ApplyAccent(ResourceDictionary res)
    {
        var fill = _highContrast ? SystemColors.HighlightColor : Accent;
        // Text and small icons also appear on hover/pressed surfaces.
        var bg = IsDark ? Color.FromRgb(61, 61, 61) : Color.FromRgb(192, 192, 193);
        Set(res, "AccentBrush", fill);
        Set(res, "AccentFgBrush", _highContrast ? SystemColors.HighlightTextColor : ContrastForeground(fill));
        Set(res, "AccentTextBrush", _highContrast ? SystemColors.WindowTextColor : ReadableText(fill, bg));
        Set(res, "AccentSoftBrush", WithAlpha(fill, 0.22));
    }

    public static double Contrast(Color a, Color b)
    {
        static double Luminance(Color c)
        {
            static double Channel(byte value) { var n = value / 255.0; return n <= 0.04045 ? n / 12.92 : Math.Pow((n + 0.055) / 1.055, 2.4); }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }
        var x = Luminance(a); var y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    public static Color ContrastForeground(Color fill) => Contrast(Colors.Black, fill) >= Contrast(Colors.White, fill) ? Colors.Black : Colors.White;

    public static Color ReadableText(Color accent, Color background)
    {
        var target = ContrastForeground(background);
        for (var i = 0; i <= 100; i++)
        {
            var t = i / 100.0;
            var color = Color.FromRgb((byte)Math.Round(accent.R + (target.R - accent.R) * t), (byte)Math.Round(accent.G + (target.G - accent.G) * t), (byte)Math.Round(accent.B + (target.B - accent.B) * t));
            if (Contrast(color, background) >= 4.5) return color;
        }
        return target;
    }

    static void Set(ResourceDictionary res, string key, Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        res[key] = b;
    }

    public static Color WithAlpha(Color c, double a) => Color.FromArgb((byte)Math.Round(255 * a), c.R, c.G, c.B);
}
