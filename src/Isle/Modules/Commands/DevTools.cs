using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Isle.Core;

namespace Isle.Modules.Commands;

/// <summary>One developer-tool answer: what to show and what Enter copies.</summary>
public sealed record DevResult(string Title, string Kind, string Copy);

/// <summary>
/// Small developer conversions for the command bar. Explicit prefixes ("b64 …", "sha256 …", "json …", "uuid")
/// and things recognised on sight (JWTs, unix timestamps, ISO dates, #colours, 0x/0b/0o numbers).
/// Pure functions of the input (and the clock for "now"/"uuid"), so they are unit tested directly.
/// </summary>
public static partial class DevTools
{
    [GeneratedRegex(@"^eyJ[\w-]*\.[\w-]+\.[\w-]*$")]
    private static partial Regex Jwt();

    [GeneratedRegex(@"^#(?<h>[0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$", RegexOptions.IgnoreCase)]
    private static partial Regex HexColor();

    [GeneratedRegex(@"^rgba?\(\s*(?<r>\d{1,3})\s*,\s*(?<g>\d{1,3})\s*,\s*(?<b>\d{1,3})\s*(?:,\s*(?<a>[\d.]+)\s*)?\)$", RegexOptions.IgnoreCase)]
    private static partial Regex RgbColor();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?$")]
    private static partial Regex IsoDate();

    static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static IReadOnlyList<DevResult> Evaluate(string text, DateTimeOffset now)
    {
        var q = text.Trim();
        if (q.Length == 0) return [];
        var space = q.IndexOf(' ');
        var verb = (space < 0 ? q : q[..space]).ToLowerInvariant();
        var arg = space < 0 ? "" : q[(space + 1)..].Trim();
        try
        {
            if (Prefixed(verb, arg, now) is { Count: > 0 } explicitResults) return explicitResults;
            return Recognised(q);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException or OverflowException or DecoderFallbackException)
        {
            return [];
        }
    }

