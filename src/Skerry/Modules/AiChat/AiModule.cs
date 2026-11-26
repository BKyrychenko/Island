using System.Windows.Threading;
using Skerry.Core;
using Skerry.Views.AiChat;

namespace Skerry.Modules.AiChat;

/// <summary>
/// AI chat and text actions through a signed-in Codex or Claude Code CLI. Owns the conversations
/// (`chats.json`), the AI tab and one active request at a time.
/// </summary>
public sealed class AiModule(IslandWindow island, Func<AiProviderKind, IChatProvider>? providerFactory = null, ChatStore? store = null) : IModule, IDisposable
{
    public const string TabId = "ai";

    ChatStore? _store;
    AiChatView? _view;
    CancellationTokenSource? _run;
    Conversation? _current;
    AiPopupView? _popup;
    IDisposable? _askSub;

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
        island.Hotkeys.Register("textActions", "hotkey.textActions", "Win+Alt+A", () => _ = TextActionHotkey());
        _askSub = EventBus.Subscribe<Skerry.Modules.Commands.AskAi>(q => island.Dispatcher.BeginInvoke(() => Ask(q.Question)));
    }

    AiPopupView Popup => _popup ??= new AiPopupView(this);

    // ---- text actions / quick questions ----

    async Task TextActionHotkey()
    {
        try
        {
            var own = new System.Windows.Interop.WindowInteropHelper(island).Handle;
            var target = Native.GetForegroundWindow();
            if (target == own || target == IntPtr.Zero) target = island.PasteTarget;
            var text = await ClipboardOps.CopySelectionAsync();
            if (text.Trim().Length == 0)
            {
                island.ShowToast(Strings.T("ta.noSelection"));
                return;
            }
            Logger.Info($"text action: selection of {text.Length} chars");
            island.SetHidden("user", false);
            Popup.ShowSelection(text, target);
            island.ShowOverlay(Popup);
        }
        catch (Exception ex) { Logger.Error("text actions", ex); }
    }

    /// <summary>Command bar `? question`: a short streamed answer in the popup.</summary>
    public void Ask(string question)
    {
        if (question.Trim().Length == 0) return;
        island.SetHidden("user", false);
        island.ShowOverlay(Popup);
        Popup.ShowQuestion(question.Trim());
    }

    /// <summary>Opens the AI tab with its provider settings.</summary>
    public void OpenSettings()
    {
        island.HideOverlay();
        island.Expand(TabId, sticky: true);
        island.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => _view?.ShowSettings());
    }

    /// <summary>Continues a quick question as a stored chat in the AI tab.</summary>
    public void OpenInChat(string question, ChatMessage answer)
    {
        var c = Store.New();
        c.Messages.Add(new ChatMessage { Role = "user", Text = question });
        c.Messages.Add(new ChatMessage { Role = "assistant", Text = answer.Text, Note = answer.Note, Stopped = answer.Stopped });
        Store.Touch(c);
        Select(c);
        island.HideOverlay();
        island.Expand(TabId, sticky: true);
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
        _view?.ForgetDraft(c);
    }

    public void SetSystemPrompt(string text)
    {
        var c = _current ?? NewChat();
        c.SystemPrompt = text.Trim();
        if (c.Messages.Count > 0) Store.Touch(c); else Store.Save();
    }

    // ---- sending ----

    /// <summary>Adds the user's message to the current chat and streams the answer into it.</summary>
    public async Task Send(string text, List<ChatAttachment> attachments, Action? accepted = null)
    {
        if (Busy || (text.Trim().Length == 0 && attachments.Count == 0)) return;
        var chat = _current ?? NewChat();
        var user = new ChatMessage { Role = "user", Text = text.Trim(), Attachments = attachments };
        chat.Messages.Add(user);
        var answer = new ChatMessage { Role = "assistant" };
        chat.Messages.Add(answer);
        Store.Touch(chat);
        _view?.ShowChat(chat);
        var acknowledged = false;
        var stream = StreamInto(chat, answer, d =>
        {
            if (!acknowledged && !string.IsNullOrWhiteSpace(d)) { acknowledged = true; accepted?.Invoke(); }
            if (ReferenceEquals(_current, chat)) _view?.Streamed(answer);
        });
        if (ReferenceEquals(_current, chat)) _view?.ShowChat(chat);
        await stream;
        if (!acknowledged) chat.Messages.Remove(user); // The editable draft is still in the composer.
        Store.Touch(chat);
        if (ReferenceEquals(_current, chat)) _view?.Finished(answer);
    }

    /// <summary>Streams an answer for <paramref name="chat"/> into <paramref name="answer"/>; failures become a readable
    /// error text on the message (never thrown). Used by the tab and (later) by text actions and the command bar.</summary>
    public Task StreamInto(Conversation chat, ChatMessage answer, Action<string>? delta = null) =>
        StreamInto(AiProviders.FromSettings(S), chat, answer, delta);

    async Task StreamInto(AiProviderKind kind, Conversation chat, ChatMessage answer, Action<string>? delta = null)
    {
        if (!IsProviderInstalled(kind))
        {
            Fail(answer, new AiException(AiProviders.MissingKey(kind)));
            return;
        }
        if (_run is not null) return;
        _run = new CancellationTokenSource();
        _view?.RefreshBusy();
        var ct = _run.Token;
        Logger.Info($"ai request ({kind})");
        try
        {
            var provider = providerFactory?.Invoke(kind)
                ?? (kind == AiProviderKind.ClaudeCode ? (IChatProvider)new ClaudeCodeProvider() : new CodexChatProvider());
            await foreach (var text in provider.StreamAsync(chat, ct).WithCancellation(ct))
            {
                answer.Text = text;
                delta?.Invoke(text);
            }
            ct.ThrowIfCancellationRequested();
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
            _view?.RefreshBusy();
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

    internal bool IsProviderInstalled(AiProviderKind kind) => providerFactory is not null ||
        (kind == AiProviderKind.ClaudeCode ? ClaudeCodeProvider.FindExecutable() : CodexChatProvider.FindExecutable()) is not null;

    // ---- settings ----

    internal static void SaveConfig(AiProviderKind kind)
    {
        S.SetOpt("ai.provider", kind == AiProviderKind.ClaudeCode ? "claude" : "codex");
        S.Save("Options");
    }

    /// <summary>Tests the selected provider without changing the saved choice.</summary>
    internal async Task<string?> TestConnection(AiProviderKind kind)
    {
        var chat = new Conversation();
        chat.Messages.Add(new ChatMessage { Role = "user", Text = "Reply with the single word OK." });
        var answer = new ChatMessage { Role = "assistant" };
        await StreamInto(kind, chat, answer);
        if (answer.Stopped) return Strings.T("ai.stopped");
        return answer.Error ? answer.Text : answer.Note.Length > 0 ? answer.Note : null;
    }

    public void Stop()
    {
        Cancel();
        _askSub?.Dispose();
        _askSub = null;
        island.Hotkeys.Unregister("textActions");
        if (_popup is not null) island.HideOverlay(_popup);
        _popup = null;
        island.RemoveTab(TabId);
        _view = null;
    }

    public void Dispose() => Stop();
}
