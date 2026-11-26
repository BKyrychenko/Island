using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Skerry.Modules.Commands;

/// <summary>
/// Safe expression evaluator (no code execution): + − × ÷ ^ %, parentheses, sqrt/sin/cos/tan/log/ln/abs/round/floor/ceil,
/// pi/e, implicit multiplication ("2pi", "3(4+1)"), thousands separators ("1,000,000", "1 000"), decimal comma ("3,5"),
/// "15% of 80", and unit conversion ("10 km to mi", "100 f in c", "5 GB to MB", "3 кг в фунти").
/// </summary>
public static partial class Calculator
{
    public sealed record Result(double Value, string Text, string? Unit = null);

    /// <summary>Evaluates a calculator query. Plain numbers without any operation return false (nothing to calculate).</summary>
    public static bool TryEvaluate(string input, out Result result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 300) return false;
        var s = input.Trim().TrimStart('=').Trim();
        if (Units.TryConvert(s, out var value, out var unit))
        {
            result = new Result(value, Format(value) + " " + unit, unit);
            return true;
        }
        s = PercentOf().Replace(s, "$1% * ");
        try
        {
            var p = new Parser(Normalize(s));
            var v = p.ParseAll();
            if (!p.DidWork || double.IsNaN(v) || double.IsInfinity(v)) return false;
            result = new Result(v, Format(v));
            return true;
        }
        catch (FormatException) { return false; }
    }

    // Keep the percentage in the existing expression's parentheses and precedence.
    [GeneratedRegex(@"([\d.,\s]+)%\s*(?:of|від|з)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex PercentOf();

    [GeneratedRegex(@"(?<=\d)[   ](?=\d{3}(?!\d))")]
    private static partial Regex SpaceThousands();

    [GeneratedRegex(@"\d{1,3}(?:,\d{3})+(?![\d,])(?:\.\d+)?")]
    private static partial Regex CommaThousands();

    /// <summary>Thousands separators out, decimal comma → point, unicode operators → ASCII, unbalanced "(" closed.</summary>
    internal static string Normalize(string s)
    {
        s = s.Replace('×', '*').Replace('·', '*').Replace('÷', '/').Replace('−', '-').Replace('–', '-');
        s = SpaceThousands().Replace(s, "");
        // Commas separate arguments in multi-argument functions; elsewhere they are numeric separators.
        var arguments = new Stack<bool>();
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                var end = i;
                while (end > 0 && char.IsWhiteSpace(s[end - 1])) end--;
                var start = end;
                while (start > 0 && char.IsLetter(s[start - 1])) start--;
                arguments.Push(s[start..end].ToLowerInvariant() is "round" or "log" or "min" or "max");
            }
            else if (s[i] == ')' && arguments.Count > 0) arguments.Pop();
            if (char.IsDigit(s[i]) && (arguments.Count == 0 || !arguments.Peek()))
            {
                var start = i;
                while (i + 1 < s.Length && (char.IsDigit(s[i + 1]) || s[i + 1] == '.' ||
                    (s[i + 1] == ',' && char.IsDigit(s[i]) && i + 2 < s.Length && char.IsDigit(s[i + 2])))) i++;
                var number = CommaThousands().Replace(s[start..(i + 1)], m => m.Value.Replace(",", ""));
                sb.Append(number.Replace(',', '.'));
            }
            else sb.Append(s[i]);
        }
        var open = sb.ToString().Count(c => c == '(') - sb.ToString().Count(c => c == ')');
        for (var i = 0; i < open; i++) sb.Append(')');
        return sb.ToString();
    }

    public static string Format(double v, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        if (v == 0) return "0";
        var a = Math.Abs(v);
        if (a >= 1e15 || a < 1e-6) return v.ToString("G10", culture);
        var rounded = Math.Round(v, Math.Max(0, 10 - (int)Math.Floor(Math.Log10(a)) - 1));
        return rounded.ToString("#,##0.##########", culture);
    }

    sealed class Parser(string s)
    {
        int _pos;
        public bool DidWork { get; private set; }

        public double ParseAll()
        {
            var v = Expr();
            Skip();
            if (_pos != s.Length) throw new FormatException();
            return v;
        }

        double Expr()
        {
            var v = Term();
            while (true)
            {
                Skip();
                if (Eat('+')) { v += Term(); DidWork = true; }
                else if (Eat('-')) { v -= Term(); DidWork = true; }
                else return v;
            }
        }

        double Term()
        {
            var v = Unary();
            while (true)
            {
                Skip();
                if (Eat('*')) { v *= Unary(); DidWork = true; }
                else if (Eat('/')) { v /= Unary(); DidWork = true; }
                else if (_pos < s.Length && (s[_pos] == '(' || char.IsLetter(s[_pos]) || s[_pos] == 'π')) { v *= Unary(); DidWork = true; } // implicit
                else return v;
            }
        }

        double Unary()
        {
            Skip();
            if (Eat('-')) return -Unary();
            if (Eat('+')) return Unary();
            return Power();
        }

        double Power()
        {
            var v = Postfix();
            Skip();
            if (Eat('^') || EatStr("**")) { DidWork = true; return Math.Pow(v, Unary()); }
            return v;
        }

        double Postfix()
        {
            var v = Primary();
            while (true)
            {
                Skip();
                if (Eat('%')) { v /= 100; DidWork = true; }
                else if (Eat('!'))
                {
                    if (v < 0 || v > 170 || v != Math.Floor(v)) throw new FormatException();
                    var f = 1.0;
                    for (var i = 2; i <= (int)v; i++) f *= i;
                    v = f;
                    DidWork = true;
                }
                else return v;
            }
        }

        double Primary()
        {
            Skip();
            if (_pos >= s.Length) throw new FormatException();
            if (Eat('('))
            {
                var v = Expr();
                Skip();
                if (!Eat(')')) throw new FormatException();
                return v;
            }
            var c = s[_pos];
            if (char.IsDigit(c) || c == '.')
            {
                var start = _pos;
                while (_pos < s.Length && (char.IsDigit(s[_pos]) || s[_pos] == '.')) _pos++;
                if (_pos < s.Length && (s[_pos] == 'e' || s[_pos] == 'E') && _pos + 1 < s.Length && (char.IsDigit(s[_pos + 1]) || ((s[_pos + 1] == '-' || s[_pos + 1] == '+') && _pos + 2 < s.Length && char.IsDigit(s[_pos + 2]))))
                {
                    _pos += 2;
                    while (_pos < s.Length && char.IsDigit(s[_pos])) _pos++;
                }
                if (!double.TryParse(s.AsSpan(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) throw new FormatException();
                return n;
            }
            if (c == 'π') { _pos++; return Math.PI; }
            if (char.IsLetter(c))
            {
                var start = _pos;
                while (_pos < s.Length && char.IsLetter(s[_pos])) _pos++;
                var name = s[start.._pos].ToLowerInvariant();
                switch (name)
                {
                    case "pi": return Math.PI;
                    case "e": return Math.E;
                    case "tau": return Math.Tau;
                }
                Skip();
                if (!Eat('(')) throw new FormatException();
                var args = new List<double> { Expr() };
                Skip();
                while (Eat(',')) { args.Add(Expr()); Skip(); }
                if (!Eat(')')) throw new FormatException();
                DidWork = true;
                return Call(name, args);
            }
            throw new FormatException();
        }

        static double Call(string name, List<double> a) => (name, a.Count) switch
        {
            ("sqrt", 1) => Math.Sqrt(a[0]),
            ("cbrt", 1) => Math.Cbrt(a[0]),
            ("sin", 1) => Math.Sin(a[0]),
            ("cos", 1) => Math.Cos(a[0]),
            ("tan", 1) => Math.Tan(a[0]),
            ("asin", 1) => Math.Asin(a[0]),
            ("acos", 1) => Math.Acos(a[0]),
            ("atan", 1) => Math.Atan(a[0]),
            ("log", 1) => Math.Log10(a[0]),
            ("log", 2) => Math.Log(a[0], a[1]),
            ("ln", 1) => Math.Log(a[0]),
            ("exp", 1) => Math.Exp(a[0]),
            ("abs", 1) => Math.Abs(a[0]),
            ("round", 1) => Math.Round(a[0], MidpointRounding.AwayFromZero),
            ("round", 2) => Math.Round(a[0], (int)Math.Clamp(a[1], 0, 15), MidpointRounding.AwayFromZero),
            ("floor", 1) => Math.Floor(a[0]),
            ("ceil", 1) => Math.Ceiling(a[0]),
            ("min", _) => a.Min(),
            ("max", _) => a.Max(),
            _ => throw new FormatException(),
        };

        void Skip() { while (_pos < s.Length && char.IsWhiteSpace(s[_pos])) _pos++; }
        bool Eat(char c) { if (_pos < s.Length && s[_pos] == c) { _pos++; return true; } return false; }
        bool EatStr(string t) { if (string.CompareOrdinal(s, _pos, t, 0, t.Length) == 0) { _pos += t.Length; return true; } return false; }
    }
}

/// <summary>Unit conversion for length, mass, temperature and data size ("10 km to mi", "5 гб в мб").</summary>
public static partial class Units
{
    enum Kind { Length, Mass, Temp, Data }

    sealed record U(string Symbol, Kind Kind, double Factor);

    static readonly Dictionary<string, U> Map = Build();

    static Dictionary<string, U> Build()
    {
        var d = new Dictionary<string, U>(StringComparer.OrdinalIgnoreCase);
        void Add(string symbol, Kind k, double f, params string[] aliases)
        {
            var u = new U(symbol, k, f);
            d[symbol] = u;
            foreach (var a in aliases) d[a] = u;
        }
        Add("mm", Kind.Length, 0.001, "millimeter", "millimeters", "millimetre", "millimetres", "мм");
        Add("cm", Kind.Length, 0.01, "centimeter", "centimeters", "centimetre", "centimetres", "см");
        Add("m", Kind.Length, 1, "meter", "meters", "metre", "metres", "м", "метр", "метри", "метрів");
        Add("km", Kind.Length, 1000, "kilometer", "kilometers", "kilometre", "kilometres", "км", "кілометр", "кілометри", "кілометрів");
        Add("in", Kind.Length, 0.0254, "inch", "inches", "\"", "дюйм", "дюйми", "дюймів");
        Add("ft", Kind.Length, 0.3048, "foot", "feet", "'", "фут", "фути", "футів");
        Add("yd", Kind.Length, 0.9144, "yard", "yards", "ярд", "ярди", "ярдів");
        Add("mi", Kind.Length, 1609.344, "mile", "miles", "миля", "милі", "миль");
        Add("nmi", Kind.Length, 1852, "nautical mile", "nautical miles");
        Add("mg", Kind.Mass, 0.001, "milligram", "milligrams", "мг");
        Add("g", Kind.Mass, 1, "gram", "grams", "г", "грам", "грами", "грамів");
        Add("kg", Kind.Mass, 1000, "kilogram", "kilograms", "kilo", "kilos", "кг", "кілограм", "кілограми", "кілограмів");
        Add("t", Kind.Mass, 1_000_000, "tonne", "tonnes", "ton", "tons", "т", "тонна", "тонни", "тонн");
        Add("oz", Kind.Mass, 28.349523125, "ounce", "ounces", "унція", "унції", "унцій");
        Add("lb", Kind.Mass, 453.59237, "lbs", "pound", "pounds", "фунт", "фунти", "фунтів");
        Add("st", Kind.Mass, 6350.29318, "stone", "stones");
        Add("°C", Kind.Temp, 0, "c", "celsius", "°c", "ºc", "цельсій", "цельсія", "°с", "с");
        Add("°F", Kind.Temp, 1, "f", "fahrenheit", "°f", "ºf", "фаренгейт", "фаренгейта");
        Add("K", Kind.Temp, 2, "k", "kelvin", "kelvins", "кельвін", "кельвіна");
        Add("B", Kind.Data, 1, "byte", "bytes", "б", "байт", "байти", "байтів");
        Add("KB", Kind.Data, 1e3, "kilobyte", "kilobytes", "кб");
        Add("MB", Kind.Data, 1e6, "megabyte", "megabytes", "мб");
        Add("GB", Kind.Data, 1e9, "gigabyte", "gigabytes", "гб");
        Add("TB", Kind.Data, 1e12, "terabyte", "terabytes", "тб");
        Add("PB", Kind.Data, 1e15, "petabyte", "petabytes", "пб");
        Add("KiB", Kind.Data, 1024, "kib");
        Add("MiB", Kind.Data, 1024d * 1024, "mib");
        Add("GiB", Kind.Data, 1024d * 1024 * 1024, "gib");
        Add("TiB", Kind.Data, 1024d * 1024 * 1024 * 1024, "tib");
        return d;
    }

    [GeneratedRegex(@"^\s*(-?[\d\s.,]*\d)\s*([^\d\s][^\s]*(?:\s(?:mile|miles))?)\s+(?:to|in|into|as|в|у|до|=)\s+(.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Query();

    public static bool TryConvert(string input, out double value, out string unit)
    {
        value = 0;
        unit = "";
        var m = Query().Match(input);
        if (!m.Success) return false;
        if (!Map.TryGetValue(m.Groups[2].Value.Trim(), out var from) || !Map.TryGetValue(m.Groups[3].Value.Trim(), out var to) || from.Kind != to.Kind) return false;
        var numText = Calculator.Normalize(m.Groups[1].Value.Replace(" ", ""));
        if (!double.TryParse(numText, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) return false;
        value = from.Kind == Kind.Temp ? ToTemp(FromTemp(n, from.Factor), to.Factor) : n * from.Factor / to.Factor;
        unit = to.Symbol;
        return true;
    }

    // temperature "factor" is an id: 0 = °C, 1 = °F, 2 = K
    static double FromTemp(double v, double id) => id switch { 1 => (v - 32) * 5 / 9, 2 => v - 273.15, _ => v };
    static double ToTemp(double c, double id) => id switch { 1 => c * 9 / 5 + 32, 2 => c + 273.15, _ => c };
}
