using Isle.Core;

namespace Isle.Modules.AiChat;

public enum AttachmentKind { Image, Pdf, Text }

/// <summary>A file sent with a user message. Images/PDF keep base64 data (PDF also keeps extracted text for providers
/// without document support); text-like files (code, .docx, .xlsx) keep their extracted text only.</summary>
public sealed class ChatAttachment
{
    public AttachmentKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string MediaType { get; set; } = "";
    public string Data { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class ChatMessage
{
    public string Role { get; set; } = "user"; // "user" | "assistant"
    public string Text { get; set; } = "";
    public List<ChatAttachment> Attachments { get; set; } = [];
    public bool Error { get; set; }
    /// <summary>The user pressed Stop during this answer.</summary>
    public bool Stopped { get; set; }
    /// <summary>A failure that happened after part of the answer had arrived (shown under it).</summary>
    public string Note { get; set; } = "";
}

public sealed class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.Now;
    public List<ChatMessage> Messages { get; set; } = [];

    /// <summary>Messages to send: errors and empty assistant turns are left out, and consecutive same-role turns are
    /// merged so the history always alternates user/assistant and starts with the user.</summary>
    public List<ChatMessage> ForRequest()
    {
        var list = new List<ChatMessage>();
        foreach (var m in Messages)
        {
            if (m.Error || (m.Role == "assistant" && m.Text.Trim().Length == 0)) continue;
            if (list.Count == 0 && m.Role != "user") continue;
            if (list.Count > 0 && list[^1].Role == m.Role)
            {
                var prev = list[^1];
                list[^1] = new ChatMessage { Role = prev.Role, Text = prev.Text + "\n\n" + m.Text, Attachments = [.. prev.Attachments, .. m.Attachments] };
                continue;
            }
            list.Add(m);
        }
        return list;
    }
}

public enum AiProviderKind { Codex, ClaudeCode }

static class AiProviders
{
    public static AiProviderKind FromSettings(Settings settings) =>
        settings.Opt("ai.provider") is "claude" or "anthropic" ? AiProviderKind.ClaudeCode : AiProviderKind.Codex;

    public static string TitleKey(AiProviderKind provider) => provider == AiProviderKind.ClaudeCode ? "ai.claudeCode" : "ai.codex";

    public static string MissingKey(AiProviderKind provider) =>
        provider == AiProviderKind.ClaudeCode ? "ai.err.claudeMissing" : "ai.err.codexMissing";
}

/// <summary>A readable failure with a localization key and optional detail.</summary>
public sealed class AiException(string key, string detail = "") : Exception(key)
{
    public string Key { get; } = key;
    public string Detail { get; } = detail;
}

public interface IChatProvider
{
    /// <summary>Yields the answer so far, including any final correction. Throws <see cref="AiException"/> on failure.</summary>
    IAsyncEnumerable<string> StreamAsync(Conversation chat, CancellationToken ct);
}
