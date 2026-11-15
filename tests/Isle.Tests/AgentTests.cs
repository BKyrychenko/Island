using System.Text.Json.Nodes;
using Isle.Agents;
using Isle.Modules.Agents;

namespace Isle.Tests;

public class AgentTests
{
    static JsonObject Input(string ev, string tool = "", JsonObject? toolInput = null, string session = "s1", string cwd = @"C:\Code\island")
    {
        var o = new JsonObject { ["session_id"] = session, ["cwd"] = cwd, ["hook_event_name"] = ev };
        if (tool.Length > 0) o["tool_name"] = tool;
        if (toolInput is not null) o["tool_input"] = toolInput;
        return o;
    }

    static JsonObject AskInput() => Input("PreToolUse", "AskUserQuestion", new JsonObject
    {
        ["questions"] = new JsonArray(
            new JsonObject
            {
                ["question"] = "Which framework?", ["header"] = "Framework",
                ["options"] = new JsonArray(new JsonObject { ["label"] = "React", ["description"] = "lib" }, new JsonObject { ["label"] = "Vue" }),
                ["multiSelect"] = false,
            },
            new JsonObject
            {
                ["question"] = "Extras?", ["header"] = "Extras", ["multiSelect"] = true,
                ["options"] = new JsonArray(new JsonObject { ["label"] = "Tests" }, new JsonObject { ["label"] = "Docs" }, new JsonObject { ["label"] = "CI" }),
            }),
    });

    // ---- protocol / hook output JSON ----

    [Theory]
    [InlineData("PermissionRequest", "Bash", false, true)]
    [InlineData("PermissionRequest", "AskUserQuestion", false, false)]
    [InlineData("PreToolUse", "AskUserQuestion", false, true)]
    [InlineData("PreToolUse", "AskUserQuestion", true, false)]
    [InlineData("PreToolUse", "Bash", false, false)]
    [InlineData("PermissionRequest", "Bash", true, false)]
    [InlineData("Stop", "", false, false)]
    [InlineData("Notification", "", false, false)]
    public void NeedsAnswer(string ev, string tool, bool async, bool expected) =>
        Assert.Equal(expected, HookProtocol.NeedsAnswer(Input(ev, tool), async));

    [Fact]
    public void PermissionAllowOutput()
    {
        var output = HookProtocol.BuildOutput(Input("PermissionRequest", "Bash"), HookProtocol.Reply("allow"))!;
        var o = JsonNode.Parse(output)!["hookSpecificOutput"]!;
        Assert.Equal("PermissionRequest", (string?)o["hookEventName"]);
        Assert.Equal("allow", (string?)o["decision"]!["behavior"]);
        Assert.Null(o["decision"]!["message"]);
    }

    [Fact]
    public void PermissionDenyOutputHasMessage()
    {
        var output = HookProtocol.BuildOutput(Input("PermissionRequest", "Bash"), HookProtocol.Reply("deny", "no thanks"))!;
        var d = JsonNode.Parse(output)!["hookSpecificOutput"]!["decision"]!;
        Assert.Equal("deny", (string?)d["behavior"]);
        Assert.Equal("no thanks", (string?)d["message"]);
    }

    [Theory]
    [InlineData("""{"decision":"none"}""")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("garbage{")]
    [InlineData("""{"decision":"answer"}""")]
    public void NoDecisionPrintsNothing(string? reply)
    {
        Assert.Null(HookProtocol.BuildOutput(Input("PermissionRequest", "Bash"), reply));
        Assert.Null(HookProtocol.BuildOutput(AskInput(), reply));
    }

    [Fact]
    public void QuestionAnswerKeepsInputAndAddsAnswers()
    {
        var input = AskInput();
        var reply = HookProtocol.Reply("answer", answers: new Dictionary<string, string> { ["Which framework?"] = "React", ["Extras?"] = "Tests, CI" });
        var o = JsonNode.Parse(HookProtocol.BuildOutput(input, reply)!)!["hookSpecificOutput"]!;
        Assert.Equal("PreToolUse", (string?)o["hookEventName"]);
        Assert.Equal("allow", (string?)o["permissionDecision"]);
        var updated = o["updatedInput"]!;
        Assert.Equal(2, updated["questions"]!.AsArray().Count);
        Assert.Equal("React", (string?)updated["answers"]!["Which framework?"]);
        Assert.Equal("Tests, CI", (string?)updated["answers"]!["Extras?"]);
        Assert.Null(input["tool_input"]!["answers"]); // original untouched
    }

