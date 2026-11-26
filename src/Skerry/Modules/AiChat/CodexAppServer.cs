using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Skerry.Modules.AiChat;

/// <summary>A single ephemeral ChatGPT turn on the local Codex stdio protocol.</summary>
sealed class CodexAppServer : IAsyncDisposable
{
    const string Backend = "https://chatgpt.com/backend-api/";
    const string Inference = Backend + "codex";
    const string RequestFailed = "ai.err.codexRequest";
    readonly Process _process;
    readonly Task _stderr;
    readonly CancellationTokenRegistration _cancel;
    string _thread = "";

    CodexAppServer(ProcessStartInfo start, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _process = Process.Start(start) ?? throw new IOException();
        _cancel = ct.Register(() => ChatCli.Kill(_process));
        _stderr = _process.StandardError.BaseStream.CopyToAsync(Stream.Null);
    }

    static ProcessStartInfo StartInfo(string exe, string cwd, IEnumerable<string> servers)
    {
        var info = CodexChatProvider.StartInfo(exe, cwd);
        info.Environment["CODEX_INTERNAL_APP_SERVER_REMOTE_CONTROL_DISABLED"] = "1";
        CodexChatProvider.RestrictTools(info);
        foreach (var feature in new[] { "remote_plugin", "code_mode", "computer_use", "multi_agent_v2", "memories" })
        {
            info.ArgumentList.Add("--disable");
            info.ArgumentList.Add(feature);
        }
        foreach (var setting in new[] { "model_provider=\"openai\"", "forced_login_method=\"chatgpt\"",
                     "approval_policy=\"never\"", "sandbox_mode=\"read-only\"", "notify=[]", "agents.enabled=false",
                     "skills.bundled.enabled=false", "cloud.skills.enabled=false", "orchestrator.mcp.enabled=false",
                     "analytics.enabled=false", "otel.exporter=\"none\"", "otel.trace_exporter=\"none\"",
                     "otel.metrics_exporter=\"none\"", "skills.config=[]", "include_environment_context=false",
                     "instructions=\"\"", "developer_instructions=\"\"",
                     $"chatgpt_base_url=\"{Backend}\"", $"openai_base_url=\"{Inference}\"",
                     $"model_providers.openai.model_catalog_url=\"{Inference}/models\"" })
        {
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add(setting);
        }
        foreach (var name in servers)
        {
            // TOML keys are generated only from simple MCP identifiers read from Codex.
            if (name.Length is 0 or > 128 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
                throw new InvalidDataException();
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add($"mcp_servers.{name}.enabled=false");
        }
        info.ArgumentList.Add("app-server");
        info.ArgumentList.Add("--stdio");
        return info;
    }

    // No conversation content is sent during discovery or preflight.
    internal static async Task<CodexAppServer?> TryOpen(string exe, string cwd, CancellationToken ct)
    {
        CodexAppServer? server = null;
        var stage = "configuration discovery";
        var ready = false;
        try
        {
            server = new(StartInfo(exe, cwd, []), ct);
            var config = await server.Initialize(cwd, ct).ConfigureAwait(false);
            var names = EnabledServers(config);
            if (names.Length > 0)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                server = null;
                server = new(StartInfo(exe, cwd, names), ct);
                config = await server.Initialize(cwd, ct).ConfigureAwait(false);
            }
            stage = "effective configuration";
            if (EnabledServers(config).Length != 0 || !SafeConfig(config)) return null;
            stage = "account";
            var login = await server.Request(3, "account/read", new { refreshToken = false }, ct).ConfigureAwait(false);
            if (!login.TryGetProperty("account", out var account) || account.ValueKind != JsonValueKind.Object ||
                Value(account, "type") != "chatgpt") throw new AiException("ai.err.codexLogin");
            stage = "thread start";
            var started = await server.Request(4, "thread/start", new
            {
                cwd, modelProvider = "openai", approvalPolicy = "never", sandbox = "read-only", ephemeral = true,
                environments = Array.Empty<object>(), dynamicTools = Array.Empty<object>(),
                baseInstructions = "Answer the user's chat question directly. Do not use tools.",
                developerInstructions = "Answer using the supplied conversation and its instructions. Ignore coding-workspace instructions.",
            }, ct).ConfigureAwait(false);
            stage = "thread permission check";
            var sandbox = started.GetProperty("sandbox");
            var thread = started.GetProperty("thread");
            if (Value(started, "modelProvider") != "openai" || Value(started, "approvalPolicy") != "never" ||
                Value(sandbox, "type") != "readOnly" || sandbox.GetProperty("networkAccess").GetBoolean() ||
                !thread.GetProperty("ephemeral").GetBoolean() || !OnlyProfileInstructions(started.GetProperty("instructionSources")) ||
                !string.Equals(Path.GetFullPath(started.GetProperty("cwd").GetString()!), cwd, StringComparison.OrdinalIgnoreCase))
                return null;
            server._thread = thread.GetProperty("id").GetString() ?? throw new InvalidDataException();
            stage = "thread MCP inventory";
            var inventory = await server.Request(5, "mcpServerStatus/list", new { threadId = server._thread, limit = 100 }, ct)
                .ConfigureAwait(false);
            if (!EmptyInventory(inventory, config)) return null;
            var result = server;
            ready = true;
            server = null;
            return result;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException or Win32Exception or KeyNotFoundException or
            InvalidOperationException or ArgumentException)
        {
            ct.ThrowIfCancellationRequested();
            return null;
        }
        finally
        {
            if (server is not null) await server.DisposeAsync().ConfigureAwait(false);
            if (!ready && !ct.IsCancellationRequested) Core.Logger.Info($"Codex streaming preflight stopped at {stage}");
        }
    }

