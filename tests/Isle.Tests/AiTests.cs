using Isle.Core;
using Isle.Modules.AiChat;

namespace Isle.Tests;

public class AiTests
{
    static Conversation Chat(params (string Role, string Text)[] msgs)
    {
        var c = new Conversation();
        foreach (var (r, t) in msgs) c.Messages.Add(new ChatMessage { Role = r, Text = t });
        return c;
    }

    [Fact]
    public void RequestHistoryAlternatesAndSkipsErrors()
    {
        var c = Chat(("assistant", "hi"), ("user", "a"), ("user", "b"), ("assistant", ""), ("assistant", "x"));
        c.Messages.Add(new ChatMessage { Role = "assistant", Text = "boom", Error = true });
        c.Messages.Add(new ChatMessage { Role = "user", Text = "c" });
        var r = c.ForRequest();
        Assert.Equal(["user", "assistant", "user"], r.Select(m => m.Role));
        Assert.Equal("a\n\nb", r[0].Text);
        Assert.Equal("x", r[1].Text);
    }

    [Theory]
    [InlineData("", AiProviderKind.Codex)]
    [InlineData("codex", AiProviderKind.Codex)]
    [InlineData("openai", AiProviderKind.Codex)]
    [InlineData("anthropic", AiProviderKind.ClaudeCode)]
    [InlineData("claude", AiProviderKind.ClaudeCode)]
    public void ProviderSelectionUsesInstalledClisForExistingProfiles(string saved, AiProviderKind expected)
    {
        var settings = new Settings();
        settings.SetOpt("ai.provider", saved);
        settings.SetOpt("ai.anthropic.key", "unused");
        settings.SetOpt("ai.openai.key", "unused");
        settings.SetOpt("ai.openai.base", "https://example.invalid");
        Assert.Equal(expected, AiProviders.FromSettings(settings));
    }

    [Fact]
    public void StoreKeepsChatsNewestFirstAndSurvivesRestart()
    {
        var file = Path.Combine(Path.GetTempPath(), "isle-chats-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new ChatStore(file);
            var a = store.New();
            a.Messages.Add(new ChatMessage { Text = "First question about weather in Kyiv today and tomorrow please" });
            store.Touch(a);
            var b = store.New("sys");
            b.Messages.Add(new ChatMessage { Text = "\n  Second\nmore" });
            store.Touch(b);
            store.Touch(a);
            Assert.Equal([a.Id, b.Id], store.All.Select(c => c.Id));
            Assert.Equal("Second", b.Title);
            Assert.EndsWith("…", a.Title);
            Assert.True(a.Title.Length <= 48);

            var again = new ChatStore(file);
            Assert.Equal(2, again.All.Count);
            Assert.Equal("sys", again.Get(b.Id)!.SystemPrompt);
            again.Delete(a.Id);
            Assert.Single(new ChatStore(file).All);
        }
        finally { File.Delete(file); }
    }
}
