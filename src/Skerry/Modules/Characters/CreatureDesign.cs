using System.Windows;
using System.Windows.Media;

namespace Skerry.Modules.Characters;

/// <summary>
/// One original character, drawn from simple frozen shapes in a 40×40 design box (feet at y ≈ 37).
/// Names and shapes are invented for Skerry.
/// </summary>
public sealed class CreatureDesign
{
    public required string Id { get; init; }
    public required Geometry Body { get; init; }
    public required Brush BodyBrush { get; init; }
    public Geometry? Extra { get; init; }
    public Brush? ExtraBrush { get; init; }
    /// <summary>Extra drawn over the body (highlight) instead of behind it (stem, antenna).</summary>
    public bool ExtraOnTop { get; init; }
    public required Point LeftEye { get; init; }
    public required Point RightEye { get; init; }
    public double EyeRx { get; init; } = 2.5;
    public double EyeRy { get; init; } = 2.7;
    public double CheekY { get; init; }
    public double CheekDx { get; init; } = 9;
    public Brush CheekBrush { get; init; } = Frozen(Color.FromArgb(0x70, 0xFF, 0x7A, 0x8A));

    public string NameKey => $"chars.{Id}";
    public string TraitKey => $"chars.{Id}.trait";

    public static readonly Brush EyeBrush = Frozen(Color.FromRgb(0x1B, 0x1C, 0x22));
    public static readonly Brush ShineBrush = Frozen(Colors.White);

    public static readonly CreatureDesign Pebb = new()
    {
        // a calm round pebble with rosy cheeks
        Id = "pebb",
        Body = G("M20,8 C30.5,8 36,15.5 36,24 C36,32 29.5,37 20,37 C10.5,37 4,32 4,24 C4,15.5 9.5,8 20,8 Z"),
        BodyBrush = Frozen(Color.FromRgb(0x86, 0xB6, 0xFF)),
        Extra = G("M13,12.5 C15,10.5 18,9.8 20,10 C17.5,11 15.5,12.2 14.2,13.6 Z"),
        ExtraBrush = Frozen(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF)),
        ExtraOnTop = true,
        LeftEye = new Point(15, 22), RightEye = new Point(25, 22),
        CheekY = 27.5, CheekDx = 10.5,
    };

    public static readonly CreatureDesign Sprig = new()
    {
        // a curious sprout: dome body, one leaf on a stem
        Id = "sprig",
        Body = G("M7,36.5 C5.5,25 10,15 20,15 C30,15 34.5,25 33,36.5 C29,37.5 11,37.5 7,36.5 Z"),
        BodyBrush = Frozen(Color.FromRgb(0x74, 0xD8, 0x9C)),
        Extra = G("F1 M19.1,6.5 L20.9,6.5 L20.9,15.5 L19.1,15.5 Z M20.5,8 C21.5,3.5 26.5,2 30.5,3 C29.5,7 25,9.5 20.5,8 Z M19.5,10 C17.5,7 13.5,6.5 11,7.5 C12.5,10.5 16.5,11.5 19.5,10 Z"),
        ExtraBrush = Frozen(Color.FromRgb(0x3C, 0xA8, 0x68)),
        LeftEye = new Point(15.5, 25), RightEye = new Point(24.5, 25),
        EyeRx = 2.3, EyeRy = 2.6,
        CheekY = 30, CheekDx = 9.5,
    };

    public static readonly CreatureDesign Kubo = new()
    {
        // a cheerful little box with an antenna
        Id = "kubo",
        Body = new RectangleGeometry(new Rect(6, 13, 28, 24), 7, 7).Frozen(),
        BodyBrush = Frozen(Color.FromRgb(0xFF, 0xB2, 0x6B)),
        Extra = new GeometryGroup
        {
            FillRule = FillRule.Nonzero,
            Children = { G("M19.2,6.5 L20.8,6.5 L20.8,13.5 L19.2,13.5 Z"), new EllipseGeometry(new Point(20, 5.5), 2.8, 2.8) },
        }.Frozen(),
        ExtraBrush = Frozen(Color.FromRgb(0xFF, 0xDD, 0xB0)),
        LeftEye = new Point(14.5, 24), RightEye = new Point(25.5, 24),
        EyeRx = 2.2, EyeRy = 3.1,
        CheekY = 29.5, CheekDx = 11,
        CheekBrush = Frozen(Color.FromArgb(0x80, 0xFF, 0x6A, 0x55)),
    };

    public static readonly IReadOnlyList<CreatureDesign> All = [Pebb, Sprig, Kubo];

    public static CreatureDesign ById(string? id) => All.FirstOrDefault(d => d.Id == id) ?? Pebb;

    static Geometry G(string data) => Geometry.Parse(data).Frozen();

    static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}

static class FreezeExt
{
    public static T Frozen<T>(this T f) where T : Freezable
    {
        if (f.CanFreeze) f.Freeze();
        return f;
    }
}
