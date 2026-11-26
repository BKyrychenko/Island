using System.Text.Json;
using System.Threading.Channels;
using Skerry.Modules.AiChat;

namespace Skerry.Tests;

public class CodexStreamingTests
{
    sealed class Pipe : TextReader
    {
        readonly Channel<string> _lines = Channel.CreateUnbounded<string>();
        public void Write(object value) => _lines.Writer.TryWrite(JsonSerializer.Serialize(value));
        public override async ValueTask<string?> ReadLineAsync(CancellationToken ct) =>
            await _lines.Reader.ReadAsync(ct);
    }

    static object Item(string id, string text, string phase = "final_answer") => new { type = "agentMessage", id, text, phase };
    static object Event(string method, object parameters) => new { method, @params = parameters };
    static object Update(string method, object item) => Event(method, new { threadId = "thread", turnId = "turn", item });
    static object Delta(string delta, string itemId = "reply") =>
        Event("item/agentMessage/delta", new { threadId = "thread", turnId = "turn", itemId, delta });
    static object End(string status, params object[] items) =>
        Event("turn/completed", new { threadId = "thread", turn = new { id = "turn", status, items } });

    [Fact]
    public async Task DeliversTextBeforeCompletionAndReconcilesTheAuthoritativeAnswerOnce()
    {
        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var pipe = new Pipe();
        await using var stream = CodexAppServer.ReadTurn(pipe, "thread", ct.Token).GetAsyncEnumerator();
        pipe.Write(new { id = 6, result = new { turn = new { id = "turn" } } });
        pipe.Write(Update("item/started", Item("progress", "", "commentary")));
        pipe.Write(Delta("Private progress.", "progress"));
        pipe.Write(Update("item/completed", Item("progress", "Private progress.", "commentary")));
        pipe.Write(Update("item/started", Item("reply", "")));
        pipe.Write(Delta("Перша"));
        Assert.True(await stream.MoveNextAsync());
        Assert.Equal("Перша", stream.Current);
        pipe.Write(Update("item/started", Item("reply", "")));
        pipe.Write(Delta(" частина"));
        Assert.True(await stream.MoveNextAsync());
        Assert.Equal("Перша частина", stream.Current);
        pipe.Write(Update("item/completed", Item("reply", "Кінець.")));
        Assert.True(await stream.MoveNextAsync());
        Assert.Equal("Кінець.", stream.Current);
        pipe.Write(Update("item/completed", Item("reply", "Кінець.")));
        pipe.Write(End("completed", Item("reply", "Кінець.")));
        Assert.False(await stream.MoveNextAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InterruptedOrFailedTurnKeepsPreviouslyDeliveredText(bool cancel)
    {
        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var pipe = new Pipe();
        await using var stream = CodexAppServer.ReadTurn(pipe, "thread", ct.Token).GetAsyncEnumerator();
        pipe.Write(Update("item/started", Item("reply", "")));
        pipe.Write(Delta("Received"));
        Assert.True(await stream.MoveNextAsync());
        Assert.Equal("Received", stream.Current);
        if (cancel)
        {
            ct.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.MoveNextAsync().AsTask());
        }
        else
        {
            pipe.Write(End("failed"));
            Assert.Equal("ai.err.codexRequest", (await Assert.ThrowsAsync<AiException>(() => stream.MoveNextAsync().AsTask())).Key);
        }
    }

    [Fact]
    public async Task SuppressedMessageClearsAnyPreviouslyDisplayedText()
    {
        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var pipe = new Pipe();
        await using var stream = CodexAppServer.ReadTurn(pipe, "thread", ct.Token).GetAsyncEnumerator();
        pipe.Write(Update("item/started", Item("reply", "")));
        pipe.Write(Delta("Partial"));
        Assert.True(await stream.MoveNextAsync());
        var item = new { type = "agentMessage", id = "reply", text = "Withheld", phase = "final_answer",
            moderation = new { moderated = true, display = new { mode = "hide" } } };
        pipe.Write(Update("item/completed", item));
        Assert.True(await stream.MoveNextAsync());
        Assert.Equal("", stream.Current);
        pipe.Write(End("completed", item));
        await Assert.ThrowsAsync<AiException>(() => stream.MoveNextAsync().AsTask());
    }

    [Fact]
    public void OnlyTheSignedInProfilesCanonicalInstructionsArePermitted()
    {
        var profile = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(profile)) profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        var allowed = Path.Combine(profile, "AGENTS.md");
        Assert.True(CodexAppServer.OnlyProfileInstructions(JsonSerializer.SerializeToElement(new[] { allowed })));
        Assert.True(CodexAppServer.OnlyProfileInstructions(JsonSerializer.SerializeToElement(Array.Empty<string>())));
        Assert.False(CodexAppServer.OnlyProfileInstructions(JsonSerializer.SerializeToElement(new[] { Path.Combine(profile, "project", "AGENTS.md") })));
        Assert.False(CodexAppServer.OnlyProfileInstructions(JsonSerializer.SerializeToElement(new[] { "AGENTS.md" })));
    }
}