    [Fact]
    public void RequestLineRoundTrip()
    {
        var line = HookProtocol.Request("codex", true, 45, Input("PermissionRequest", "shell"));
        Assert.DoesNotContain('\n', line);
        var m = HookMessage.Parse(line)!;
        Assert.Equal("codex", m.Agent);
        Assert.True(m.Wait);
        Assert.Equal(45, m.TimeoutSeconds);
        Assert.Equal("PermissionRequest", m.Event);
        Assert.Null(HookMessage.Parse("{\"agent\":\"x\"}"));
        Assert.Null(HookMessage.Parse("not json"));
    }

    // ---- session book ----

    static (AgentBook Book, Func<DateTimeOffset> Clock, Action<TimeSpan> Advance) NewBook()
    {
        var t = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
        return (new AgentBook(() => t), () => t, d => t += d);
    }

    static HookMessage Msg(JsonObject input, string agent = "claude", bool wait = false) => new(agent, wait, 60, input);

    [Fact]
    public void LifecycleStates()
    {
        var (b, _, _) = NewBook();
        b.Apply(Msg(Input("SessionStart")));
        var s = Assert.Single(b.Sessions);
        Assert.Equal("island", s.Project);
        Assert.Equal(AgentState.Idle, s.State);

        var prompt = Input("UserPromptSubmit");
        prompt["prompt"] = "\n  fix   the\tbuild  \nsecond line";
        b.Apply(Msg(prompt));
        Assert.Equal(AgentState.Working, s.State);
        Assert.Equal("fix the build", s.LastAction);

        b.Apply(Msg(Input("PreToolUse", "Bash", new JsonObject { ["command"] = "npm test" })));
        Assert.Equal("Bash · npm test", s.LastAction);

        var stop = Input("Stop");
        stop["last_assistant_message"] = "All done.";
        AgentSession? finished = null;
        b.Finished += x => finished = x;
        b.Apply(Msg(stop));
        Assert.Equal(AgentState.Done, s.State);
        Assert.Equal("All done.", s.LastAction);
        Assert.Same(s, finished);

        b.Apply(Msg(Input("SessionEnd")));
        Assert.Equal(0, b.Count);
    }

    [Fact]
    public void PermissionRequestWaitsUntilResolved()
    {
        var (b, _, _) = NewBook();
        b.Apply(Msg(Input("UserPromptSubmit")));
        var r = b.Apply(Msg(Input("PermissionRequest", "Bash", new JsonObject { ["command"] = "rm -rf build" }), wait: true))!;
        Assert.NotNull(r);
        Assert.False(r.IsQuestion);
        Assert.Equal("rm -rf build", r.Detail);
        Assert.Equal(AgentState.Waiting, b.Urgent);
        // status messages do not hide a pending request
        b.Apply(Msg(Input("PreToolUse", "Read", new JsonObject { ["file_path"] = @"C:\a\b.cs" })));
        Assert.Equal(AgentState.Waiting, b.Sessions[0].State);

        Assert.True(b.Resolve(r, HookProtocol.Reply("allow")));
        Assert.True(r.Reply.Task.IsCompleted);
        Assert.Contains("allow", r.Reply.Task.Result);
        Assert.Equal(AgentState.Working, b.Sessions[0].State);
        Assert.False(b.Resolve(r, HookProtocol.Reply("deny"))); // only once
    }

    [Fact]
    public void RequestsOffGiveStatusOnly()
    {
        var (b, _, _) = NewBook();
        Assert.Null(b.Apply(Msg(Input("PermissionRequest", "Bash"), wait: true), allowRequests: false));
        Assert.Null(b.Apply(Msg(Input("PermissionRequest", "Bash"), wait: false)));
        Assert.Empty(b.AllPending);
    }