    static List<DevResult> Prefixed(string verb, string arg, DateTimeOffset now)
    {
        var r = new List<DevResult>();
        switch (verb)
        {
            case "uuid" or "guid" when arg.Length == 0:
                var g = Guid.NewGuid();
                r.Add(new(g.ToString(), "UUID v4", g.ToString()));
                var v7 = Guid.CreateVersion7(now);
                r.Add(new(v7.ToString(), "UUID v7", v7.ToString()));
                break;
            case "now" or "unix" or "ts" or "timestamp" when arg.Length == 0:
                var s = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
                var ms = now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
                r.Add(new(s, "Unix, s", s));
                r.Add(new(ms, "Unix, ms", ms));
                r.Add(new(now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture), "ISO 8601 UTC", now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));
                break;
            case "b64" or "base64" when arg.Length > 0:
                var enc = Convert.ToBase64String(Encoding.UTF8.GetBytes(arg));
                r.Add(new(enc, "Base64", enc));
                var url = enc.TrimEnd('=').Replace('+', '-').Replace('/', '_');
                if (url != enc) r.Add(new(url, "Base64url", url));
                break;
            case "b64d" or "unb64" or "base64d" when arg.Length > 0:
                if (DecodeBase64(arg) is { } dec) r.Add(new(dec, "Base64 → text", dec));
                break;
            case "url" or "urlencode" when arg.Length > 0:
                var u = Uri.EscapeDataString(arg);
                r.Add(new(u, "URL encode", u));
                break;
            case "urld" or "urldecode" when arg.Length > 0:
                var d = Uri.UnescapeDataString(arg.Replace('+', ' '));
                r.Add(new(d, "URL decode", d));
                break;
            case "md5" or "sha1" or "sha256" or "sha512" when arg.Length > 0:
                var bytes = Encoding.UTF8.GetBytes(arg);
                var hash = Convert.ToHexStringLower(verb switch
                {
                    "md5" => MD5.HashData(bytes),
                    "sha1" => SHA1.HashData(bytes),
                    "sha256" => SHA256.HashData(bytes),
                    _ => SHA512.HashData(bytes),
                });
                r.Add(new(hash, verb.ToUpperInvariant().Replace("SHA", "SHA-"), hash));
                break;
            case "json" when arg.Length > 0:
                var node = JsonNode.Parse(arg);
                var pretty = node?.ToJsonString(Pretty) ?? "null";
                var compact = node?.ToJsonString(Compact) ?? "null";
                r.Add(new(compact, "JSON, minified", compact));
                r.Add(new(Shorten(pretty.Replace("\r", "").Replace("\n", " ⏎ ")), "JSON, formatted", pretty));
                break;
            case "hex" or "bin" or "oct" or "dec" when arg.Length > 0:
                if (ParseInteger(arg) is { } n) r.AddRange(Bases(n));
                break;
        }
        return r;
    }

    static List<DevResult> Recognised(string q)
    {
        var r = new List<DevResult>();
        if (Jwt().IsMatch(q)) r.AddRange(DecodeJwt(q));
        else if (q.All(char.IsAsciiDigit) && q.Length is 10 or 13) r.AddRange(FromUnix(long.Parse(q, CultureInfo.InvariantCulture), q.Length == 13));
        else if (IsoDate().IsMatch(q) && DateTimeOffset.TryParse(q, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            var s = date.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            r.Add(new(s, "Unix, s", s));
        }
        else if (HexColor().Match(q) is { Success: true } hm) r.AddRange(FromHexColor(hm.Groups["h"].Value));
        else if (RgbColor().Match(q) is { Success: true } rm)
        {
            int c(string g) => Math.Clamp(int.Parse(rm.Groups[g].Value, CultureInfo.InvariantCulture), 0, 255);
            var hex = $"#{c("r"):x2}{c("g"):x2}{c("b"):x2}";
            r.Add(new(hex, "HEX", hex));
        }
        else if ((q.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || q.StartsWith("0b", StringComparison.OrdinalIgnoreCase) || q.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) && ParseInteger(q) is { } n)
            r.AddRange(Bases(n));
        return r;
    }

    static IEnumerable<DevResult> DecodeJwt(string token)
    {
        var parts = token.Split('.');
        var header = DecodeBase64(parts[0]);
        var payload = DecodeBase64(parts[1]);
        if (header is null || payload is null) yield break;
        var node = JsonNode.Parse(payload) as JsonObject;
        var compact = node?.ToJsonString(Compact) ?? payload;
        yield return new(Shorten(compact), "JWT payload (signature not checked)", node?.ToJsonString(Pretty) ?? payload);
        yield return new(Shorten(header), "JWT header", header);
        foreach (var claim in new[] { "exp", "iat", "nbf" })
            if (node?[claim] is JsonValue v && v.TryGetValue(out long secs))
            {
                var when = DateTimeOffset.FromUnixTimeSeconds(secs);
                var shown = when.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                yield return new(shown, $"JWT {claim}", when.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
            }
    }

    static IEnumerable<DevResult> FromUnix(long value, bool millis)
    {
        var when = millis ? DateTimeOffset.FromUnixTimeMilliseconds(value) : DateTimeOffset.FromUnixTimeSeconds(value);
        var local = when.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var utc = when.UtcDateTime.ToString(millis ? "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" : "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        yield return new(local, millis ? "Unix ms → local time" : "Unix s → local time", local);
        yield return new(utc, "ISO 8601 UTC", utc);
    }

    static IEnumerable<DevResult> FromHexColor(string h)
    {
        if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
        int p(int i) => int.Parse(h.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var (r, g, b) = (p(0), p(2), p(4));
        var rgb = h.Length == 8 ? $"rgba({r}, {g}, {b}, {Math.Round(p(6) / 255.0, 2).ToString(CultureInfo.InvariantCulture)})" : $"rgb({r}, {g}, {b})";
        yield return new(rgb, "RGB", rgb);
        var hsl = Hsl(r, g, b);
        yield return new(hsl, "HSL", hsl);
    }

    internal static string Hsl(int r8, int g8, int b8)
    {
        double r = r8 / 255.0, g = g8 / 255.0, b = b8 / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), l = (max + min) / 2, h = 0, s = 0;
        var d = max - min;
        if (d > 0)
        {
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            h = max == r ? (g - b) / d + (g < b ? 6 : 0) : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
            h *= 60;
        }
        return $"hsl({Math.Round(h)}, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";
    }

    static IEnumerable<DevResult> Bases(long n)
    {
        var dec = n.ToString(CultureInfo.InvariantCulture);
        var hex = "0x" + n.ToString("X", CultureInfo.InvariantCulture);
        var bin = "0b" + Convert.ToString(n, 2);
        var oct = "0o" + Convert.ToString(n, 8);
        yield return new(dec, "Decimal", dec);
        yield return new(hex, "Hex", hex);
        yield return new(bin, "Binary", bin);
        yield return new(oct, "Octal", oct);
    }

    internal static long? ParseInteger(string s)
    {
        s = s.Replace("_", "").Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(s[2..], 16);
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(s[2..], 2);
            if (s.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(s[2..], 8);
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException) { return null; }
    }

    /// <summary>Standard or URL-safe Base64, padding optional; null unless it decodes to valid UTF-8.</summary>
    internal static string? DecodeBase64(string s)
    {
        s = s.Trim().Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        try
        {
            var bytes = Convert.FromBase64String(s);
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException) { return null; }
    }

    static string Shorten(string s) => s.Length <= 120 ? s : s[..117] + "…";
}

/// <summary>Command-bar front end for <see cref="DevTools"/>: Enter copies the value.</summary>
public sealed class DevToolsProvider(Action<string> copy, Func<DateTimeOffset>? clock = null) : ICommandProvider
{
    public Task<IEnumerable<CommandItem>> QueryAsync(string text, CancellationToken ct)
    {
        var results = DevTools.Evaluate(text, (clock ?? (() => DateTimeOffset.Now))());
        IEnumerable<CommandItem> items = results.Select((r, i) => new CommandItem
        {
            Title = r.Title,
            Subtitle = r.Kind + " · " + Strings.T("cmd.copyResult"),
            IconKey = "Icon.Code",
            Score = 4700 - i,
            Run = () => copy(r.Copy),
        });
        return Task.FromResult(items);
    }
}
