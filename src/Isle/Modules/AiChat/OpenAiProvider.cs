using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Isle.Modules.AiChat;

/// <summary>
/// OpenAI-compatible Chat Completions (`POST {base}/chat/completions`, `stream: true`, SSE). Works with OpenAI and the
/// many servers that copy its API (OpenRouter, Groq, LM Studio, Ollama's /v1, …). Images go as data-URL `image_url`
/// parts; PDFs and documents as their extracted text.
/// </summary>
public sealed class OpenAiProvider(HttpClient http, AiConfig cfg) : IChatProvider
{
    public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, cfg.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = new StringContent(BuildBody(chat, cfg.Model).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.Key);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage resp;
        try { resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false); }
        catch (HttpRequestException) { throw new AiException("ai.err.network"); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new AiException("ai.err.timeout"); }

        using (resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var body = "";
                try { body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false); } catch (HttpRequestException) { }
                throw new AiException(AiException.KeyForStatus((int)resp.StatusCode), ErrorDetail(body));
            }
            using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (true)
            {
                string? line;
                try { line = await reader.ReadLineAsync(ct).ConfigureAwait(false); }
                catch (IOException) { throw new AiException("ai.err.network"); }
                if (line is null) yield break;
                var ev = ParseLine(line);
                if (ev.Done) yield break;
                if (ev.Error is { } err) throw new AiException("ai.err.server", err);
                if (ev.Text is { Length: > 0 } t) yield return t;
            }
        }
    }

    public static JsonObject BuildBody(Conversation chat, string model)
    {
        var messages = new JsonArray();
        if (chat.SystemPrompt.Trim().Length > 0)
            messages.Add(new JsonObject { ["role"] = "system", ["content"] = chat.SystemPrompt });
        foreach (var m in chat.ForRequest())
        {
            var images = m.Attachments.Where(a => a.Kind == AttachmentKind.Image).ToList();
            var text = WithDocuments(m);
            if (images.Count == 0 || m.Role != "user")
            {
                messages.Add(new JsonObject { ["role"] = m.Role, ["content"] = text });
                continue;
            }
            var parts = new JsonArray();
            foreach (var img in images)
                parts.Add(new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = $"data:{img.MediaType};base64,{img.Data}" } });
            parts.Add(new JsonObject { ["type"] = "text", ["text"] = text });
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = parts });
        }
        return new JsonObject { ["model"] = model, ["stream"] = true, ["messages"] = messages };
    }

    /// <summary>Message text with every non-image attachment's text prepended as a labelled block.</summary>
    public static string WithDocuments(ChatMessage m)
    {
        var docs = m.Attachments.Where(a => a.Kind != AttachmentKind.Image && a.Text.Length > 0).ToList();
        if (docs.Count == 0) return m.Text;
        var sb = new StringBuilder();
        foreach (var d in docs) sb.Append("<file name=\"").Append(d.Name).Append("\">\n").Append(d.Text).Append("\n</file>\n\n");
        return sb.Append(m.Text).ToString();
    }

    public readonly record struct SseEvent(string? Text, bool Done, string? Error);

    /// <summary>One SSE line → delta text / done / error. Comments, blank lines and non-data fields are ignored.</summary>
    public static SseEvent ParseLine(string line)
    {
        if (!line.StartsWith("data:", StringComparison.Ordinal)) return default;
        var data = line[5..].Trim();
        if (data == "[DONE]") return new SseEvent(null, true, null);
        if (data.Length == 0) return default;
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var e))
                return new SseEvent(null, false, e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out var msg) ? msg.GetString() : "error");
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                return new SseEvent(c.GetString(), false, null);
        }
        catch (JsonException) { }
        return default;
    }

    /// <summary>The server's short error message (`error.message`), cut to 200 chars; never our request body.</summary>
    public static string ErrorDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e))
            {
                var msg = e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out var m) ? m.GetString() : e.ValueKind == JsonValueKind.String ? e.GetString() : null;
                if (msg is not null) return msg.Length > 200 ? msg[..200] + "…" : msg;
            }
        }
        catch (JsonException) { }
        return "";
    }
}
