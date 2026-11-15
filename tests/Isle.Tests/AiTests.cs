using System.Net;
using System.Net.Http;
using System.Text;
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
    [InlineData("data: {\"choices\":[{\"delta\":{\"content\":\"Hel\"}}]}", "Hel", false)]
    [InlineData("data:{\"choices\":[{\"delta\":{\"role\":\"assistant\"}}]}", null, false)]
    [InlineData("data: [DONE]", null, true)]
    [InlineData(": keep-alive", null, false)]
    [InlineData("", null, false)]
    [InlineData("event: message", null, false)]
    [InlineData("data: not json", null, false)]
    public void ParsesOpenAiSse(string line, string? text, bool done)
    {
        var ev = OpenAiProvider.ParseLine(line);
        Assert.Equal(text, ev.Text);
        Assert.Equal(done, ev.Done);
    }

    [Fact]
    public void OpenAiSseErrorIsReported()
    {
        var ev = OpenAiProvider.ParseLine("data: {\"error\":{\"message\":\"quota\"}}");
        Assert.Equal("quota", ev.Error);
    }

    [Fact]
    public void OpenAiBodyHasSystemImagesAndDocuments()
    {
        var c = Chat(("user", "what is this?"));
        c.SystemPrompt = "be brief";
        c.Messages[0].Attachments.Add(new ChatAttachment { Kind = AttachmentKind.Image, MediaType = "image/png", Data = "AAA", Name = "a.png" });
        c.Messages[0].Attachments.Add(new ChatAttachment { Kind = AttachmentKind.Text, Name = "notes.txt", Text = "line" });
        var json = OpenAiProvider.BuildBody(c, "gpt-x").ToJsonString();
        Assert.Contains("\"stream\":true", json);
        Assert.Contains("\"role\":\"system\",\"content\":\"be brief\"", json);
        Assert.Contains("data:image/png;base64,AAA", json);
        Assert.Contains("notes.txt", json);
        Assert.Contains("\\nline\\n", json);
        Assert.Contains("what is this?", json);
    }

    [Theory]
    [InlineData(401, "ai.err.auth")]
    [InlineData(403, "ai.err.auth")]
    [InlineData(404, "ai.err.model")]
    [InlineData(429, "ai.err.rate")]
    [InlineData(529, "ai.err.overloaded")]
    [InlineData(500, "ai.err.server")]
    [InlineData(400, "ai.err.request")]
    public void StatusMapsToReadableKey(int status, string key)
    {
        Assert.Equal(key, AiException.KeyForStatus(status));
        Assert.True(Strings.English.ContainsKey(key));
        Assert.True(Strings.Ukrainian.ContainsKey(key));
    }

    [Fact]
    public void ErrorDetailIsShortServerMessage()
    {
        Assert.Equal("Incorrect API key", OpenAiProvider.ErrorDetail("{\"error\":{\"message\":\"Incorrect API key\",\"type\":\"x\"}}"));
        Assert.Equal("", OpenAiProvider.ErrorDetail("<html>"));
        Assert.EndsWith("…", OpenAiProvider.ErrorDetail("{\"error\":\"" + new string('x', 400) + "\"}"));
    }

    sealed class FakeHandler(HttpStatusCode status, string body, string contentType) : HttpMessageHandler
    {
        public string? LastBody;
        public HttpRequestMessage? Last;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };
        }
    }

    static async Task<string> Collect(IChatProvider p, Conversation c)
    {
        var sb = new StringBuilder();
        await foreach (var t in p.StreamAsync(c, default)) sb.Append(t);
        return sb.ToString();
    }

    [Fact]
    public async Task OpenAiStreamsDeltas()
    {
        var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\ndata: {\"choices\":[{\"delta\":{\"content\":\", world\"}}]}\n\ndata: [DONE]\n\n";
        var h = new FakeHandler(HttpStatusCode.OK, sse, "text/event-stream");
        var p = new OpenAiProvider(new HttpClient(h), new AiConfig(AiProviderKind.OpenAi, "m", "https://example.test/v1/", "sk-test"));
        Assert.Equal("Hello, world", await Collect(p, Chat(("user", "hi"))));
        Assert.Equal("https://example.test/v1/chat/completions", h.Last!.RequestUri!.ToString());
        Assert.Equal("Bearer", h.Last.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task OpenAiHttpErrorBecomesReadable()
    {
        var h = new FakeHandler(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"bad key\"}}", "application/json");
        var p = new OpenAiProvider(new HttpClient(h), new AiConfig(AiProviderKind.OpenAi, "m", "https://example.test/v1", "k"));
        var ex = await Assert.ThrowsAsync<AiException>(() => Collect(p, Chat(("user", "hi"))));
        Assert.Equal("ai.err.auth", ex.Key);
        Assert.Equal("bad key", ex.Detail);
    }

    const string AnthropicSse = """
        event: message_start
        data: {"type":"message_start","message":{"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5-5","content":[],"stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":5,"output_tokens":1}}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hi"}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" there"}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"output_tokens":3}}

        event: message_stop
        data: {"type":"message_stop"}


        """;

    [Fact]
    public async Task AnthropicStreamsThroughSdk()
    {
        var h = new FakeHandler(HttpStatusCode.OK, AnthropicSse.Replace("\r\n", "\n"), "text/event-stream");
        var c = Chat(("user", "hello"));
        c.SystemPrompt = "short answers";
        c.Messages[0].Attachments.Add(new ChatAttachment { Kind = AttachmentKind.Pdf, Name = "a.pdf", Data = "JVBERi0=" });
        var p = new AnthropicProvider(new AiConfig(AiProviderKind.Anthropic, "claude-opus-5-5", "", "sk-ant-test"), new HttpClient(h));
        Assert.Equal("Hi there", await Collect(p, c));
        var body = h.LastBody!;
        Assert.Contains("\"model\":\"claude-opus-5-5\"", body);
        Assert.Contains("\"stream\":true", body);
        Assert.Contains("\"system\":\"short answers\"", body);
        Assert.Contains("\"fallbacks\":\"default\"", body);
        Assert.Contains("\"type\":\"document\"", body);
        Assert.Contains("application/pdf", body);
        var headers = string.Join("; ", h.Last!.Headers.Select(x => x.Key + "=" + string.Join(",", x.Value)));
        Assert.Contains("server-side-fallback-2026-07-01", headers);
        Assert.Contains("sk-ant-test", headers);
    }

    [Fact]
    public async Task AnthropicOldModelHasNoFallbacks()
    {
        var h = new FakeHandler(HttpStatusCode.OK, AnthropicSse.Replace("\r\n", "\n"), "text/event-stream");
        var p = new AnthropicProvider(new AiConfig(AiProviderKind.Anthropic, "claude-haiku-4-5", "", "k"), new HttpClient(h));
        await Collect(p, Chat(("user", "hello")));
        Assert.DoesNotContain("fallbacks", h.LastBody!);
    }

    [Fact]
    public async Task AnthropicRefusalIsReadable()
    {
        var sse = AnthropicSse.Replace("\r\n", "\n").Replace("\"stop_reason\":\"end_turn\"", "\"stop_reason\":\"refusal\"");
        var p = new AnthropicProvider(new AiConfig(AiProviderKind.Anthropic, "claude-opus-5-5", "", "k"), new HttpClient(new FakeHandler(HttpStatusCode.OK, sse, "text/event-stream")));
        var ex = await Assert.ThrowsAsync<AiException>(() => Collect(p, Chat(("user", "x"))));
        Assert.Equal("ai.err.refusal", ex.Key);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "authentication_error", "ai.err.auth")]
    [InlineData(HttpStatusCode.TooManyRequests, "rate_limit_error", "ai.err.rate")]
    [InlineData((HttpStatusCode)529, "overloaded_error", "ai.err.overloaded")]
    public async Task AnthropicHttpErrorsAreReadable(HttpStatusCode status, string type, string key)
    {
        var body = "{\"type\":\"error\",\"error\":{\"type\":\"" + type + "\",\"message\":\"nope\"}}";
        var http = new HttpClient(new FakeHandler(status, body, "application/json"));
        var p = new AnthropicProvider(new AiConfig(AiProviderKind.Anthropic, "claude-opus-5-5", "", "k"), http);
        var ex = await Assert.ThrowsAsync<AiException>(() => Collect(p, Chat(("user", "x"))));
        Assert.Equal(key, ex.Key);
    }

    [Fact]
    public void ConfigReadsProtectedKeys()
    {
        var s = new Settings();
        s.SetOpt("ai.anthropic.key", Secret.Protect("sk-ant-123"));
        var cfg = AiConfig.FromSettings(s);
        Assert.Equal(AiProviderKind.Anthropic, cfg.Provider);
        Assert.Equal(AiConfig.DefaultAnthropicModel, cfg.Model);
        Assert.Equal("sk-ant-123", cfg.Key);
        Assert.True(cfg.IsReady);
        s.SetOpt("ai.provider", "openai");
        var o = AiConfig.FromSettings(s);
        Assert.Equal(AiConfig.DefaultOpenAiBase, o.BaseUrl);
        Assert.False(o.IsReady); // no key, no model
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
