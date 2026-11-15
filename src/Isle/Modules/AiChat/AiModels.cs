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

public enum AiProviderKind { Anthropic, OpenAi }

/// <summary>Provider settings as entered by the user. The key is plain text only in memory; stored DPAPI-protected.</summary>
public sealed record AiConfig(AiProviderKind Provider, string Model, string BaseUrl, string Key)
{
    public const string DefaultAnthropicModel = "claude-opus-5-5";
    public const string DefaultOpenAiBase = "https://api.openai.com/v1";

    public static AiConfig FromSettings(Settings s)
    {
        var kind = s.Opt("ai.provider") == "openai" ? AiProviderKind.OpenAi : AiProviderKind.Anthropic;
        var model = s.Opt(kind == AiProviderKind.OpenAi ? "ai.openai.model" : "ai.anthropic.model");
        if (model.Length == 0 && kind == AiProviderKind.Anthropic) model = DefaultAnthropicModel;
        var baseUrl = s.Opt("ai.openai.base");
        if (baseUrl.Length == 0) baseUrl = DefaultOpenAiBase;
        var key = Secret.Unprotect(s.Opt(kind == AiProviderKind.OpenAi ? "ai.openai.key" : "ai.anthropic.key"));
        return new AiConfig(kind, model, baseUrl, key);
    }

    public bool IsReady => Key.Length > 0 && Model.Length > 0 && (Provider == AiProviderKind.Anthropic || BaseUrl.Length > 0);
}

/// <summary>A readable failure (already localized key + optional detail). Never contains keys or message bodies.</summary>
public sealed class AiException(string key, string detail = "") : Exception(key)
{
    public string Key { get; } = key;
    public string Detail { get; } = detail;

    /// <summary>HTTP status → Strings key.</summary>
    public static string KeyForStatus(int status) => status switch
    {
        400 => "ai.err.request",
        401 or 403 => "ai.err.auth",
        404 => "ai.err.model",
        413 => "ai.err.tooLarge",
        429 => "ai.err.rate",
        529 or 503 => "ai.err.overloaded",
        >= 500 => "ai.err.server",
        _ => "ai.err.network",
    };
}

public interface IChatProvider
{
    /// <summary>Streams the assistant's text. Throws <see cref="AiException"/> with a readable key on failure.</summary>
    IAsyncEnumerable<string> StreamAsync(Conversation chat, CancellationToken ct);
}
