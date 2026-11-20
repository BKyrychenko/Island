using System.Text;
using Isle.Agents;

// Isle.Hook.exe <agent> [--timeout <seconds>] [--async]
// Registered as a Claude Code / Codex command hook. Reads the hook input JSON from stdin and forwards it to the
// island over a named pipe. For permission requests and AskUserQuestion it waits for the island's answer and prints
// the hook decision JSON. Fails open: island not running, pipe missing, bad input or timeout → exit 0, no output.
try
{
    if (args is ["--terminal-probe", ..]) return await ConsoleTerminal.Probe(args);
    string text;
    using (var stdin = Console.OpenStandardInput())
    using (var reader = new StreamReader(stdin, new UTF8Encoding(false)))
        text = reader.ReadToEnd();
    // The installer places the helper in the data directory's bin folder.
    var root = Directory.GetParent(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory))!.FullName;
    var endpoint = HookProtocol.Endpoint.Read(root);
    if (endpoint is null) return 0;
    var trustedPath = File.ReadAllText(HookProtocol.TrustedPathFile(root));
    var output = await HookProtocol.RunClientAsync(args, text, endpoint, trustedPath, terminal: ConsoleTerminal.Capture());
    if (output is not null)
    {
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(new UTF8Encoding(false).GetBytes(output + "\n"));
        stdout.Flush();
    }
}
catch (Exception)
{
    // fail open, always
}
return 0;
