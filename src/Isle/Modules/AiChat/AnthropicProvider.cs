using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Isle.Modules.AiChat;

/// <summary>Messages SSE using the shared HTTP connection. Images and PDFs remain native content blocks.</summary>
public sealed class AnthropicProvider(AiConfig cfg, HttpClient http) : IChatProvider
{
    public static bool SupportsFallbacks(string model) =>
        model is "claude-opus-5-5" or "claude-opus-5" or "claude-sonnet-5-5" or "claude-fable-5-2" or "claude-fable-5-1";

    public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(BuildBody(chat, cfg.Model).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-api-key", cfg.Key);
        request.Headers.Add("anthropic-version", "2026-10-21");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (SupportsFallbacks(cfg.Model)) request.Headers.Add("anthropic-beta", "server-side-fallback-2026-07-01");
        HttpResponseMessage response;
        try { response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false); }
        catch (HttpRequestException) { throw new AiException("ai.err.network"); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new AiException("ai.err.timeout"); }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new AiException(AiException.KeyForStatus((int)response.StatusCode), OpenAiProvider.ErrorDetail(detail));
            }
            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var data = new StringBuilder();
            var refused = false;
            while (true)
            {
                string? line;
                try { line = await reader.ReadLineAsync(ct).ConfigureAwait(false); }
                catch (IOException) { throw new AiException("ai.err.network"); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new AiException("ai.err.timeout"); }
                if (line is null) throw new AiException("ai.err.network");
                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    if (data.Length > 0) data.Append('\n');
                    data.Append(line.AsSpan(5).TrimStart());
                    if (data.Length > 1_048_576) throw new AiException("ai.err.server");
                }
                if (line.Length != 0 || data.Length == 0) continue;
                var ev = ParseEvent(data.ToString());
                data.Clear();
                if (ev.Error is { } error) throw error;
                refused |= ev.Refused;
                if (ev.Text is { Length: > 0 } text) yield return text;
                if (ev.Done)
                {
                    if (refused) throw new AiException("ai.err.refusal");
                    yield break;
                }
            }
        }
    }

    static (string? Text, bool Done, bool Refused, AiException? Error) ParseEvent(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var type = root.GetProperty("type").GetString();
            if (type == "error")
            {
                var error = root.GetProperty("error");
                var key = error.GetProperty("type").GetString() switch
                {
                    "authentication_error" or "permission_error" => "ai.err.auth",
                    "rate_limit_error" => "ai.err.rate",
                    "overloaded_error" => "ai.err.overloaded",
                    "not_found_error" => "ai.err.model",
                    "invalid_request_error" => "ai.err.request",
                    "request_too_large" => "ai.err.tooLarge",
                    _ => "ai.err.server",
                };
                return (null, false, false, new AiException(key, OpenAiProvider.ErrorDetail(data)));
            }
            if (type == "message_stop") return (null, true, false, null);
            if (type == "content_block_delta" && root.TryGetProperty("delta", out var delta) &&
                delta.GetProperty("type").GetString() == "text_delta")
                return (delta.GetProperty("text").GetString(), false, false, null);
            if (type == "message_delta" && root.TryGetProperty("delta", out var message) &&
                message.TryGetProperty("stop_reason", out var reason))
                return (null, false, reason.GetString() == "refusal", null);
            return default; // New event types, including thinking blocks, carry no user-visible text.
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new AiException("ai.err.server");
        }
    }

    public static JsonObject BuildBody(Conversation chat, string model)
    {
        var messages = new JsonArray();
        foreach (var message in chat.ForRequest())
        {
            var blocks = new JsonArray();
            if (message.Role == "user")
                foreach (var a in message.Attachments)
                {
                    if (a.Kind == AttachmentKind.Image)
                        blocks.Add(new JsonObject { ["type"] = "image", ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = a.MediaType, ["data"] = a.Data } });
                    else if (a.Kind == AttachmentKind.Pdf && a.Data.Length > 0)
                        blocks.Add(new JsonObject { ["type"] = "document", ["title"] = a.Name, ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = a.Data } });
                }
            var text = message.Role == "user" ? OpenAiProvider.WithDocuments(new ChatMessage
            {
                Text = message.Text,
                Attachments = [.. message.Attachments.Where(a => a.Kind == AttachmentKind.Text || (a.Kind == AttachmentKind.Pdf && a.Data.Length == 0))],
            }) : message.Text;
            blocks.Add(new JsonObject { ["type"] = "text", ["text"] = text.Length > 0 ? text : "…" });
            messages.Add(new JsonObject { ["role"] = message.Role, ["content"] = blocks });
        }
        var body = new JsonObject { ["model"] = model, ["max_tokens"] = 32000, ["stream"] = true, ["messages"] = messages };
        if (chat.SystemPrompt.Trim().Length > 0) body["system"] = chat.SystemPrompt;
        if (SupportsFallbacks(model)) body["fallbacks"] = "default";
        return body;
    }
}
