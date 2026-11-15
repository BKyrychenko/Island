using System.Text;
using Isle.Agents;

// Isle.Hook.exe <agent> [--timeout <seconds>] [--async]
// Registered as a Claude Code / Codex command hook. Reads the hook input JSON from stdin and forwards it to the
// island over a named pipe. For permission requests and AskUserQuestion it waits for the island's answer and prints
// the hook decision JSON. Fails open: island not running, pipe missing, bad input or timeout → exit 0, no output.
try
{
    string text;
    using (var stdin = Console.OpenStandardInput())
    using (var reader = new StreamReader(stdin, new UTF8Encoding(false)))
        text = reader.ReadToEnd();
    var output = await HookProtocol.RunClientAsync(args, text, HookProtocol.PipeName);
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
