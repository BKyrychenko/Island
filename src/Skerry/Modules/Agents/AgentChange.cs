using System.Text;
using System.Text.Json.Nodes;
using Skerry.Agents;

namespace Skerry.Modules.Agents;

public sealed record AgentChange(string Path, string Preview, int? Added, int? Removed, bool Limited)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    internal static AgentChange? Read(string agent, string tool, JsonObject input)
    {
        if (input["tool_input"] is not JsonObject args) return null;
        if (agent == "codex" && tool == "apply_patch")
        {
            var result = HookProtocol.Str(input, "tool_response");
            if (!result.StartsWith("Exit code: 0\n", StringComparison.Ordinal) &&
                !result.StartsWith("Exit code: 0\r\n", StringComparison.Ordinal) &&
                !result.StartsWith("Success. Updated the following files:", StringComparison.Ordinal)) return null;
            return CodexPatch(HookProtocol.Str(args, "command"));
        }
        if (agent != "claude" || tool is not ("Edit" or "Write" or "MultiEdit")) return null;
        var path = HookProtocol.Str(args, "file_path");
        if (path.Length is 0 or > 32767) return null;

        var resultObject = input["tool_response"] as JsonObject;
        var preview = new PreviewBuilder(path);
        if (resultObject?["structuredPatch"] is JsonArray { Count: > 0 } patches)
        {
            foreach (var hunk in patches.OfType<JsonObject>())
            {
                if (hunk["lines"] is not JsonArray lines) continue;
                var heading = FormattableString.Invariant($"@@ -{Integer(hunk, "oldStart")},{Integer(hunk, "oldLines")} +{Integer(hunk, "newStart")},{Integer(hunk, "newLines")} @@");
                preview.Add(heading);
                foreach (var item in lines.OfType<JsonValue>())
                    if (item.TryGetValue<string>(out var line)) preview.Add(line);
            }
            if (preview.HasChanges) return preview.Build();
        }

        if (tool == "Write" && args["content"] is JsonValue content && content.TryGetValue<string>(out var after))
        {
            if (resultObject is not null && HookProtocol.Str(resultObject, "type") == "create")
                return Compare(path, "", after);
            if (resultObject?["originalFile"] is JsonValue original && original.TryGetValue<string>(out var before))
                return Compare(path, before, after);
        }
        if (tool == "Edit" && args["old_string"] is JsonValue oldValue && oldValue.TryGetValue<string>(out var oldText) &&
            args["new_string"] is JsonValue newValue && newValue.TryGetValue<string>(out var newText))
        {
            var repeated = args["replace_all"] is JsonValue replace && replace.TryGetValue<bool>(out var every) && every;
            var edit = Compare(path, oldText, newText);
            return repeated ? edit with { Added = null, Removed = null, Limited = true } : edit;
        }
        return new AgentChange(path, "", null, null, true);
    }

    static AgentChange? CodexPatch(string patch)
    {
        using var reader = new StringReader(patch);
        if (reader.ReadLine() != "*** Begin Patch") return null;
        PreviewBuilder? file = null;
        bool removedFile = false;
        while (reader.ReadLine() is { } line)
        {
            if (line == "*** End Patch") return file?.Build(countsKnown: !removedFile);
            var header = line.StartsWith("*** Add File: ", StringComparison.Ordinal) ? "*** Add File: " :
                line.StartsWith("*** Update File: ", StringComparison.Ordinal) ? "*** Update File: " :
                line.StartsWith("*** Delete File: ", StringComparison.Ordinal) ? "*** Delete File: " : "";
            if (header.Length > 0)
            {
                var path = line[header.Length..].Trim();
                file = path.Length is > 0 and <= 32767 ? new PreviewBuilder(path) : null;
                removedFile = header == "*** Delete File: ";
                continue;
            }
            const string move = "*** Move to: ";
            if (file is not null && line.StartsWith(move, StringComparison.Ordinal))
            {
                var path = line[move.Length..].Trim();
                if (path.Length is > 0 and <= 32767) file.Path = path;
            }
            if (line is { Length: > 0 } && (line[0] is '+' or '-' or ' ' || line.StartsWith("@@", StringComparison.Ordinal)))
                file?.Add(line);
        }
        return null;
    }

    static AgentChange Compare(string path, string before, string after)
    {
        var oldLines = Lines(before);
        var newLines = Lines(after);
        var left = 0;
        while (left < oldLines.Length && left < newLines.Length && oldLines[left] == newLines[left]) left++;
        var oldEnd = oldLines.Length;
        var newEnd = newLines.Length;
        while (oldEnd > left && newEnd > left && oldLines[oldEnd - 1] == newLines[newEnd - 1]) { oldEnd--; newEnd--; }
        var preview = new PreviewBuilder(path);
        for (var i = left; i < oldEnd; i++) preview.Add("-" + oldLines[i]);
        for (var i = left; i < newEnd; i++) preview.Add("+" + newLines[i]);
        return preview.Build(countsKnown: preview.HasChanges || before == after);
    }

    static string[] Lines(string text)
    {
        if (text.Length == 0) return [];
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return lines[^1].Length == 0 ? lines[..^1] : lines;
    }

    static int Integer(JsonObject obj, string key) =>
        obj[key] is JsonValue value && value.TryGetValue<int>(out var integer) ? integer : 0;

    sealed class PreviewBuilder(string path)
    {
        const int MaxChars = 12_000, MaxLines = 180;
        readonly StringBuilder _text = new();
        int _lines, _added, _removed;
        bool _limited;
        public string Path { get; set; } = path;
        public bool HasChanges => _added > 0 || _removed > 0;

        public void Add(string line)
        {
            if (line.StartsWith('+')) _added++;
            if (line.StartsWith('-')) _removed++;
            if (_limited || ++_lines > MaxLines || line.Length >= MaxChars - _text.Length)
            {
                _limited = true;
                return;
            }
            _text.Append(line).Append('\n');
        }

        public AgentChange Build(bool countsKnown = true) =>
            new(Path, _text.ToString(), countsKnown ? _added : null, countsKnown ? _removed : null, _limited || !countsKnown);
    }
}
