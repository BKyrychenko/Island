using Isle.Core;

namespace Isle.Modules.AiChat;

/// <summary>All conversations in `chats.json` (atomic writes, corrupt → backup + fresh). Newest first.</summary>
public sealed class ChatStore(string file)
{
    public const int MaxChats = 100;

    public sealed class Data { public List<Conversation> Chats { get; set; } = []; }

    Data? _data;

    Data D => _data ??= File.Exists(file) ? JsonStore.Load<Data>(file) : new Data();

    public IReadOnlyList<Conversation> All => D.Chats;

    public Conversation? Get(string id) => D.Chats.FirstOrDefault(c => c.Id == id);

    public Conversation New(string systemPrompt = "")
    {
        var c = new Conversation { SystemPrompt = systemPrompt };
        D.Chats.Insert(0, c);
        if (D.Chats.Count > MaxChats) D.Chats.RemoveRange(MaxChats, D.Chats.Count - MaxChats);
        return c;
    }

    public void Delete(string id)
    {
        D.Chats.RemoveAll(c => c.Id == id);
        Save();
    }

    /// <summary>Marks a conversation as changed (moves it to the top, sets a title from the first user line) and saves.</summary>
    public void Touch(Conversation c)
    {
        var i = D.Chats.IndexOf(c);
        if (i < 0) return; // Deleted or evicted while an answer was in flight.
        c.Updated = DateTimeOffset.Now;
        if (c.Title.Length == 0 && c.Messages.FirstOrDefault(m => m.Role == "user") is { } first) c.Title = TitleFrom(first.Text, first.Attachments);
        if (i > 0) { D.Chats.RemoveAt(i); D.Chats.Insert(0, c); }
        Save();
    }

    public void Save() => JsonStore.Save(file, D);

    public static string TitleFrom(string text, List<ChatAttachment> attachments)
    {
        var line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "";
        if (line.Length == 0 && attachments.Count > 0) line = attachments[0].Name;
        return line.Length > 48 ? line[..47].TrimEnd() + "…" : line;
    }
}
