using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Isle.Modules.AiChat;

static class ChatCli
{
    public const int MaxPromptChars = 400_000;

    public static string? FindExecutable(string name, params string[] installedPaths)
    {
        foreach (var path in installedPaths)
            if (Path.IsPathFullyQualified(path) && File.Exists(path)) return path;
        foreach (var entry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var directory = entry.Trim().Trim('"');
            if (!Path.IsPathFullyQualified(directory)) continue;
            var path = Path.Combine(directory, name);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static ProcessStartInfo StartInfo(string exe, string workspace, string profileVariable)
    {
        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workspace, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // Start from Windows' runtime and profile locations. The parent may itself be an agent
        // with API credentials, provider URL overrides, or process wrappers in its environment.
        info.Environment.Clear();
        foreach (var key in new[] { "SystemRoot", "windir", "ComSpec", "Path", "PATHEXT", "TEMP", "TMP",
                     "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432", "ProgramData", "PROCESSOR_ARCHITECTURE",
                     "USERPROFILE", "HOME", "HOMEDRIVE", "HOMEPATH", "USERNAME", "USERDOMAIN", "APPDATA",
                     "LOCALAPPDATA", "LANG", "LC_ALL", "HTTP_PROXY", "HTTPS_PROXY", "NO_PROXY", profileVariable })
            if (Environment.GetEnvironmentVariable(key) is { Length: > 0 } value) info.Environment[key] = value;
        return info;
    }

    public static async Task<(int Code, string Text)> Run(ProcessStartInfo info, string input,
        Func<StreamReader, CancellationToken, Task<string>> read, CancellationToken ct,
        string missingKey, string errorKey, bool captureStatus = false)
    {
        using var process = new Process { StartInfo = info };
        ct.ThrowIfCancellationRequested();
        try { if (!process.Start()) throw new AiException(missingKey); }
        catch (Win32Exception) { throw new AiException(missingKey); }
        using var cancel = ct.Register(() => Kill(process));
        try
        {
            var stdout = ReadOutput();
            var status = captureStatus ? process.StandardError.ReadToEndAsync(ct) : null;
            var stderr = (Task?)status ?? process.StandardError.BaseStream.CopyToAsync(Stream.Null, ct);
            if (input.Length > 0) await process.StandardInput.WriteAsync(input.AsMemory(), ct).ConfigureAwait(false);
            process.StandardInput.Close();
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(ct)).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            var output = await stdout.ConfigureAwait(false);
            if (status is not null) output += await status.ConfigureAwait(false);
            return (process.ExitCode, output);
        }
        catch (IOException)
        {
            ct.ThrowIfCancellationRequested();
            throw new AiException(errorKey);
        }
        finally
        {
            if (Kill(process)) await process.WaitForExitAsync().ConfigureAwait(false);
        }

        async Task<string> ReadOutput()
        {
            try { return await read(process.StandardOutput, ct).ConfigureAwait(false); }
            catch
            {
                // Stop a writer immediately if its malformed stream can no longer be drained.
                Kill(process);
                throw;
            }
        }
    }

    internal static bool Kill(Process process)
    {
        try
        {
            if (process.HasExited) return false;
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
        return false;
    }
}
