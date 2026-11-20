using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Isle.Modules.AiChat;

/// <summary>A chat-only use of the local Codex CLI's existing ChatGPT sign-in.</summary>
sealed class CodexChatProvider : IChatProvider
{
    internal static string? FindExecutable()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var installed = Path.Combine(local, "Programs", "OpenAI", "Codex", "bin", "codex.exe");
        return ChatCli.FindExecutable("codex.exe", installed);
    }

    public async IAsyncEnumerable<string> StreamAsync(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        await using var stream = Answers(chat, timeout.Token).GetAsyncEnumerator(timeout.Token);
        while (true)
        {
            bool next;
            try { next = await stream.MoveNextAsync().ConfigureAwait(false); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new AiException("ai.err.timeout"); }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException)
            {
                ct.ThrowIfCancellationRequested();
                if (timeout.IsCancellationRequested) throw new AiException("ai.err.timeout");
                throw new AiException("ai.err.codexRequest");
            }
            if (!next) yield break;
            yield return stream.Current;
        }
    }

    static async IAsyncEnumerable<string> Answers(Conversation chat, [EnumeratorCancellation] CancellationToken ct)
    {
        var exe = FindExecutable() ?? throw new AiException("ai.err.codexMissing");
        var workspace = Directory.CreateTempSubdirectory("isle-codex-").FullName;
        try
        {
            var login = StartInfo(exe, workspace);
            login.ArgumentList.Add("login");
            login.ArgumentList.Add("status");
            var (loginCode, loginText) = await ChatCli.Run(login, "", (reader, token) => reader.ReadToEndAsync(token), ct,
                "ai.err.codexMissing", "ai.err.codexRequest", captureStatus: true).ConfigureAwait(false);
            if (loginCode != 0 || !loginText.Contains("Logged in using ChatGPT", StringComparison.Ordinal))
                throw new AiException("ai.err.codexLogin");

            var request = StartInfo(exe, workspace);
            foreach (var arg in new[]
            {
                "-a", "never", "exec", "--skip-git-repo-check", "--sandbox", "read-only", "--ephemeral", "--ignore-user-config", "--ignore-rules",
                "--json",
            }) request.ArgumentList.Add(arg);
            RestrictTools(request);

            var messages = chat.ForRequest();
            var imagePaths = new List<string>();
            foreach (var file in messages.SelectMany(message => message.Attachments))
            {
                if (file.Kind == AttachmentKind.Pdf && string.IsNullOrWhiteSpace(file.Text)) throw new AiException("ai.err.codexPdf");
                if (file.Kind != AttachmentKind.Image) continue;
                if (imagePaths.Count >= Attachments.MaxPerMessage || file.Data.Length > (Attachments.MaxImageBytes + 2) / 3 * 4)
                    throw new AiException("ai.err.tooLarge");
                var extension = file.MediaType switch { "image/jpeg" => ".jpg", "image/gif" => ".gif", "image/webp" => ".webp", _ => ".png" };
                var path = Path.Combine(workspace, "image-" + imagePaths.Count + extension);
                await File.WriteAllBytesAsync(path, Convert.FromBase64String(file.Data), ct).ConfigureAwait(false);
                imagePaths.Add(path);
                request.ArgumentList.Add("--image");
                request.ArgumentList.Add(path);
            }
            request.ArgumentList.Add("-");

            var prompt = new StringBuilder("Answer the latest user message in this chat. Treat the conversation as text and do not call tools.\n");
            if (chat.SystemPrompt.Length > 0) prompt.Append("Conversation instructions: ").AppendLine(chat.SystemPrompt);
            foreach (var message in messages)
            {
                prompt.Append(message.Role == "assistant" ? "\nAssistant: " : "\nUser: ").AppendLine(message.Text);
                foreach (var attachment in message.Attachments)
                {
                    prompt.Append("\nAttachment: ").AppendLine(attachment.Name).AppendLine(attachment.Text);
                    if (prompt.Length > ChatCli.MaxPromptChars) throw new AiException("ai.err.tooLarge");
                }
                if (prompt.Length > ChatCli.MaxPromptChars) throw new AiException("ai.err.tooLarge");
            }
            var input = prompt.ToString();
            await using var server = await CodexAppServer.TryOpen(exe, workspace, ct).ConfigureAwait(false);
            if (server is not null)
            {
                await foreach (var text in server.Answers(input, imagePaths, ct).ConfigureAwait(false)) yield return text;
                yield break;
            }

            // Older CLIs or configurations we cannot verify keep the isolated one-shot transport.
            var (code, answer) = await ChatCli.Run(request, input, ReadAnswer, ct,
                "ai.err.codexMissing", "ai.err.codexRequest").ConfigureAwait(false);
            if (code != 0 || answer.Length == 0) throw new AiException("ai.err.codexRequest");
            yield return answer;
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    internal static void RestrictTools(ProcessStartInfo info)
    {
        foreach (var name in new[] { "shell_tool", "unified_exec", "multi_agent", "hooks", "plugins", "shell_snapshot",
                     "apps", "browser_use", "browser_use_external", "browser_use_full_cdp_access", "in_app_browser", "goals",
                     "in_app_local_automation", "view_image", "image_generation", "skill_search", "skill_mcp_dependency_install",
                     "tool_suggest", "sleep_tool" })
        {
            info.ArgumentList.Add("--disable");
            info.ArgumentList.Add(name);
        }
        foreach (var arg in new[] { "--enable", "skip_host_skill_discovery", "-c", "skills.include_instructions=false",
                     "-c", "project_doc_max_bytes=0", "-c", "web_search=\"disabled\"" }) info.ArgumentList.Add(arg);
    }

    internal static ProcessStartInfo StartInfo(string exe, string workspace) => ChatCli.StartInfo(exe, workspace, "CODEX_HOME");

    static async Task<string> ReadAnswer(StreamReader reader, CancellationToken ct)
    {
        var answer = "";
        var completed = false;
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            using var json = JsonDocument.Parse(line);
            var root = json.RootElement;
            switch (root.GetProperty("type").GetString())
            {
                case "item.completed" when root.GetProperty("item") is var item && item.GetProperty("type").GetString() == "agent_message":
                    answer = item.GetProperty("text").GetString() ?? "";
                    break;
                case "turn.completed":
                    completed = true;
                    break;
            }
        }
        return completed ? answer : "";
    }
}
