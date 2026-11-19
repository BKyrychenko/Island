using System.Text.Json;
using System.Text.Json.Nodes;
using Isle.Agents;

namespace Isle.Modules.Agents;

public enum AgentState { Idle, Done, Working, Waiting }

/// <summary>One hook call as received from Isle.Hook.exe.</summary>
public sealed record HookMessage(string Agent, bool Wait, int TimeoutSeconds, JsonObject Input)
{
    public string Event => HookProtocol.Str(Input, "hook_event_name");

    public static HookMessage? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        try
        {
            if (JsonNode.Parse(line) is not JsonObject o || o["v"] is not JsonValue v ||
                !v.TryGetValue<int>(out var version) || version != HookProtocol.Version || o["input"] is not JsonObject input) return null;
            var agent = HookProtocol.Str(o, "agent") is { Length: > 0 } a ? a : "claude";
            var wait = o["wait"] is JsonValue w && w.TryGetValue<bool>(out var b) && b;
            var timeout = o["timeout"] is JsonValue t && t.TryGetValue<int>(out var n) ? Math.Clamp(n, 5, 3600) : HookProtocol.DefaultTimeoutSeconds;
            return new HookMessage(agent, wait, timeout, input);
        }
        catch (JsonException) { return null; }
    }
}

public sealed record AgentQuestion(string Text, string Header, IReadOnlyList<(string Label, string Description)> Options, bool Multi);

