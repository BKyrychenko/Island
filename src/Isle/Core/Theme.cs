using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Isle.Core;

/// <summary>
/// Dark / Light / System palette. Brushes live in Application.Resources under fixed keys and are used through
/// DynamicResource, so <see cref="Apply"/> restyles everything live. Accent can be overridden (album art).
/// </summary>
public static class Theme
{
    public static readonly Color DefaultAccentDark = Color.FromRgb(0x5A, 0xA9, 0xFF);
    public static readonly Color DefaultAccentLight = Color.FromRgb(0x2F, 0x7F, 0xE6);

    static Color? _accentOverride;
    static SolidColorBrush? _accentBrush;

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
        if (!first && dark == IsDark && target is null) return;
        IsDark = dark;

        Color fg = dark ? Colors.White : Color.FromRgb(0x11, 0x11, 0x13);
        Color bg = dark ? Colors.Black : Color.FromRgb(0xF7, 0xF7, 0xF8);
        Set(res, "BgBrush", bg);
        Set(res, "FgBrush", fg);
        Set(res, "FgDimBrush", WithAlpha(fg, 0.60));
        Set(res, "FgFaintBrush", WithAlpha(fg, 0.35));
        Set(res, "SurfaceBrush", WithAlpha(fg, dark ? 0.10 : 0.06));
        Set(res, "SurfaceHoverBrush", WithAlpha(fg, dark ? 0.17 : 0.11));
        Set(res, "SurfacePressedBrush", WithAlpha(fg, dark ? 0.24 : 0.16));
        Set(res, "StrokeBrush", WithAlpha(fg, dark ? 0.14 : 0.12));
        Set(res, "PanelBgBrush", dark ? Color.FromRgb(0x16, 0x16, 0x18) : Color.FromRgb(0xFF, 0xFF, 0xFF)); // settings window / popups
        Set(res, "DangerBrush", dark ? Color.FromRgb(0xFF, 0x6B, 0x6B) : Color.FromRgb(0xD9, 0x3A, 0x3A));
        Set(res, "WarnBrush", dark ? Color.FromRgb(0xFF, 0xA9, 0x4D) : Color.FromRgb(0xD9, 0x7A, 0x10));
        Set(res, "OkBrush", dark ? Color.FromRgb(0x5B, 0xD7, 0x8A) : Color.FromRgb(0x1F, 0x9D, 0x55));

        _accentBrush = new SolidColorBrush(Accent);
        res["AccentBrush"] = _accentBrush;
        Set(res, "AccentFgBrush", Colors.White);
        res["AccentSoftBrush"] = new SolidColorBrush(WithAlpha(Accent, 0.22));

        if (target is null) Changed?.Invoke();
    }

    /// <summary>Overrides the accent (null = default soft blue) with a smooth transition.</summary>
    public static void SetAccent(Color? color)
    {
        if (_accentOverride == color) return;
        _accentOverride = color;
        var res = Application.Current?.Resources;
        if (res is null || _accentBrush is null) return;
        _accentBrush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(Accent, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase() });
        if (res["AccentSoftBrush"] is SolidColorBrush soft && !soft.IsFrozen)
            soft.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(WithAlpha(Accent, 0.22), TimeSpan.FromMilliseconds(400)));
    }

    static void Set(ResourceDictionary res, string key, Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        res[key] = b;
    }

    public static Color WithAlpha(Color c, double a) => Color.FromArgb((byte)Math.Round(255 * a), c.R, c.G, c.B);
}
