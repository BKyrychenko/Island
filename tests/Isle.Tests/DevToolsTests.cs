using Isle.Modules.Commands;

namespace Isle.Tests;

public class DevToolsTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    static IReadOnlyList<DevResult> Eval(string q) => DevTools.Evaluate(q, Now);
    static string? Copy(string q, string kind) => Eval(q).FirstOrDefault(r => r.Kind == kind)?.Copy;

    [Fact]
    public void Base64_round_trips_including_url_safe_and_unpadded()
    {
        Assert.Equal("aGVsbG8gd29ybGQ=", Copy("b64 hello world", "Base64"));
        Assert.Equal("hello world", Copy("b64d aGVsbG8gd29ybGQ=", "Base64 → text"));
        Assert.Equal("hello world", Copy("b64d aGVsbG8gd29ybGQ", "Base64 → text"));
        Assert.Equal("Pz8_", Copy("b64 ???", "Base64url"));
        Assert.Empty(Eval("b64d %%%"));
    }

    [Fact]
    public void Hashes_match_known_vectors()
    {
        Assert.Equal("5d41402abc4b2a76b9719d911017c592", Copy("md5 hello", "MD5"));
        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", Copy("sha256 hello", "SHA-256"));
    }

    [Fact]
    public void Url_encode_and_decode()
    {
        Assert.Equal("a%20b%26c%3D%D1%8F", Copy("url a b&c=я", "URL encode"));
        Assert.Equal("a b&c=я", Copy("urld a+b%26c%3D%D1%8F", "URL decode"));
    }

    [Fact]
    public void Json_is_minified_and_formatted_and_bad_json_gives_nothing()
    {
        Assert.Equal("{\"a\":1,\"b\":[true,null]}", Copy("json { \"a\": 1, \"b\": [ true, null ] }", "JSON, minified"));
        Assert.Contains("\n", Copy("json {\"a\":1}", "JSON, formatted"));
        Assert.Empty(Eval("json {oops"));
    }

    [Fact]
    public void Jwt_is_decoded_with_expiry()
    {
        // {"alg":"HS256","typ":"JWT"}.{"sub":"42","exp":1767225600}
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0MiIsImV4cCI6MTc2NzIyNTYwMH0.sig";
        var r = Eval(jwt);
        Assert.Contains(r, x => x.Kind.StartsWith("JWT payload") && x.Title.Contains("\"sub\":\"42\""));
        Assert.Contains(r, x => x.Kind == "JWT header" && x.Copy.Contains("HS256"));
        Assert.Equal("2026-01-01T00:00:00Z", r.Single(x => x.Kind == "JWT exp").Copy);
    }

    [Fact]
    public void Timestamps_both_ways()
    {
        Assert.Equal("2026-01-01T00:00:00Z", Copy("1767225600", "ISO 8601 UTC"));
        Assert.Equal("2026-01-01T00:00:00.500Z", Copy("1767225600500", "ISO 8601 UTC"));
        Assert.Equal("1767225600", Copy("2026-01-01T00:00:00Z", "Unix, s"));
        Assert.Equal("1795255200", Copy("now", "Unix, s"));
        Assert.Empty(Eval("12345"));
    }

    [Fact]
    public void Colours_convert()
    {
        Assert.Equal("rgb(255, 136, 0)", Copy("#ff8800", "RGB"));
        Assert.Equal("rgb(255, 136, 0)", Copy("#f80", "RGB"));
        Assert.Equal("hsl(32, 100%, 50%)", Copy("#ff8800", "HSL"));
        Assert.Equal("rgba(0, 0, 0, 0.5)", Copy("#00000080", "RGB"));
        Assert.Equal("#ff8800", Copy("rgb(255, 136, 0)", "HEX"));
    }

    [Fact]
    public void Number_bases()
    {
        Assert.Equal("255", Copy("0xff", "Decimal"));
        Assert.Equal("0b101", Copy("0x5", "Binary"));
        Assert.Equal("0xFF", Copy("hex 255", "Hex"));
        Assert.Equal("10", Copy("0b1010", "Decimal"));
        Assert.Equal("0o17", Copy("dec 15", "Octal"));
    }

    [Fact]
    public void Uuid_gives_v4_and_v7()
    {
        var r = Eval("uuid");
        Assert.True(Guid.TryParse(r.Single(x => x.Kind == "UUID v4").Copy, out var v4) && v4.Version == 4);
        Assert.True(Guid.TryParse(r.Single(x => x.Kind == "UUID v7").Copy, out var v7) && v7.Version == 7);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("2+2")]
    [InlineData("b64")]
    [InlineData("notes for tomorrow")]
    public void Ordinary_text_gives_nothing(string q) => Assert.Empty(Eval(q));
}
