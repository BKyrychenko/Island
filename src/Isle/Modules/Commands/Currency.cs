using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Isle.Core;

namespace Isle.Modules.Commands;

public sealed record CurrencyQuery(decimal Amount, string From, string To);

/// <summary>"100 usd to eur", "$20 in uah", "100 євро в гривню", "usd to pln" (amount 1).</summary>
public static partial class CurrencyParser
{
    static readonly Dictionary<string, string> Aliases = Build();

    static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void A(string code, params string[] names) { d[code] = code; foreach (var n in names) d[n] = code; }
        A("USD", "$", "us$", "dollar", "dollars", "buck", "bucks", "долар", "долари", "доларів", "долара", "доларах", "бакс", "бакси", "баксів");
        A("EUR", "€", "euro", "euros", "євро", "евро");
        A("UAH", "₴", "грн", "грн.", "hryvnia", "hryvnias", "hryvnya", "гривня", "гривні", "гривень", "гривню", "гривнях");
        A("GBP", "£", "pound sterling", "sterling", "фунт стерлінгів", "фунтів стерлінгів");
        A("PLN", "zł", "zl", "zloty", "zlotys", "злотий", "злоті", "злотих");
        A("CHF", "franc", "francs", "франк", "франки", "франків");
        A("JPY", "¥", "yen", "єна", "єни", "єн");
        A("CNY", "yuan", "rmb", "юань", "юані", "юанів");
        A("CZK", "kč", "koruna", "крона", "крони", "крон");
        A("CAD", "c$", "cad$");
        A("AUD", "a$");
        A("TRY", "₺", "lira", "lire", "ліра", "ліри", "лір");
        A("SEK"); A("NOK"); A("DKK"); A("HUF"); A("RON"); A("BGN"); A("GEL"); A("MDL"); A("KZT"); A("INR", "₹"); A("ILS", "₪"); A("KRW", "₩"); A("SGD"); A("HKD"); A("NZD"); A("MXN"); A("BRL"); A("ZAR"); A("AED"); A("EGP"); A("THB"); A("IDR"); A("VND"); A("AZN"); A("DZD"); A("BDT"); A("SAR"); A("RSD"); A("TND"); A("LBP"); A("MYR");
        return d;
    }


    [GeneratedRegex(@"^\s*(?<pre>[$€£¥₴₺₹₪₩])?\s*(?<num>\d[\d\s.,]*)?\s*(?<from>[^\d\s][^\d]*?)?\s+(?:to|in|into|=|в|у|на)\s+(?<to>[^\d]+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Query();

    public static CurrencyQuery? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 80) return null;
        var m = Query().Match(input);
        if (!m.Success) return null;
        var fromText = m.Groups["pre"].Success ? m.Groups["pre"].Value : m.Groups["from"].Value.Trim();
        if (m.Groups["pre"].Success && m.Groups["from"].Success && m.Groups["from"].Value.Trim().Length > 0) return null;
        if (!Aliases.TryGetValue(fromText, out var from) || !Aliases.TryGetValue(m.Groups["to"].Value.Trim(), out var to)) return null;
        var amount = 1m;
        if (m.Groups["num"].Success)
        {
            var n = Calculator.Normalize(m.Groups["num"].Value.Trim().Replace(" ", ""));
            if (!decimal.TryParse(n, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)) return null;
        }
        else if (!m.Groups["pre"].Success && fromText.Length == 0) return null;
        return new CurrencyQuery(amount, from, to);
    }
}

/// <summary>Rates as "UAH per 1 unit" (the NBU format); UAH itself is 1.</summary>
public sealed class RatesSnapshot
{
    public DateOnly Date { get; set; }
    public DateTimeOffset Fetched { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal? Convert(decimal amount, string from, string to)
    {
        var a = Rate(from);
        var b = Rate(to);
        return a is null || b is null || b == 0 ? null : amount * a.Value / b.Value;
    }

    decimal? Rate(string code) => code.Equals("UAH", StringComparison.OrdinalIgnoreCase) ? 1m : Rates.TryGetValue(code, out var r) ? r : null;
}

public interface IRatesSource
{
    Task<RatesSnapshot?> FetchAsync(CancellationToken ct);
}

/// <summary>National Bank of Ukraine daily rates (free, no key; all currencies against UAH, so UAH is covered — ECB has no UAH).</summary>
public sealed class NbuRatesSource(IdleHttp http) : IRatesSource
{
    const string Url = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";

    public async Task<RatesSnapshot?> FetchAsync(CancellationToken ct)
    {
        var (status, body) = await http.GetAsync(Url, ct).ConfigureAwait(false);
        return status == 200 ? Parse(body, DateTimeOffset.Now) : null;
    }

    public static RatesSnapshot? Parse(string json, DateTimeOffset now)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var snap = new RatesSnapshot { Fetched = now };
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var cc = e.GetProperty("cc").GetString();
                var rate = e.GetProperty("rate").GetDecimal();
                if (string.IsNullOrEmpty(cc) || rate <= 0) continue;
                snap.Rates[cc] = rate;
                if (snap.Date == default && e.TryGetProperty("exchangedate", out var d) &&
                    DateOnly.TryParseExact(d.GetString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    snap.Date = date;
            }
            return snap.Rates.Count > 0 ? snap : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { return null; }
    }
}

/// <summary>Rates with a 12 h disk cache; offline (or source failing) falls back to whatever is cached, however old.</summary>
public sealed class CurrencyService(IRatesSource source, string cacheFile, Func<DateTimeOffset>? now = null)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(12);
    readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.Now);
    readonly SemaphoreSlim _gate = new(1, 1);
    RatesSnapshot? _mem;

    public async Task<RatesSnapshot?> GetAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _mem ??= LoadCache();
            if (_mem is not null && _now() - _mem.Fetched < MaxAge) return _mem;
            RatesSnapshot? fresh = null;
            try { fresh = await source.FetchAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { Logger.Warn($"rates fetch failed: {ex.GetType().Name}"); }
            if (fresh is null) return _mem;
            _mem = fresh;
            JsonStore.Save(cacheFile, fresh);
            return fresh;
        }
        finally { _gate.Release(); }
    }

    RatesSnapshot? LoadCache()
    {
        if (!File.Exists(cacheFile)) return null;
        var s = JsonStore.Load<RatesSnapshot>(cacheFile);
        return s.Rates.Count > 0 ? s : null;
    }
}
