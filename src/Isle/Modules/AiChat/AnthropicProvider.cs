using System.Net.Http;
using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

namespace Isle.Modules.AiChat;

/// <summary>
/// Anthropic Messages API through the official SDK (streaming on the beta endpoint so refusal fallbacks can be enabled).
/// Images go as base64 image blocks, PDFs as base64 document blocks, other files as their extracted text.
/// </summary>
public sealed class AnthropicProvider(AiConfig cfg, HttpClient? http = null) : IChatProvider
{
    const int MaxTokens = 32000;

    /// <summary>Models that accept server-side refusal fallbacks (`fallbacks: "default"`).</summary>
    public static bool SupportsFallbacks(string model) =>
        model is "claude-opus-5-5" or "claude-opus-5" or "claude-sonnet-5-5" or "claude-fable-5-2" or "claude-fable-5-1";

    public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = http is null ? new AnthropicClient { ApiKey = cfg.Key } : new AnthropicClient { ApiKey = cfg.Key, HttpClient = http };
        var p = BuildParams(chat, cfg.Model);
        var refused = false;
        var e = client.Beta.Messages.CreateStreaming(p, ct).GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                BetaRawMessageStreamEvent ev;
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false)) break;
                    ev = e.Current;
                }
                catch (Exception ex) when (Map(ex) is { } mapped) { throw mapped; }

                if (ev.TryPickContentBlockDelta(out var d) && d.Delta.TryPickText(out var t)) yield return t.Text;
                else if (ev.TryPickDelta(out var md) && md.Delta.StopReason is { } sr && sr.ToString().Contains("refusal", StringComparison.OrdinalIgnoreCase)) refused = true;
            }
        }
        finally { await e.DisposeAsync().ConfigureAwait(false); }
        if (refused) throw new AiException("ai.err.refusal");
    }

    public static MessageCreateParams BuildParams(Conversation chat, string model)
    {
        var messages = new List<BetaMessageParam>();
        foreach (var m in chat.ForRequest())
        {
            var blocks = new List<BetaContentBlockParam>();
            if (m.Role == "user")
            {
                foreach (var a in m.Attachments)
                {
                    if (a.Kind == AttachmentKind.Image)
                        blocks.Add(new BetaImageBlockParam { Source = new BetaBase64ImageSource { Data = a.Data, MediaType = a.MediaType } });
                    else if (a.Kind == AttachmentKind.Pdf && a.Data.Length > 0)
                        blocks.Add(new BetaRequestDocumentBlock { Source = new BetaBase64PdfSource { Data = a.Data }, Title = a.Name });
                }
                var text = OpenAiProvider.WithDocuments(new ChatMessage
                {
                    Text = m.Text,
                    Attachments = [.. m.Attachments.Where(a => a.Kind == AttachmentKind.Text || (a.Kind == AttachmentKind.Pdf && a.Data.Length == 0))],
                });
                blocks.Add(new BetaTextBlockParam { Text = text.Length > 0 ? text : "…" });
                messages.Add(new BetaUserMessageParam { Content = blocks });
            }
            else
            {
                blocks.Add(new BetaTextBlockParam { Text = m.Text });
                messages.Add(new BetaAssistantMessageParam { Content = blocks });
            }
        }
        var p = new MessageCreateParams
        {
            Model = model,
            MaxTokens = MaxTokens,
            Messages = messages,
        };
        if (chat.SystemPrompt.Trim().Length > 0) p = p with { System = chat.SystemPrompt };
        if (SupportsFallbacks(model)) p = p with { Betas = ["server-side-fallback-2026-07-01"], Fallbacks = new Default() };
        return p;
    }

    static AiException? Map(Exception ex) => ex switch
    {
        AiException => null,
        OperationCanceledException => null,
        AnthropicUnauthorizedException or AnthropicForbiddenException => new AiException("ai.err.auth"),
        AnthropicRateLimitException => new AiException("ai.err.rate"),
        AnthropicOverloadedException or AnthropicServiceUnavailableException => new AiException("ai.err.overloaded"),
        AnthropicNotFoundException e => new AiException("ai.err.model", Short(e.ApiMessage)),
        AnthropicBadRequestException e => new AiException("ai.err.request", Short(e.ApiMessage)),
        Anthropic5xxException => new AiException("ai.err.server"),
        AnthropicServiceException e => new AiException("ai.err.server", Short(e.ApiMessage)),
        AnthropicIOException or HttpRequestException or IOException => new AiException("ai.err.network"),
        _ => new AiException("ai.err.server", ex.GetType().Name),
    };

    static string Short(string? s) => s is null ? "" : s.Length > 200 ? s[..200] + "…" : s;
}
