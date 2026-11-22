using System.Text.Json.Serialization;

namespace Isle.Modules.Usage;

/// <summary>Token counts of one bucket. Cache writes are split by TTL because they are priced differently.</summary>
public sealed class Tokens
{
    public long In { get; set; }
    public long Out { get; set; }
    public long CacheRead { get; set; }
    public long Write5m { get; set; }
    public long Write1h { get; set; }

    [JsonIgnore] public long CacheWrite => Write5m + Write1h;
    [JsonIgnore] public long Total => In + Out + CacheRead + CacheWrite;

    public void Add(Tokens t)
    {
        In += t.In; Out += t.Out; CacheRead += t.CacheRead; Write5m += t.Write5m; Write1h += t.Write1h;
    }
}

/// <summary>USD per million tokens.</summary>
public readonly record struct Price(decimal In, decimal Out, decimal Write5m, decimal Write1h, decimal Read);

public static class UsagePrices
{
    // ponytail: prices are static (Anthropic API list prices, checked 2026-11) and must be updated by hand when
    // Anthropic changes pricing or ships a model. Longest matching id prefix wins; an unknown model has no price
    // and its cost shows as "—". Haiku 5.5 is listed at its <=100K-prompt rate (the long-prompt rate is 5x).
    static readonly (string Prefix, Price Price)[] Table =
    [
        ("claude-fable-5-2", new(8, 40, 10, 16, 0.20m)),
        ("claude-mythos-5-2", new(8, 40, 10, 16, 0.20m)),
        ("claude-fable-5-1", new(10, 50, 12.5m, 20, 0.25m)),
        ("claude-mythos-5-1", new(10, 50, 12.5m, 20, 0.25m)),
        ("claude-fable-5", new(10, 50, 12.5m, 20, 1.00m)),
        ("claude-mythos-5", new(10, 50, 12.5m, 20, 1.00m)),
        ("claude-opus-5-5", new(4, 20, 5, 8, 0.20m)),
        ("claude-opus-5", new(5, 25, 6.25m, 10, 0.50m)),
        ("claude-opus-4-8", new(5, 25, 6.25m, 10, 0.50m)),
        ("claude-opus-4-7", new(5, 25, 6.25m, 10, 0.50m)),
        ("claude-opus-4-6", new(5, 25, 6.25m, 10, 0.50m)),
        ("claude-sonnet-5-5", new(2, 10, 2.5m, 4, 0.10m)),
        ("claude-sonnet-5", new(2, 10, 2.5m, 4, 0.20m)),
        ("claude-sonnet-4-6", new(3, 15, 3.75m, 6, 0.30m)),
        ("claude-haiku-5-5", new(0.10m, 0.50m, 0.125m, 0.20m, 0.01m)),
        ("claude-haiku-4-5", new(1, 5, 1.25m, 2, 0.10m)),
    ];

    public static Price? Find(string model)
    {
        Price? best = null;
        var len = 0;
        foreach (var (prefix, price) in Table)
            if (prefix.Length > len && model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { best = price; len = prefix.Length; }
        return best;
    }

    /// <summary>API-equivalent cost in USD, or null when the model has no known price.</summary>
    public static decimal? Cost(string model, Tokens t) =>
        Find(model) is { } p
            ? (t.In * p.In + t.Out * p.Out + t.Write5m * p.Write5m + t.Write1h * p.Write1h + t.CacheRead * p.Read) / 1_000_000m
            : null;

    /// <summary>Sum over models; null as soon as one model with tokens is unpriced (a partial sum would mislead).</summary>
    public static decimal? Cost(IEnumerable<KeyValuePair<string, Tokens>> byModel)
    {
        decimal sum = 0;
        foreach (var (model, t) in byModel)
        {
            if (t.Total == 0) continue;
            if (Cost(model, t) is not { } c) return null;
            sum += c;
        }
        return sum;
    }
}
