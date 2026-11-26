using Skerry.Core;
using Skerry.Views.AiChat;

namespace Skerry.Modules.AiChat;

/// <summary>--snapshot states for the AI tab (in-memory chats in a temp file that is never written, no network).</summary>
static class AiSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["ai-setup"] = w => Tab(w, ready: false, (m, v) => v.ShowChat(null));
        states["ai-empty"] = w => Tab(w, ready: true, (m, v) => v.ShowChat(m.Store.New()));
        states["ai-chat"] = w => Tab(w, ready: true, (m, v) => v.ShowChat(Demo(m.Store)));
        states["ai-code"] = w => Tab(w, ready: true, (m, v) => { v.ShowChat(CodeDemo(m.Store)); v.ScrollToEndNow(); });
        states["ai-streaming"] = w => Tab(w, ready: true, (m, v) =>
        {
            var c = m.Store.New();
            c.Title = Strings.Language == "uk" ? "Коротко про WPF" : "WPF in short";
            c.Messages.Add(new ChatMessage { Role = "user", Text = Strings.Language == "uk" ? "Що таке WPF, одним абзацом?" : "What is WPF, in one paragraph?" });
            c.Messages.Add(new ChatMessage { Role = "assistant", Text = "**WPF** (Windows Presentation Foundation) is Microsoft's UI framework for" });
            v.ShowStreamingDemo(c);
        });
        states["ai-error"] = w => Tab(w, ready: true, (m, v) =>
        {
            var c = m.Store.New();
            c.Messages.Add(new ChatMessage { Role = "user", Text = "Hello!" });
            c.Messages.Add(new ChatMessage { Role = "assistant", Text = Strings.T("ai.err.codexLogin"), Error = true });
            v.ShowChat(c);
        });
        states["ai-attach"] = w => Tab(w, ready: true, (m, v) =>
        {
            v.ShowChat(m.Store.New());
            v.AddPending(new ChatAttachment { Kind = AttachmentKind.Pdf, Name = "Quarterly report 2026.pdf" });
            v.AddPending(new ChatAttachment { Kind = AttachmentKind.Image, Name = "screenshot.png" });
            v.SetInput(Strings.Language == "uk" ? "Підсумуй звіт у трьох пунктах" : "Summarise the report in three bullets");
        });
        states["ai-chats"] = w => Tab(w, ready: true, (m, v) =>
        {
            CodeDemo(m.Store);
            var d = Demo(m.Store);
            m.Store.New().Title = "Trip to Amsterdam";
            v.ShowChat(d);
            v.ShowChatList();
        });
        states["ai-system"] = w => Tab(w, ready: true, (m, v) => { var c = m.Store.New(); c.SystemPrompt = "Answer briefly. Use metric units."; v.ShowChat(c); v.ShowSystemPrompt(); });
        states["ai-actions"] = w => Popup(w, v => v.ShowSelection(Sample, IntPtr.Zero));
        states["ai-action-result"] = w => Popup(w, v =>
        {
            v.ShowSelection(Sample, IntPtr.Zero);
            v.ShowDemoResult(TextAction.TranslateUk, "Привіт, команда! Реліз перенесено на четвер, бо останні тести знайшли помилку в експорті. Будь ласка, оновіть свої плани й скажіть, якщо четвер вам не підходить.", false);
        });
        states["ai-action-streaming"] = w => Popup(w, v =>
        {
            v.ShowSelection(Sample, IntPtr.Zero);
            v.ShowDemoResult(TextAction.Summarise, "- Release moved to **Thursday**\n- Reason: a bug in the export", true);
        });
        states["ai-question"] = w => Popup(w, v =>
        {
            v.ShowQuestion(Strings.Language == "uk" ? "скільки кілометрів у милі?" : "how many km in a mile?", startRequest: false);
            v.ShowDemoResult(null, Strings.Language == "uk" ? "Одна миля — це приблизно **1,609 км**." : "One mile is about **1.609 km** (exactly 1.609344 km).", false);
        });
        states["ai-question-setup"] = w => Popup(w, v =>
        {
            v.ShowQuestion("how many km in a mile?", startRequest: false);
            v.ShowDemoResult(null, Strings.T("ai.err.codexLogin"), false, error: true);
        });
        states["ai-settings"] = w => Tab(w, ready: true, (m, v) => { v.ShowChat(null); v.ShowSettings(); });
        states["ai-settings-codex"] = w => Tab(w, ready: true, (m, v) =>
        {
            Settings.Current.SetOpt("ai.provider", "codex");
            v.ShowChat(null);
            v.ShowSettings();
        });
        states["ai-settings-claude"] = w => Tab(w, ready: true, (m, v) =>
        {
            Settings.Current.SetOpt("ai.provider", "claude");
            v.ShowChat(null);
            v.ShowSettings();
        });
    }

    const string Sample = "Hi team, the release is moved to Thursday because the last tests found a bug in the export. Please update your plans and tell me if Thursday does not work for you.";

    static void Popup(IslandWindow w, Action<AiPopupView> setup)
    {
        var module = new AiModule(w, store: new ChatStore(Path.Combine(Path.GetTempPath(), "skerry-snapshot-chats.json")));
        var view = new AiPopupView(module);
        w.ShowOverlay(view);
        setup(view);
    }

    static void Tab(IslandWindow w, bool ready, Action<AiModule, AiChatView> setup)
    {
        Settings.Current.SetOpt("ai.provider", "codex");
        var module = new AiModule(w, store: new ChatStore(Path.Combine(Path.GetTempPath(), "skerry-snapshot-chats.json")));
        var view = new AiChatView(module, ready);
        setup(module, view);
        w.AddTab(new TabInfo(AiModule.TabId, "tab.ai", "Icon.Ai", () => view, 90));
        w.Expand(AiModule.TabId);
    }

    static Conversation Demo(ChatStore store)
    {
        var uk = Strings.Language == "uk";
        var c = store.New();
        c.Title = uk ? "Вихідні в Амстердамі" : "A weekend in Amsterdam";
        c.Messages.Add(new ChatMessage { Role = "user", Text = uk ? "Що подивитися в Амстердамі за вихідні?" : "What should I see in Amsterdam over a weekend?" });
        c.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Text = uk
                ? "Ось короткий план:\n\n1. **Субота** — канали Йордана, Рейксмузеум\n2. **Неділя** — Вондельпарк і ринок Альберта Кейпа\n\nПорада: купіть *GVB*-квиток на 48 годин."
                : "Here is a short plan:\n\n1. **Saturday** — the Jordaan canals, the Rijksmuseum\n2. **Sunday** — Vondelpark and the Albert Cuyp market\n\nTip: buy a 48-hour *GVB* ticket.",
        });
        return c;
    }

    static Conversation CodeDemo(ChatStore store)
    {
        var c = store.New();
        c.Title = "Reverse a string in C#";
        c.Messages.Add(new ChatMessage { Role = "user", Text = "How do I reverse a string in C#?" });
        c.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Text = "Use `Array.Reverse` on the characters:\n\n```csharp\nvar chars = text.ToCharArray();\nArray.Reverse(chars);\nvar reversed = new string(chars);\n```\n\n- works for plain text\n- for emoji use `StringInfo` text elements",
        });
        return c;
    }
}
