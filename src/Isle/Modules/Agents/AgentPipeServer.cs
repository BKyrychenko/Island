using System.IO.Pipes;
using Isle.Agents;
using Isle.Core;

namespace Isle.Modules.Agents;

/// <summary>
/// Named-pipe server for Isle.Hook.exe (current user only, any number of concurrent hook calls). One request line per
/// connection; for waiting calls the handler's reply is written back. The handler's token is cancelled when the hook
/// disconnects (its own timeout, Ctrl+C in the terminal) or after the hook's timeout + 5 s.
/// </summary>
public sealed class AgentPipeServer(string pipeName, Func<HookMessage, CancellationToken, Task<string?>> handle) : IDisposable
{
    CancellationTokenSource? _cts;

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        // The first instance is created here, so the pipe exists as soon as Start returns (a hook arriving right
        // after start-up must not see "no island").
        NamedPipeServerStream? first = null;
        try { first = Create(); }
        catch (Exception ex) { Logger.Warn($"agent pipe: {ex.GetType().Name}"); }
        _ = Task.Run(() => AcceptLoop(first, ct));
    }

    NamedPipeServerStream Create() =>
        new(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    async Task AcceptLoop(NamedPipeServerStream? next, CancellationToken ct)
    {
        var failures = 0;
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? server = next;
            next = null;
            try
            {
                server ??= Create();
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                failures = 0;
                var s = server;
                server = null;
                _ = Task.Run(() => Serve(s, ct), CancellationToken.None);
            }
            catch (OperationCanceledException) { server?.Dispose(); break; }
            catch (Exception ex)
            {
                server?.Dispose();
                Logger.Warn($"agent pipe: {ex.GetType().Name}");
                if (++failures > 5) { Logger.Warn("agent pipe stopped after repeated failures"); break; }
                try { await Task.Delay(1000 * failures, ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }
    }

    async Task Serve(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                string? line;
                using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    readCts.CancelAfter(TimeSpan.FromSeconds(5));
                    line = await HookProtocol.ReadLineAsync(pipe, readCts.Token).ConfigureAwait(false);
                }
                var msg = HookMessage.Parse(line);
                if (msg is null) return;
                if (!msg.Wait)
                {
                    await handle(msg, ct).ConfigureAwait(false);
                    return;
                }
                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                waitCts.CancelAfter(TimeSpan.FromSeconds(msg.TimeoutSeconds + 5));
                _ = WatchDisconnect(pipe, waitCts);
                var reply = await handle(msg, waitCts.Token).ConfigureAwait(false) ?? HookProtocol.Reply("none");
                if (pipe.IsConnected && !ct.IsCancellationRequested)
                    await HookProtocol.WriteLineAsync(pipe, reply, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (IOException) { } // hook went away
            catch (Exception ex) { Logger.Error("agent pipe serve", ex); }
        }
    }

    /// <summary>The hook sends nothing after its request; a completed read means it closed the pipe.</summary>
    static async Task WatchDisconnect(NamedPipeServerStream pipe, CancellationTokenSource cts)
    {
        try
        {
            var b = new byte[1];
            await pipe.ReadAsync(b, cts.Token).ConfigureAwait(false);
        }
        catch (Exception) { }
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
