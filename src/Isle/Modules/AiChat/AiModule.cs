using System.Net.Http;
using System.Windows.Threading;
using Isle.Core;
using Isle.Views.AiChat;

namespace Isle.Modules.AiChat;

/// <summary>
/// AI chat (module id "ai"): bring-your-own-key chat with Anthropic or any OpenAI-compatible server. Owns the
/// conversations (`chats.json`), the AI tab, and one streaming request at a time. The HTTP client is created on the
/// first request and disposed 2 minutes after the last one. No network unless the user sends something.
/// Options: ai.provider ("anthropic"/"openai"), ai.anthropic.model/key, ai.openai.base/model/key (keys DPAPI).
/// </summary>
public sealed class AiModule(IslandWindow island, Func<AiConfig, HttpClient, IChatProvider>? providerFactory = null, ChatStore? store = null) : IModule, IDisposable
{
    public const string TabId = "ai";

    ChatStore? _store;
    AiChatView? _view;
    HttpClient? _http;
    DispatcherTimer? _idle;
    CancellationTokenSource? _run;
    Conversation? _current;

    public string Id => "ai";
    public ChatStore Store => _store ??= store ?? new ChatStore(Paths.File("chats.json"));
    public Conversation? Current => _current;
    public bool Busy => _run is not null;

    static Settings S => Settings.Current;

    public void Start()
    {
        island.AddTab(new TabInfo(TabId, "tab.ai", "Icon.Ai", () =>
        {
            _view = new AiChatView(this);
            _view.ShowChat(CurrentOrLatest());
            return _view;
        }, 90));
    }

    Conversation? CurrentOrLatest()
    {
        if (_current is null && S.Opt("ai.last") is { Length: > 0 } last) _current = Store.Get(last);
        return _current ??= Store.All.FirstOrDefault();
    }

    // ---- conversations ----

    public void Select(Conversation? c)
    {
        _current = c;
        S.SetOpt("ai.last", c?.Id ?? "");
        S.Save("Options");
        _view?.ShowChat(c);
    }

    public Conversation NewChat()
    {
        Cancel();
        var prompt = _current?.SystemPrompt ?? "";
        var c = Store.New(prompt);
        Select(c);
        return c;
    }

    public void Delete(Conversation c)
    {
        if (ReferenceEquals(c, _current)) Cancel();
        Store.Delete(c.Id);
        if (ReferenceEquals(c, _current)) Select(Store.All.FirstOrDefault());
        else _view?.ShowChat(_current);
    }

    public void SetSystemPrompt(string text)
    {
        var c = _current ?? NewChat();
        c.SystemPrompt = text.Trim();
        if (c.Messages.Count > 0) Store.Touch(c); else Store.Save();
    }

    // ---- sending ----

    /// <summary>Adds the user's message to the current chat and streams the answer into it.</summary>
    public async Task Send(string text, List<ChatAttachment> attachments)
    {
        if (Busy || (text.Trim().Length == 0 && attachments.Count == 0)) return;
        var chat = _current ?? NewChat();
        chat.Messages.Add(new ChatMessage { Role = "user", Text = text.Trim(), Attachments = attachments });
        var answer = new ChatMessage { Role = "assistant" };
        chat.Messages.Add(answer);
        Store.Touch(chat);
        _view?.ShowChat(chat);
        await StreamInto(chat, answer, d => { if (ReferenceEquals(_current, chat)) _view?.Streamed(answer); });
        Store.Touch(chat);
        if (ReferenceEquals(_current, chat)) _view?.Finished(answer);
    }

    /// <summary>Streams an answer for <paramref name="chat"/> into <paramref name="answer"/>; failures become a readable
    /// error text on the message (never thrown). Used by the tab and (later) by text actions and the command bar.</summary>
    public async Task StreamInto(Conversation chat, ChatMessage answer, Action<string>? delta = null)
    {
        var cfg = AiConfig.FromSettings(S);
        if (!cfg.IsReady) { Fail(answer, new AiException("ai.err.noKey")); return; }
        if (_run is not null) return;
        _run = new CancellationTokenSource();
        var ct = _run.Token;
        _idle?.Stop();
        Logger.Info($"ai request ({cfg.Provider})");
        try
        {
            var provider = CreateProvider(cfg);
            await foreach (var piece in provider.StreamAsync(chat, ct).WithCancellation(ct))
            {
                answer.Text += piece;
                delta?.Invoke(piece);
            }
            if (answer.Text.Trim().Length == 0) Fail(answer, new AiException("ai.err.empty"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { answer.Stopped = true; }
        catch (AiException ex) { Fail(answer, ex); }
        catch (Exception ex)
        {
            Logger.Warn($"ai stream: {ex.GetType().Name}");
            Fail(answer, new AiException("ai.err.server", ex.GetType().Name));
        }
        finally
        {
            _run.Dispose();
            _run = null;
            ArmIdle();
            Memory.TrimSoon(5);
        }
    }

    static void Fail(ChatMessage m, AiException ex)
    {
        Logger.Info($"ai error {ex.Key}");
        var text = Strings.T(ex.Key) + (ex.Detail.Length > 0 ? " (" + ex.Detail + ")" : "");
        if (m.Text.Trim().Length == 0) { m.Text = text; m.Error = true; }
        else m.Note = text; // partial answer kept, the failure shown under it
    }

    public void Cancel() => _run?.Cancel();

    IChatProvider CreateProvider(AiConfig cfg)
    {
        _http ??= new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        if (providerFactory is not null) return providerFactory(cfg, _http);
        return cfg.Provider == AiProviderKind.OpenAi ? new OpenAiProvider(_http, cfg) : new AnthropicProvider(cfg, _http);
    }

    void ArmIdle()
    {
        if (_idle is null)
        {
            _idle = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(2) };
            _idle.Tick += (_, _) => { _idle!.Stop(); if (!Busy) { _http?.Dispose(); _http = null; } };
        }
        _idle.Stop();
        _idle.Start();
    }

    // ---- settings ----

    /// <summary>Saves provider settings; an empty key field keeps the stored key.</summary>
    public static void SaveConfig(AiProviderKind kind, string model, string baseUrl, string key)
    {
        var p = kind == AiProviderKind.OpenAi ? "ai.openai" : "ai.anthropic";
        S.SetOpt("ai.provider", kind == AiProviderKind.OpenAi ? "openai" : "anthropic");
        S.SetOpt(p + ".model", model.Trim());
        if (kind == AiProviderKind.OpenAi) S.SetOpt("ai.openai.base", baseUrl.Trim());
        if (key.Trim().Length > 0) S.SetOpt(p + ".key", Secret.Protect(key.Trim()));
        S.Save("Options");
    }

    public static void ForgetKey(AiProviderKind kind)
    {
        S.Options.Remove(kind == AiProviderKind.OpenAi ? "ai.openai.key" : "ai.anthropic.key");
        S.Save("Options");
    }

    /// <summary>Sends a one-line test message with the saved settings. Returns null on success, else a readable error.</summary>
    public async Task<string?> TestConnection()
    {
        var chat = new Conversation();
        chat.Messages.Add(new ChatMessage { Role = "user", Text = "Reply with the single word OK." });
        var answer = new ChatMessage { Role = "assistant" };
        await StreamInto(chat, answer);
        return answer.Error ? answer.Text : answer.Note.Length > 0 && !answer.Stopped ? answer.Note : null;
    }

    public void Stop()
    {
        Cancel();
        _idle?.Stop();
        _idle = null;
        island.RemoveTab(TabId);
        _view = null;
        _http?.Dispose();
        _http = null;
    }

    public void Dispose() => Stop();
}
