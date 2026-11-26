namespace Skerry.Core;

/// <summary>A feature that can be started and stopped at runtime. Start/Stop are called on the UI thread.</summary>
public interface IModule
{
    string Id { get; }
    void Start();
    void Stop();
}

public enum ModuleState { Stopped, Running, Failed }

/// <summary>
/// Owns module lifetime: modules are registered as factories (nothing is constructed until needed),
/// started when enabled in settings, stopped when disabled, and isolated from each other's failures.
/// </summary>
public sealed class ModuleHost
{
    sealed class Entry(string id, Func<IModule> factory, bool defaultEnabled)
    {
        public readonly string Id = id;
        public readonly Func<IModule> Factory = factory;
        public readonly bool DefaultEnabled = defaultEnabled;
        public IModule? Instance;
        public ModuleState State;
    }

    readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> _order = [];
    readonly Func<string, bool, bool> _isEnabled;

    /// <summary>Raised after a module changes state.</summary>
    public event Action<string, ModuleState>? StateChanged;

    public ModuleHost(Func<string, bool, bool>? isEnabled = null) =>
        _isEnabled = isEnabled ?? ((id, def) => Settings.Current.IsModuleEnabled(id, def));

    public IReadOnlyList<string> Ids => _order;

    public void Register(string id, Func<IModule> factory, bool defaultEnabled = true)
    {
        _entries[id] = new Entry(id, factory, defaultEnabled);
        _order.Add(id);
    }

    public bool IsEnabled(string id) => _entries.TryGetValue(id, out var e) && _isEnabled(e.Id, e.DefaultEnabled);

    public ModuleState StateOf(string id) => _entries.TryGetValue(id, out var e) ? e.State : ModuleState.Stopped;

    /// <summary>The running instance, or null when the module is stopped/failed/unknown.</summary>
    public T? Get<T>(string id) where T : class, IModule =>
        _entries.TryGetValue(id, out var e) && e.State == ModuleState.Running ? e.Instance as T : null;

    /// <summary>Starts every enabled module and stops every disabled one. Call after settings change.</summary>
    public void Sync()
    {
        foreach (var id in _order)
        {
            if (IsEnabled(id)) Start(id);
            else Stop(id);
        }
    }

    public bool Start(string id)
    {
        if (!_entries.TryGetValue(id, out var e)) return false;
        if (e.State == ModuleState.Running) return true;
        try
        {
            e.Instance ??= e.Factory();
            e.Instance.Start();
            SetState(e, ModuleState.Running);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"module {id} failed to start", ex);
            try { e.Instance?.Stop(); } catch { /* already failing */ }
            e.Instance = null;
            SetState(e, ModuleState.Failed);
            return false;
        }
    }

    public void Stop(string id)
    {
        if (!_entries.TryGetValue(id, out var e) || e.Instance is null)
        {
            if (e is not null && e.State == ModuleState.Failed) SetState(e, ModuleState.Stopped);
            return;
        }
        try { e.Instance.Stop(); }
        catch (Exception ex) { Logger.Error($"module {id} failed to stop", ex); }
        (e.Instance as IDisposable)?.Dispose();
        e.Instance = null;
        SetState(e, ModuleState.Stopped);
    }

    public void StopAll()
    {
        for (var i = _order.Count - 1; i >= 0; i--) Stop(_order[i]);
    }

    void SetState(Entry e, ModuleState state)
    {
        if (e.State == state) return;
        e.State = state;
        try { StateChanged?.Invoke(e.Id, state); }
        catch (Exception ex) { Logger.Error("module state handler", ex); }
    }
}
