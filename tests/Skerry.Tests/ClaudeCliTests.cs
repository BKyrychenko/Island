using System.Text;
using System.Text.Json;
using Skerry.Modules.AiChat;

namespace Skerry.Tests;

public class ClaudeCliTests
{
    [Fact]
    public void NativeChatProcessesRunHiddenWithRedirectedPipes()
    {
        var start = ChatCli.StartInfo("claude.exe", Path.GetTempPath(), "CLAUDE_CONFIG_DIR");
        Assert.False(start.UseShellExecute);
        Assert.True(start.CreateNoWindow);
        Assert.True(start.RedirectStandardInput && start.RedirectStandardOutput && start.RedirectStandardError);
        Assert.Empty(start.StandardInputEncoding!.GetPreamble());
    }

    [Theory]
    [InlineData("claude.ai", true)]
    [InlineData("api_key", false)]
    [InlineData("api_key_helper", false)]
    [InlineData("oauth_token", false)]
    [InlineData("third_party", false)]
    public void RequiresTheLocalClaudeAccount(string method, bool accepted)
    {
        var status = JsonSerializer.Serialize(new { loggedIn = true, authMethod = method });
        if (accepted) ClaudeCodeProvider.CheckLogin(0, status);
        else Assert.Equal("ai.err.claudeLogin", Assert.Throws<AiException>(() => ClaudeCodeProvider.CheckLogin(0, status)).Key);
    }

    [Fact]
    public void SendsHistoryAndKeepsEachAttachmentNextToItsMessage()
    {
        var chat = new Conversation
        {
            SystemPrompt = "Answer in Ukrainian",
            Messages =
            [
                new() { Text = "Первое фото", Attachments = [new() { Name = "1.png", Kind = AttachmentKind.Image, MediaType = "image/png", Data = "AQ==" }] },
                new() { Role = "assistant", Text = "First reply" },
                new() { Error = true, Text = "Discarded error" },
                new() { Text = "Compare the document", Attachments =
                [
                    new() { Name = "2.pdf", Kind = AttachmentKind.Pdf, MediaType = "application/pdf", Data = "Ag==" },
                    new() { Name = "notes.txt", Kind = AttachmentKind.Text, Text = "Notes" },
                ] },
            ],
        };
        var input = ClaudeCodeProvider.BuildInput(chat);
        Assert.Single(input.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        using var json = JsonDocument.Parse(input);
        var message = json.RootElement.GetProperty("message");
        Assert.Equal("user", message.GetProperty("role").GetString());
        var blocks = message.GetProperty("content").EnumerateArray().ToArray();
        Assert.Equal(["text", "image", "text", "document", "text"], blocks.Select(b => b.GetProperty("type").GetString()));
        Assert.Contains(chat.SystemPrompt, blocks[0].GetProperty("text").GetString());
        Assert.Contains("Первое фото", blocks[0].GetProperty("text").GetString());
        Assert.Equal("AQ==", blocks[1].GetProperty("source").GetProperty("data").GetString());
        Assert.Contains("First reply", blocks[2].GetProperty("text").GetString());
        Assert.Contains("Compare the document", blocks[2].GetProperty("text").GetString());
        Assert.DoesNotContain("Discarded error", blocks[2].GetProperty("text").GetString());
        Assert.Equal("application/pdf", blocks[3].GetProperty("source").GetProperty("media_type").GetString());
        Assert.Contains("Notes", blocks[4].GetProperty("text").GetString());
    }

    [Fact]
    public async Task OnlyASuccessfulFinalClaudeResultBecomesChatText()
    {
        static async Task<string> Parse(string final)
        {
            var events = "{\"type\":\"system\",\"subtype\":\"init\"}\n" + final;
            using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(events)));
            return await ClaudeCodeProvider.ReadAnswer(reader, CancellationToken.None);
        }

        Assert.Equal("Готово", await Parse("""{"type":"result","is_error":false,"result":"Готово"}"""));
        Assert.Empty(await Parse("""{"type":"result","is_error":true,"result":"private diagnostic"}"""));
        Assert.Empty(await Parse("""{"type":"assistant","message":{"content":"partial"}}"""));
        Assert.Equal("ai.err.claudeRequest", (await Assert.ThrowsAsync<AiException>(() => Parse("{}"))).Key);
    }
}