    [Fact]
    public void QuestionRequestParsesOptions()
    {
        var (b, _, _) = NewBook();
        var r = b.Apply(Msg(AskInput(), wait: true))!;
        Assert.True(r.IsQuestion);
        Assert.Equal(2, r.Questions.Count);
        Assert.Equal(["React", "Vue"], r.Questions[0].Options.Select(o => o.Label));
        Assert.True(r.Questions[1].Multi);
        var reply = AgentText.AnswerReply(r.Questions, [new[] { "Vue" }, new[] { "CI", "Tests" }]);
        var answers = JsonNode.Parse(reply)!["answers"]!;
        Assert.Equal("Vue", (string?)answers["Which framework?"]);
        Assert.Equal("Tests, CI", (string?)answers["Extras?"]); // option order, not click order
        Assert.Contains("\"none\"", AgentText.AnswerReply(r.Questions, [Array.Empty<string>(), Array.Empty<string>()]));
    }

    [Fact]
    public void CancelAndSessionEndReleaseHooks()
    {
        var (b, _, _) = NewBook();
        var r1 = b.Apply(Msg(Input("PermissionRequest", "Bash"), wait: true))!;
        Assert.True(b.Cancel(r1));
        Assert.Contains("none", r1.Reply.Task.Result);
        var r2 = b.Apply(Msg(Input("PermissionRequest", "Edit"), wait: true))!;
        b.Apply(Msg(Input("SessionEnd")));
        Assert.Contains("none", r2.Reply.Task.Result);
        Assert.Equal(0, b.Count);
    }