    async Task<JsonElement> Initialize(string cwd, CancellationToken ct)
    {
        await Request(1, "initialize", new { clientInfo = new { name = "skerry", version = "1" },
            capabilities = new { experimentalApi = true } }, ct).ConfigureAwait(false);
        await Write(new { method = "initialized" }, ct).ConfigureAwait(false);
        var result = await Request(2, "config/read", new { cwd, includeLayers = false }, ct).ConfigureAwait(false);
        return result.GetProperty("config");
    }

    static string[] EnabledServers(JsonElement config) => config.TryGetProperty("mcp_servers", out var servers)
        ? servers.EnumerateObject().Where(s => !s.Value.TryGetProperty("enabled", out var enabled) ||
            enabled.ValueKind != JsonValueKind.False).Select(s => s.Name).ToArray() : [];

    internal static bool OnlyProfileInstructions(JsonElement sources)
    {
        // Both exec and app-server load the signed-in CLI's global AGENTS file independently of project_doc_max_bytes.
        var profile = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(profile)) profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        if (!Path.IsPathFullyQualified(profile) || sources.GetArrayLength() > 1) return false;
        foreach (var source in sources.EnumerateArray())
        {
            if (source.ValueKind != JsonValueKind.String || source.GetString() is not { } file || !Path.IsPathFullyQualified(file))
                return false;
            var path = Path.GetFullPath(file);
            if (!new[] { "AGENTS.md", "AGENTS.override.md" }.Any(name =>
                string.Equals(path, Path.GetFullPath(Path.Combine(profile, name)), StringComparison.OrdinalIgnoreCase))) return false;
        }
        return true;
    }

    static bool EmptyInventory(JsonElement inventory, JsonElement config)
    {
        if (Value(inventory, "nextCursor") is not null) return false;
        foreach (var server in inventory.GetProperty("data").EnumerateArray())
        {
            if (Value(server, "name") is not { } name || !config.TryGetProperty("mcp_servers", out var servers) ||
                !servers.TryGetProperty(name, out var entry) || entry.GetProperty("enabled").ValueKind != JsonValueKind.False ||
                server.GetProperty("tools").EnumerateObject().Any() || server.GetProperty("resources").GetArrayLength() != 0 ||
                server.GetProperty("resourceTemplates").GetArrayLength() != 0) return false;
        }
        return true;
    }

    static bool SafeConfig(JsonElement c)
    {
        var features = c.GetProperty("features");
        var otel = c.GetProperty("otel");
        return Value(c, "chatgpt_base_url") == Backend && Value(c, "openai_base_url") == Inference &&
            Value(c.GetProperty("model_providers").GetProperty("openai"), "model_catalog_url") == Inference + "/models" &&
            Value(c, "model_provider") == "openai" && Value(c, "approval_policy") == "never" &&
            Value(c, "sandbox_mode") == "read-only" && Value(c, "web_search") == "disabled" &&
            Value(c, "forced_login_method") == "chatgpt" && c.GetProperty("notify").GetArrayLength() == 0 &&
            Value(otel, "exporter") == "none" && Value(otel, "trace_exporter") == "none" &&
            Value(otel, "metrics_exporter") == "none" &&
            new[] { "shell_tool", "unified_exec", "hooks", "apps", "plugins", "remote_plugin", "computer_use", "browser_use",
                "code_mode", "multi_agent", "multi_agent_v2" }.All(f => features.GetProperty(f).ValueKind == JsonValueKind.False);
    }

    async Task Write(object message, CancellationToken ct)
    {
        await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    async Task<JsonElement> Request(int id, string method, object parameters, CancellationToken ct)
    {
        await Write(new { id, method, @params = parameters }, ct).ConfigureAwait(false);
        while (await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            using var json = JsonDocument.Parse(line);
            var root = json.RootElement;
            if (!root.TryGetProperty("id", out var received)) continue;
            if (received.ValueKind != JsonValueKind.Number || received.GetInt32() != id ||
                root.TryGetProperty("method", out _) || !root.TryGetProperty("result", out var result)) throw new InvalidDataException();
            return result.Clone();
        }
        throw new EndOfStreamException();
    }

    internal async IAsyncEnumerable<string> Answers(string prompt, List<string> images, [EnumeratorCancellation] CancellationToken ct)
    {
        var input = new List<object> { new { type = "text", text = prompt, text_elements = Array.Empty<object>() } };
        input.AddRange(images.Select(path => new { type = "localImage", path }));
        await Write(new { id = 6, method = "turn/start", @params = new { threadId = _thread, input } }, ct).ConfigureAwait(false);
        await foreach (var text in ReadTurn(_process.StandardOutput, _thread, ct).ConfigureAwait(false)) yield return text;
    }

    internal static async IAsyncEnumerable<string> ReadTurn(TextReader reader, string thread, [EnumeratorCancellation] CancellationToken ct)
    {
        string? turn = null, itemId = null;
        var answer = "";
        var itemText = "";
        var completed = false;
        var finishedItems = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            using var json = JsonDocument.Parse(line);
            var root = json.RootElement;
            if (root.TryGetProperty("id", out var id))
            {
                if (id.ValueKind != JsonValueKind.Number || id.GetInt32() != 6 || root.TryGetProperty("method", out _) ||
                    !root.TryGetProperty("result", out var result)) throw new AiException(RequestFailed);
                turn = Value(result.GetProperty("turn"), "id");
                continue;
            }
            var method = Value(root, "method");
            if (!root.TryGetProperty("params", out var p) || Value(p, "threadId") != thread) continue;
            if (Value(p, "turnId") is { } eventTurn && turn is not null && eventTurn != turn) continue;
            var before = answer;
            switch (method)
            {
                case "item/started":
                    var started = p.GetProperty("item");
                    if (!IsAnswer(started) || Value(started, "id") is not { } startId ||
                        startId == itemId || finishedItems.Contains(startId)) break;
                    if (!CanShow(started)) { itemId = null; answer = ""; break; }
                    itemId = startId;
                    itemText = VisibleText(started);
                    if (itemText.Length != 0) answer = itemText;
                    completed = false;
                    break;
                case "item/agentMessage/delta" when !completed && itemId is not null && Value(p, "itemId") == itemId:
                    itemText += Value(p, "delta");
                    answer = itemText;
                    break;
                case "item/completed":
                    var item = p.GetProperty("item");
                    if (!IsAnswer(item) || Value(item, "id") is not { } endId ||
                        (!finishedItems.Add(endId) && endId != itemId)) break;
                    itemId = endId;
                    answer = VisibleText(item);
                    completed = true;
                    break;
                case "turn/completed":
                    var final = p.GetProperty("turn");
                    if (turn is not null && Value(final, "id") != turn) continue;
                    if (Value(final, "status") != "completed") throw new AiException(RequestFailed);
                    if (final.TryGetProperty("items", out var items))
                        foreach (var entry in items.EnumerateArray().Where(IsAnswer))
                        {
                            answer = VisibleText(entry);
                            completed = true;
                        }
                    if (answer != before) yield return answer;
                    if (!completed || string.IsNullOrWhiteSpace(answer)) throw new AiException(RequestFailed);
                    yield break;
                case "error" when !p.TryGetProperty("willRetry", out var retry) || retry.ValueKind != JsonValueKind.True:
                    throw new AiException(RequestFailed);
            }
            if (answer != before) yield return answer;
        }
        ct.ThrowIfCancellationRequested();
        throw new AiException(RequestFailed);
    }

    static bool IsAnswer(JsonElement item) => Value(item, "type") == "agentMessage" && Value(item, "phase") != "commentary";
    static string VisibleText(JsonElement item) => CanShow(item) ? Value(item, "text") ?? "" : "";

    static bool CanShow(JsonElement item)
    {
        if (item.TryGetProperty("moderation", out var moderation) && moderation.ValueKind == JsonValueKind.Object)
        {
            if (moderation.TryGetProperty("display", out var display) && display.ValueKind == JsonValueKind.Object)
            {
                if (Value(display, "mode") != "show") return false;
            }
            else if (moderation.TryGetProperty("moderated", out var flag) && flag.ValueKind == JsonValueKind.True) return false;
        }
        return true;
    }
    static string? Value(JsonElement obj, string key) => obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString() : null;

    public async ValueTask DisposeAsync()
    {
        _cancel.Dispose();
        try
        {
            ChatCli.Kill(_process);
            await _process.WaitForExitAsync().ConfigureAwait(false);
            await _stderr.ConfigureAwait(false);
        }
        finally { _process.Dispose(); }
    }
}