/// <summary>A hook call blocked until the user answers in the island (or the hook gives up).</summary>
public sealed class AgentRequest
{
    static long _ids;
    public long Id { get; } = Interlocked.Increment(ref _ids);
    public required AgentSession Session { get; init; }
    /// <summary>True: AskUserQuestion (options); false: permission (Allow / Deny).</summary>
    public bool IsQuestion { get; init; }
    public string Tool { get; init; } = "";
    public string Detail { get; init; } = "";
    public IReadOnlyList<AgentQuestion> Questions { get; init; } = [];
    /// <summary>Completed with the reply line for the hook (<see cref="HookProtocol.Reply"/>).</summary>
    public TaskCompletionSource<string> Reply { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class AgentSession
{
    public required string Key { get; init; }
    public required string Agent { get; init; }
    public string Project { get; set; } = "";
    public string Cwd { get; set; } = "";
    public AgentState Reported { get; set; } = AgentState.Idle;
    public string LastAction { get; set; } = "";
    public DateTimeOffset Updated { get; set; }
    public List<AgentRequest> Pending { get; } = [];

    /// <summary>A pending request always wins over the last reported state.</summary>
    public AgentState State => Pending.Count > 0 ? AgentState.Waiting : Reported;
}

/// <summary>
/// All known agent sessions, fed by hook messages. Pure (no UI, injectable clock) — call it on one thread.
/// Event mapping (Claude Code and Codex share the names): SessionStart → idle; UserPromptSubmit / PreToolUse →
/// working; PermissionRequest / PreToolUse(AskUserQuestion) → waiting with a pending request; Notification
/// permission_prompt / elicitation_dialog / agent_needs_input → waiting; Stop → done; SessionEnd → removed.
/// </summary>
public sealed class AgentBook(Func<DateTimeOffset> now)
{
    public static readonly TimeSpan Stale = TimeSpan.FromHours(3);
    readonly Dictionary<string, AgentSession> _sessions = [];

    /// <summary>Raised for a Stop event (the pill shows a short "done" check).</summary>
    public event Action<AgentSession>? Finished;

    public int Count => _sessions.Count;

    /// <summary>Most urgent first (waiting, working, done, idle), then most recent.</summary>
    public IReadOnlyList<AgentSession> Sessions =>
        [.. _sessions.Values.OrderByDescending(s => s.State).ThenByDescending(s => s.Updated)];

    public AgentState Urgent => _sessions.Count == 0 ? AgentState.Idle : _sessions.Values.Max(s => s.State);

    public IEnumerable<AgentRequest> AllPending => _sessions.Values.SelectMany(s => s.Pending);

    /// <summary>Applies one hook message. Returns the request to wait on when the hook waits for an answer
    /// (<paramref name="allowRequests"/> false = status only, the hook gets "none" at once).</summary>
    public AgentRequest? Apply(HookMessage m, bool allowRequests = true)
    {
        var t = now();
        Prune(t);
        var input = m.Input;
        var sessionId = HookProtocol.Str(input, "session_id");
        var cwd = HookProtocol.Str(input, "cwd");
        var key = m.Agent + ":" + (sessionId.Length > 0 ? sessionId : cwd);

        if (m.Event == "SessionEnd")
        {
            if (_sessions.Remove(key, out var gone)) CancelAll(gone);
            return null;
        }

        if (!_sessions.TryGetValue(key, out var s))
        {
            s = new AgentSession { Key = key, Agent = m.Agent };
            _sessions[key] = s;
        }
        if (cwd.Length > 0) { s.Cwd = cwd; s.Project = ProjectName(cwd); }
        s.Updated = t;

        var tool = HookProtocol.Str(input, "tool_name");
        switch (m.Event)
        {
            case "SessionStart":
                if (s.Reported != AgentState.Working) s.Reported = AgentState.Idle;
                break;
            case "UserPromptSubmit":
                s.Reported = AgentState.Working;
                s.LastAction = AgentText.OneLine(HookProtocol.Str(input, "prompt"));
                break;
            case "PreToolUse" or "PostToolUse":
                s.Reported = AgentState.Working;
                if (tool != HookProtocol.AskUserQuestion) s.LastAction = AgentText.Describe(tool, input["tool_input"] as JsonObject);
                break;
            case "Notification":
                var type = HookProtocol.Str(input, "notification_type");
                var msg = AgentText.OneLine(HookProtocol.Str(input, "message"));
                if (type is "permission_prompt" or "elicitation_dialog" or "agent_needs_input")
                {
                    s.Reported = AgentState.Waiting;
                    if (msg.Length > 0) s.LastAction = msg;
                }
                else if (type == "agent_completed") s.Reported = AgentState.Done;
                break;
            case "Stop":
                s.Reported = AgentState.Done;
                var last = AgentText.OneLine(HookProtocol.Str(input, "last_assistant_message"));
                if (last.Length > 0) s.LastAction = last;
                Finished?.Invoke(s);
                break;
        }

        if (!m.Wait || !allowRequests) return null;
        AgentRequest? r = null;
        if (m.Event == "PermissionRequest")
        {
            var ti = input["tool_input"] as JsonObject;
            r = new AgentRequest { Session = s, Tool = tool, Detail = AgentText.Detail(tool, ti) };
        }
        else if (m.Event == "PreToolUse" && tool == HookProtocol.AskUserQuestion)
        {
            var qs = ParseQuestions(input["tool_input"] as JsonObject);
            if (qs.Count > 0) r = new AgentRequest { Session = s, IsQuestion = true, Tool = tool, Questions = qs };
        }
        if (r is null) return null;
        s.Pending.Add(r);
        return r;
    }

    /// <summary>Answers a pending request (the reply line goes to the hook) and removes it.</summary>
    public bool Resolve(AgentRequest r, string reply)
    {
        if (!r.Session.Pending.Remove(r)) return false;
        r.Reply.TrySetResult(reply);
        if (r.Session.Pending.Count == 0 && r.Session.Reported == AgentState.Waiting) r.Session.Reported = AgentState.Working;
        r.Session.Updated = now();
        return true;
    }

    /// <summary>The hook gave up (timeout / closed) or the user chose the terminal: no decision.</summary>
    public bool Cancel(AgentRequest r) => Resolve(r, HookProtocol.Reply("none"));

    public void Remove(string key)
    {
        if (_sessions.Remove(key, out var s)) CancelAll(s);
    }

    /// <summary>Drops sessions not heard from for <see cref="Stale"/> (crashed / closed terminals without SessionEnd).</summary>
    public bool Prune(DateTimeOffset t)
    {
        var old = _sessions.Values.Where(s => s.Pending.Count == 0 && t - s.Updated > Stale).Select(s => s.Key).ToList();
        foreach (var k in old) _sessions.Remove(k);
        return old.Count > 0;
    }

    /// <summary>"Working" with no hook event for this long is shown as idle (an interrupted run sends no Stop).</summary>
    public static readonly TimeSpan WorkingSilence = TimeSpan.FromMinutes(15);

    /// <summary>Demotes silent working sessions and prunes stale ones. True when anything changed.</summary>
    public bool Tidy(DateTimeOffset t)
    {
        var changed = Prune(t);
        foreach (var s in _sessions.Values)
            if (s.Reported == AgentState.Working && s.Pending.Count == 0 && t - s.Updated > WorkingSilence)
            {
                s.Reported = AgentState.Idle;
                changed = true;
            }
        return changed;
    }

    void CancelAll(AgentSession s)
    {
        foreach (var r in s.Pending.ToList()) r.Reply.TrySetResult(HookProtocol.Reply("none"));
        s.Pending.Clear();
    }

    public static string ProjectName(string cwd)
    {
        var trimmed = cwd.TrimEnd('\\', '/');
        var name = Path.GetFileName(trimmed);
        return name.Length > 0 ? name : trimmed;
    }

    public static List<AgentQuestion> ParseQuestions(JsonObject? toolInput)
    {
        var list = new List<AgentQuestion>();
        if (toolInput?["questions"] is not JsonArray arr) return list;
        foreach (var n in arr)
        {
            if (n is not JsonObject q) continue;
            var text = HookProtocol.Str(q, "question");
            if (text.Length == 0) continue;
            var opts = new List<(string, string)>();
            if (q["options"] is JsonArray oa)
                foreach (var o in oa)
                    if (o is JsonObject oo && HookProtocol.Str(oo, "label") is { Length: > 0 } label)
                        opts.Add((label, HookProtocol.Str(oo, "description")));
            var multi = q["multiSelect"] is JsonValue mv && mv.TryGetValue<bool>(out var mb) && mb;
            if (opts.Count > 0) list.Add(new AgentQuestion(text, HookProtocol.Str(q, "header"), opts, multi));
        }
        return list;
    }
}