    [Fact]
    public void SessionsAreKeyedByAgentAndOrderedByUrgency()
    {
        var (b, _, advance) = NewBook();
        b.Apply(Msg(Input("Stop", session: "a", cwd: @"C:\p\one")));
        advance(TimeSpan.FromSeconds(1));
        b.Apply(Msg(Input("UserPromptSubmit", session: "a", cwd: @"C:\p\two"), agent: "codex"));
        advance(TimeSpan.FromSeconds(1));
        b.Apply(Msg(Input("PermissionRequest", "Bash", session: "b", cwd: @"C:\p\three\"), wait: true));
        Assert.Equal(3, b.Count);
        Assert.Equal(["three", "two", "one"], b.Sessions.Select(s => s.Project));
    }

    [Fact]
    public void NotificationsMapToStates()
    {
        var (b, _, _) = NewBook();
        var n = Input("Notification");
        n["notification_type"] = "permission_prompt";
        n["message"] = "Claude needs your permission to use Bash";
        b.Apply(Msg(n));
        Assert.Equal(AgentState.Waiting, b.Sessions[0].State);
        Assert.Equal("Claude needs your permission to use Bash", b.Sessions[0].LastAction);
        var idle = Input("Notification");
        idle["notification_type"] = "idle_prompt";
        b.Apply(Msg(idle));
        Assert.Equal(AgentState.Waiting, b.Sessions[0].State); // idle prompt changes nothing
        var done = Input("Notification");
        done["notification_type"] = "agent_completed";
        b.Apply(Msg(done));
        Assert.Equal(AgentState.Done, b.Sessions[0].State);
    }

    [Fact]
    public void TidyDemotesSilentWorkAndPrunesStale()
    {
        var (b, clock, advance) = NewBook();
        b.Apply(Msg(Input("UserPromptSubmit", session: "w")));
        b.Apply(Msg(Input("Stop", session: "d")));
        advance(TimeSpan.FromMinutes(16));
        Assert.True(b.Tidy(clock()));
        Assert.All(b.Sessions, s => Assert.NotEqual(AgentState.Working, s.State));
        advance(AgentBook.Stale);
        Assert.True(b.Tidy(clock()));
        Assert.Equal(0, b.Count);
    }

    // ---- text ----

    [Theory]
    [InlineData("Bash", """{"command":"git status"}""", "Bash · git status")]
    [InlineData("Edit", """{"file_path":"C:\\src\\App.cs","old_string":"a"}""", "Edit · App.cs")]
    [InlineData("Grep", """{"pattern":"TODO","path":"src"}""", "Grep · TODO")]
    [InlineData("WebFetch", """{"url":"https://example.com","prompt":"x"}""", "WebFetch · https://example.com")]
    [InlineData("shell", """{"command":["npm","run","build"]}""", "shell · npm run build")]
    [InlineData("apply_patch", """{"input":"*** Begin Patch\n*** Update File: src/a.rs\n*** Add File: b.md\n*** End Patch"}""", "apply_patch · a.rs, b.md")]
    [InlineData("mcp__db__query", """{"sql":"select 1"}""", "mcp__db__query · select 1")]
    [InlineData("TodoWrite", """{"todos":[]}""", "TodoWrite")]
    public void DescribesTools(string tool, string json, string expected) =>
        Assert.Equal(expected, AgentText.Describe(tool, JsonNode.Parse(json) as JsonObject));

    [Fact]
    public void OneLineSkipsPastedBlocksAndCuts()
    {
        Assert.Equal("hello world", AgentText.OneLine("<pasted_content id=\"1\">\n\n  hello   world \nmore"));
        Assert.Equal("Heading", AgentText.OneLine("## Heading\ntext"));
        var cut = AgentText.OneLine(new string('x', 500), 20);
        Assert.Equal(20, cut.Length);
        Assert.EndsWith("…", cut);
        Assert.Equal("weather-api", AgentBook.ProjectName(@"C:\Code\weather-api\"));
        Assert.Equal("proj", AgentBook.ProjectName("/home/me/proj"));
    }

    // ---- installer ----

    const string ExistingClaude = """
        {
          "hooks": {
            "SessionStart": [ { "hooks": [ { "type": "command", "command": "other-tool hook", "timeout": 5 } ] } ],
            "PostToolUse": [ { "matcher": "Edit|Write", "hooks": [ { "type": "command", "command": "fmt" } ] } ]
          },
          "model": "claude-opus-5",
          "description": "keep me"
        }
        """;

    [Fact]
    public void MergeKeepsOtherHooksAndIsIdempotent()
    {
        var root = (JsonObject)JsonNode.Parse(ExistingClaude)!;
        HookInstaller.Merge(root, "claude", @"C:\Isle\bin\Isle.Hook.exe", 60);
        HookInstaller.Merge(root, "claude", @"C:\Isle\bin\Isle.Hook.exe", 90); // update, no duplicates
        Assert.True(HookInstaller.HasIsle(root));
        Assert.Equal("claude-opus-5", (string?)root["model"]);
        var hooks = root["hooks"]!.AsObject();
        Assert.Equal(2, hooks["SessionStart"]!.AsArray().Count); // theirs + ours
        Assert.Equal("other-tool hook", (string?)hooks["SessionStart"]![0]!["hooks"]![0]!["command"]);
        Assert.Single(hooks["PostToolUse"]!.AsArray());
        Assert.Equal(2, hooks["PreToolUse"]!.AsArray().Count); // async status + AskUserQuestion
        var perm = hooks["PermissionRequest"]![0]!["hooks"]![0]!;
        Assert.Equal(@"C:\Isle\bin\Isle.Hook.exe", (string?)perm["command"]);
        Assert.Equal(["claude", "--timeout", "90"], perm["args"]!.AsArray().Select(x => (string?)x));
        Assert.Equal(120, (int)perm["timeout"]!);
        Assert.Null(perm["async"]);
        var ask = hooks["PreToolUse"]!.AsArray().Single(g => (string?)g!["matcher"] == "AskUserQuestion")!;
        Assert.Null(ask["hooks"]![0]!["async"]);
        var status = hooks["Stop"]![0]!["hooks"]![0]!;
        Assert.True((bool)status["async"]!);
        Assert.Equal(["claude", "--async"], status["args"]!.AsArray().Select(x => (string?)x));
    }

    [Fact]
    public void StripRemovesOnlyIsle()
    {
        var root = (JsonObject)JsonNode.Parse(ExistingClaude)!;
        var before = root.ToJsonString();
        HookInstaller.Merge(root, "claude", @"C:\Isle\bin\Isle.Hook.exe", 60);
        Assert.True(HookInstaller.Strip(root));
        Assert.False(HookInstaller.HasIsle(root));
        Assert.Equal(before, root.ToJsonString());
        Assert.False(HookInstaller.Strip(root));

        var empty = new JsonObject();
        HookInstaller.Merge(empty, "codex", @"C:\Isle\bin\Isle.Hook.exe", 60);
        HookInstaller.Strip(empty);
        Assert.Equal("{}", empty.ToJsonString());
    }

    [Fact]
    public void CodexEntriesUseShellCommand()
    {
        var root = new JsonObject();
        HookInstaller.Merge(root, "codex", @"C:\Program Files\Isle\Isle.Hook.exe", 60);
        var hooks = root["hooks"]!.AsObject();
        Assert.Null(hooks["Notification"]);
        var perm = hooks["PermissionRequest"]![0]!["hooks"]![0]!;
        Assert.Equal("\"C:\\Program Files\\Isle\\Isle.Hook.exe\" codex --timeout 60", (string?)perm["command"]);
        Assert.Null(perm["args"]);
        Assert.Null(perm["async"]);
        Assert.Null(hooks["PreToolUse"]![0]!["matcher"]);
    }

    [Fact]
    public void InstallAndRemoveFilesWithBackup()
    {
        using var dir = new TempDir();
        var cfg = Path.Combine(dir.Path, ".claude", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(cfg)!);
        File.WriteAllText(cfg, ExistingClaude);
        var src = Path.Combine(dir.Path, "app");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "Isle.Hook.exe"), "fake");
        var t = new HookTarget("claude", cfg);
        var now = new DateTime(2026, 11, 15, 12, 0, 0);

        var plan = HookInstaller.Plan(t, now);
        Assert.True(plan.FileExists);
        Assert.False(plan.AlreadyInstalled);
        Assert.Contains("PreToolUse (AskUserQuestion)", plan.Events);

        var backup = HookInstaller.Install(t, 60, src, now)!;
        Assert.Equal(ExistingClaude, File.ReadAllText(backup));
        Assert.True(HookInstaller.IsInstalled(t));
        Assert.True(File.Exists(HookInstaller.InstalledExe));

        var backup2 = HookInstaller.Remove(t, now.AddMinutes(1))!;
        Assert.NotEqual(backup, backup2);
        Assert.False(HookInstaller.IsInstalled(t));
        var after = (JsonObject)JsonNode.Parse(File.ReadAllText(cfg))!;
        Assert.Equal("keep me", (string?)after["description"]);
        Assert.Equal(2, after["hooks"]!.AsObject().Count);
        Assert.Null(HookInstaller.Remove(t, now.AddMinutes(2))); // nothing left to remove
    }

    [Fact]
    public void InvalidConfigIsNeverOverwritten()
    {
        using var dir = new TempDir();
        var cfg = Path.Combine(dir.Path, "settings.json");
        File.WriteAllText(cfg, "{ \"hooks\": { // comment\n } }");
        var src = Path.Combine(dir.Path, "app");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "Isle.Hook.exe"), "fake");
        var t = new HookTarget("claude", cfg);
        Assert.Throws<InvalidDataException>(() => HookInstaller.Install(t, 60, src, DateTime.Now));
        Assert.Equal("{ \"hooks\": { // comment\n } }", File.ReadAllText(cfg));
        Assert.Single(Directory.GetFiles(dir.Path)); // no backup, no temp file
    }

    [Fact]
    public void InstallCreatesMissingFile()
    {
        using var dir = new TempDir();
        var cfg = Path.Combine(dir.Path, ".codex", "hooks.json");
        Directory.CreateDirectory(Path.GetDirectoryName(cfg)!);
        var src = Path.Combine(dir.Path, "app");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "Isle.Hook.exe"), "fake");
        Assert.Null(HookInstaller.Install(new HookTarget("codex", cfg), 30, src, DateTime.Now));
        Assert.True(HookInstaller.IsInstalled(new HookTarget("codex", cfg)));
    }

    // ---- pipe: the real client against the real server ----

    static string UniquePipe() => "Isle.Agents.test." + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task ClientFailsOpenWithoutIsland()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var output = await HookProtocol.RunClientAsync(["claude", "--timeout", "5"], Input("PermissionRequest", "Bash").ToJsonString(), UniquePipe());
        Assert.Null(output);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"took {sw.ElapsedMilliseconds} ms");
        Assert.Null(await HookProtocol.RunClientAsync(["claude"], "not json", UniquePipe()));
        Assert.Null(await HookProtocol.RunClientAsync([], "", UniquePipe()));
    }

    [Fact]
    public async Task PermissionRoundTripThroughPipe()
    {
        var name = UniquePipe();
        var got = new TaskCompletionSource<HookMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new AgentPipeServer(name, (m, ct) =>
        {
            got.TrySetResult(m);
            return Task.FromResult<string?>(m.Wait ? HookProtocol.Reply("deny", "not now") : null);
        });
        server.Start();
        var output = await HookProtocol.RunClientAsync(["claude", "--timeout", "10"], Input("PermissionRequest", "Bash", new JsonObject { ["command"] = "rm x" }).ToJsonString(), name, 2000);
        var m = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(m.Wait);
        Assert.Equal(10, m.TimeoutSeconds);
        Assert.Equal("Bash", HookProtocol.Str(m.Input, "tool_name"));
        var d = JsonNode.Parse(output!)!["hookSpecificOutput"]!["decision"]!;
        Assert.Equal("deny", (string?)d["behavior"]);
        Assert.Equal("not now", (string?)d["message"]);
    }

    [Fact]
    public async Task StatusMessagesDoNotWait()
    {
        var name = UniquePipe();
        var got = new TaskCompletionSource<HookMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new AgentPipeServer(name, (m, ct) => { got.TrySetResult(m); return Task.FromResult<string?>(null); });
        server.Start();
        var output = await HookProtocol.RunClientAsync(["claude", "--async"], Input("Stop").ToJsonString(), name, 2000);
        Assert.Null(output);
        var m = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(m.Wait);
        Assert.Equal("Stop", m.Event);
        // async status hook skips AskUserQuestion (the synchronous hook reports it)
        Assert.Null(await HookProtocol.RunClientAsync(["claude", "--async"], AskInput().ToJsonString(), name, 2000));
    }

    [Fact]
    public async Task HookTimeoutCancelsServerWait()
    {
        var name = UniquePipe();
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new AgentPipeServer(name, async (m, ct) =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.TrySetResult(); }
            return HookProtocol.Reply("none");
        });
        server.Start();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var output = await HookProtocol.RunClientAsync(["claude", "--timeout", "5"], Input("PermissionRequest", "Bash").ToJsonString(), name, 2000);
        Assert.Null(output); // timed out → no decision → terminal prompt
        Assert.InRange(sw.Elapsed.TotalSeconds, 4.5, 8);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(3)); // the server noticed the hook left
    }

    [Fact]
    public async Task QuestionRoundTripThroughPipe()
    {
        var name = UniquePipe();
        using var server = new AgentPipeServer(name, (m, ct) =>
            Task.FromResult<string?>(HookProtocol.Reply("answer", answers: new Dictionary<string, string> { ["Which framework?"] = "Vue" })));
        server.Start();
        var output = await HookProtocol.RunClientAsync(["claude", "--timeout", "10"], AskInput().ToJsonString(), name, 2000);
        var o = JsonNode.Parse(output!)!["hookSpecificOutput"]!;
        Assert.Equal("allow", (string?)o["permissionDecision"]);
        Assert.Equal("Vue", (string?)o["updatedInput"]!["answers"]!["Which framework?"]);
    }

    [Fact]
    public async Task DelayedAnswerReachesHook()
    {
        var name = UniquePipe();
        using var server = new AgentPipeServer(name, async (m, ct) =>
        {
            await Task.Delay(800, ct);
            return HookProtocol.Reply("allow");
        });
        server.Start();
        var output = await HookProtocol.RunClientAsync(["claude", "--timeout", "10"], Input("PermissionRequest", "Bash").ToJsonString(), name, 2000);
        Assert.NotNull(output);
        Assert.Equal("allow", (string?)JsonNode.Parse(output!)!["hookSpecificOutput"]!["decision"]!["behavior"]);
    }
}
