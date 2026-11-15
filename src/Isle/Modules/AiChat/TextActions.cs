namespace Isle.Modules.AiChat;

/// <summary>The text actions offered for a selection (Win+Alt+A) and the prompts they send.</summary>
public enum TextAction { Summarise, TranslateEn, TranslateUk, TranslateNl, Fix, Explain, Reply }

public static class TextActions
{
    public const int MaxChars = 60_000;

    public static readonly TextAction[] All = Enum.GetValues<TextAction>();

    public static string TitleKey(TextAction a) => a switch
    {
        TextAction.Summarise => "ta.summarise",
        TextAction.TranslateEn => "ta.toEn",
        TextAction.TranslateUk => "ta.toUk",
        TextAction.TranslateNl => "ta.toNl",
        TextAction.Fix => "ta.fix",
        TextAction.Explain => "ta.explain",
        _ => "ta.reply",
    };

    /// <summary>Whether the result is meant to replace the selection (offer "Replace" first).</summary>
    public static bool Replaces(TextAction a) => a is TextAction.TranslateEn or TextAction.TranslateUk or TextAction.TranslateNl or TextAction.Fix;

    /// <summary>System prompt for the action; <paramref name="uiLanguage"/> ("en"/"uk") is the language for
    /// explanations and summaries.</summary>
    public static string SystemPrompt(TextAction a, string uiLanguage)
    {
        var lang = uiLanguage == "uk" ? "Ukrainian" : "English";
        const string only = " Output only the result, with no preamble, quotes or comments.";
        return a switch
        {
            TextAction.Summarise => $"Summarise the user's text in {lang}: a few short bullet points or 1–3 sentences, keeping key facts, names and numbers." + only,
            TextAction.TranslateEn => "Translate the user's text into English. Keep the meaning, tone, formatting and line breaks." + only,
            TextAction.TranslateUk => "Translate the user's text into Ukrainian. Keep the meaning, tone, formatting and line breaks." + only,
            TextAction.TranslateNl => "Translate the user's text into Dutch. Keep the meaning, tone, formatting and line breaks." + only,
            TextAction.Fix => "Fix spelling, grammar and punctuation in the user's text. Keep its language, meaning, tone and formatting; change as little as possible." + only,
            TextAction.Explain => $"Explain the user's text simply and briefly in {lang}: what it means, and any terms a non-expert would not know.",
            _ => "Write a short, friendly reply to the message the user received (the text below), in the same language as that message." + only,
        };
    }

    /// <summary>One-off conversation for the action (not stored).</summary>
    public static Conversation Build(TextAction a, string selection, string uiLanguage)
    {
        var text = selection.Length > MaxChars ? selection[..MaxChars] : selection;
        var c = new Conversation { SystemPrompt = SystemPrompt(a, uiLanguage) };
        c.Messages.Add(new ChatMessage { Role = "user", Text = text });
        return c;
    }

    /// <summary>One-off conversation for a command-bar question: a short answer.</summary>
    public static Conversation Question(string question, string uiLanguage)
    {
        var c = new Conversation
        {
            SystemPrompt = "Answer the question briefly: at most a few sentences or a short list. Answer in the language of the question" +
                (uiLanguage == "uk" ? " (Ukrainian if unclear)." : " (English if unclear)."),
        };
        c.Messages.Add(new ChatMessage { Role = "user", Text = question });
        return c;
    }
}
