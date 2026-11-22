using Isle.Modules.Kube;

namespace Isle.Tests;

public class KubeTests
{
    static string? Parse(params string[] lines) => KubeConfig.ParseCurrentContext(lines);

    [Fact]
    public void Parser_reads_top_level_line_only()
    {
        Assert.Equal("dev", Parse("apiVersion: v1", "current-context: dev", "kind: Config"));
        Assert.Null(Parse("contexts:", "  current-context: prod"));
        Assert.Equal("dev", Parse("  current-context: prod", "current-context: dev"));
    }

    [Theory]
    [InlineData("current-context: \"my ctx\"  # c", "my ctx")]
    [InlineData("current-context: 'x'", "x")]
    [InlineData("current-context: prod # old", "prod")]
    [InlineData("current-context: a#b", "a#b")]
    [InlineData("current-context:", null)]
    [InlineData("current-context: # nothing", null)]
    [InlineData("current-context: \"\"", null)]
    public void Parser_strips_quotes_and_comments(string line, string? expected) => Assert.Equal(expected, Parse(line));

    [Fact]
    public void Resolve_first_file_that_sets_it_wins_and_missing_files_are_skipped()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string F(string n, string? text) { var p = Path.Combine(dir, n); if (text is not null) File.WriteAllText(p, text); return p; }
            var missing = F("none", null);
            var empty = F("empty", "current-context: \"\"\n");
            var a = F("a", "current-context: alpha\n");
            var b = F("b", "current-context: beta\n");
            Assert.Equal("alpha", KubeConfig.Resolve([missing, empty, a, b]));
            Assert.Equal("beta", KubeConfig.Resolve([b, a]));
            Assert.Null(KubeConfig.Resolve([missing, empty]));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Paths_use_KUBECONFIG_list_else_default()
    {
        Assert.Equal([@"C:\a", @"D:\b"], KubeConfig.Paths(@"C:\a; D:\b;;", @"C:\u"));
        Assert.Equal([@"C:\u\.kube\config"], KubeConfig.Paths(null, @"C:\u"));
        Assert.Equal([@"C:\u\.kube\config"], KubeConfig.Paths(" ; ", @"C:\u"));
    }

    [Fact]
    public void Pattern_matches_case_insensitively_and_falls_back_when_invalid()
    {
        var p = KubeConfig.BuildPattern(null, out var fb);
        Assert.False(fb);
        Assert.True(KubeConfig.IsProd("gke_acme_PROD-eu", p));
        Assert.False(KubeConfig.IsProd("minikube", p));
        Assert.False(KubeConfig.IsProd(null, p));
        var bad = KubeConfig.BuildPattern("(unclosed", out fb);
        Assert.True(fb);
        Assert.True(KubeConfig.IsProd("live", bad));
        Assert.True(KubeConfig.IsProd("staging-x", KubeConfig.BuildPattern("^staging", out _)));
    }
}
