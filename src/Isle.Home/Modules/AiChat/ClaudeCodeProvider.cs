using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Isle.Modules.AiChat;

/// <summary>Runs Claude Code in print mode using a local Claude account sign-in.</summary>
sealed class ClaudeCodeProvider : IChatProvider
{
    const string Missing = "ai.err.claudeMissing", RequestFailed = "ai.err.claudeRequest";
    const int MaxInputBytes = 10_000_000;
    static readonly string[] ChatArgs =
    [
        "--print", "--no-session-persistence", "--no-chrome", "--disable-slash-commands", "--tools", "", "--disallowedTools", "*",
        "--strict-mcp-config", "--mcp-config", "{\"mcpServers\":{}}",
        "--input-format", "stream-json", "--output-format", "stream-json", "--verbose",
    ];

    internal static string? FindExecutable()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return ChatCli.FindExecutable("claude.exe", Path.Combine(profile, ".local", "bin", "claude.exe"),
            Path.Combine(local, "Microsoft", "WinGet", "Links", "claude.exe"));
    }

    public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        string answer;
        try { answer = await Complete(chat, timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new AiException("ai.err.timeout"); }
        yield return answer;
    }

    static async Task<string> Complete(Conversation chat, CancellationToken ct)
    {
        var exe = FindExecutable() ?? throw new AiException(Missing);
        var input = BuildInput(chat);
        var workspace = Directory.CreateTempSubdirectory("isle-claude-");
        try
        {
            var login = StartInfo(exe, workspace.FullName);
            login.ArgumentList.Add("auth");
            login.ArgumentList.Add("status");
            var (loginCode, status) = await ChatCli.Run(login, "", (r, token) => r.ReadToEndAsync(token), ct, Missing, RequestFailed)
                .ConfigureAwait(false);
            CheckLogin(loginCode, status);

            var request = StartInfo(exe, workspace.FullName);
            foreach (var arg in ChatArgs) request.ArgumentList.Add(arg);
            var (code, answer) = await ChatCli.Run(request, input, ReadAnswer, ct, Missing, RequestFailed).ConfigureAwait(false);
            if (code != 0 || string.IsNullOrWhiteSpace(answer)) throw new AiException(RequestFailed);
            return answer;
        }
        finally
        {
            try { workspace.Delete(recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    static ProcessStartInfo StartInfo(string exe, string workspace)
    {
        var info = ChatCli.StartInfo(exe, workspace, "CLAUDE_CONFIG_DIR");
        info.Environment["DISABLE_AUTOUPDATER"] = "1";
        info.ArgumentList.Add("--safe-mode");
        info.ArgumentList.Add("--setting-sources");
        info.ArgumentList.Add("");
        info.ArgumentList.Add("--settings");
        info.ArgumentList.Add("{\"disableAllHooks\":true}");
        return info;
    }

    internal static void CheckLogin(int code, string status)
    {
        try
        {
            using var login = JsonDocument.Parse(status);
            var root = login.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("loggedIn", out var loggedIn))
                throw new AiException(RequestFailed);
            if (code != 0 || loggedIn.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("authMethod", out var method) || method.ValueKind != JsonValueKind.String || !method.ValueEquals("claude.ai"))
                throw new AiException("ai.err.claudeLogin");
        }
        catch (JsonException) { throw new AiException(RequestFailed); }
    }

    internal static string BuildInput(Conversation chat)
    {
        var blocks = new JsonArray();
        var text = new StringBuilder("Answer the latest user message in this chat. Use the supplied conversation and attachments.\n");
        if (chat.SystemPrompt.Length > 0) text.Append("Conversation instructions: ").AppendLine(chat.SystemPrompt);
        var binaryChars = 0L;
        var previousTextChars = 0;
        foreach (var message in chat.ForRequest())
        {
            text.Append(message.Role == "assistant" ? "\nAssistant: " : "\nUser: ").AppendLine(message.Text);
            foreach (var file in message.Attachments)
            {
                text.Append("\nAttachment: ").AppendLine(file.Name);
                if (file.Kind is AttachmentKind.Image or AttachmentKind.Pdf && file.Data.Length > 0)
                {
                    binaryChars += file.Data.Length;
                    if (binaryChars >= MaxInputBytes) throw new AiException("ai.err.tooLarge");
                    FlushText();
                    blocks.Add(new JsonObject
                    {
                        ["type"] = file.Kind == AttachmentKind.Image ? "image" : "document",
                        ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = file.MediaType, ["data"] = file.Data },
                    });
                }
                else if (file.Text.Length > 0) text.AppendLine(file.Text);
                else throw new AiException("ai.att.unreadable", file.Name);
                if (text.Length > ChatCli.MaxPromptChars - previousTextChars) throw new AiException("ai.err.tooLarge");
            }
            if (text.Length > ChatCli.MaxPromptChars - previousTextChars) throw new AiException("ai.err.tooLarge");
        }
        FlushText();
        var line = new JsonObject
        {
            ["type"] = "user",
            ["message"] = new JsonObject { ["role"] = "user", ["content"] = blocks },
            ["parent_tool_use_id"] = null,
        }.ToJsonString() + "\n";
        if (Encoding.UTF8.GetByteCount(line) > MaxInputBytes) throw new AiException("ai.err.tooLarge");
        return line;

        void FlushText()
        {
            if (text.Length == 0) return;
            if (text.Length > ChatCli.MaxPromptChars - previousTextChars) throw new AiException("ai.err.tooLarge");
            blocks.Add(new JsonObject { ["type"] = "text", ["text"] = text.ToString() });
            previousTextChars += text.Length;
            text.Clear();
        }
    }

    internal static async Task<string> ReadAnswer(StreamReader reader, CancellationToken ct)
    {
        var answer = "";
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) ||
                    type.ValueKind != JsonValueKind.String) throw new AiException(RequestFailed);
                if (!type.ValueEquals("result")) continue;
                answer = root.TryGetProperty("is_error", out var error) && error.ValueKind == JsonValueKind.False &&
                    root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String ? result.GetString() ?? "" : "";
            }
            catch (JsonException) { throw new AiException(RequestFailed); }
        }
        return answer;
    }
}
