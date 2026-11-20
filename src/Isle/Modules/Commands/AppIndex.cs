using Isle.Core;

namespace Isle.Modules.Commands;

public enum EntryKind { App, Recent, File }

public sealed record IndexEntry(string Name, string Path, EntryKind Kind);

public sealed class IndexFile
{
    public DateTimeOffset Built { get; set; }
    public string[]? Roots { get; set; }
    public List<IndexEntry> Entries { get; set; } = [];
}

/// <summary>
/// Searchable names: Start-menu shortcuts (all users + current user), recent files and the folders in option
/// "cmd.folders" (';'-separated, depth ≤ 6, max 30 000 entries). Built on a background thread, cached in index.json,
/// rebuilt in the background when older than 10 minutes and the command bar opens. Readers see an immutable snapshot.
/// </summary>
public sealed class AppIndex(string cacheFile, Func<IEnumerable<string>>? extraFolders = null)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);
    const int MaxFiles = 30_000, MaxDepth = 6;
    static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase) { "node_modules", ".git", "bin", "obj", "$RECYCLE.BIN", "AppData", ".vs", ".idea", "__pycache__" };

    volatile IReadOnlyList<IndexEntry> _entries = [];
    DateTimeOffset _built;
    string[]? _roots;
    int _building;

    public IReadOnlyList<IndexEntry> Entries => _entries;

    /// <summary>Raised (thread pool) after a background rebuild replaced <see cref="Entries"/>.</summary>
    public event Action? Updated;

    /// <summary>Loads the cache (if any) and rebuilds in the background when it is stale. Never blocks.</summary>
    public void Refresh()
    {
        var roots = NormalizeRoots(extraFolders?.Invoke() ?? []);
        if (_built == default && File.Exists(cacheFile))
        {
            var f = JsonStore.Load<IndexFile>(cacheFile);
            if (SameRoots(f.Roots, roots))
            {
                _entries = f.Entries;
                _built = f.Built;
                _roots = roots;
            }
        }
        if (!SameRoots(_roots, roots)) { _entries = []; _built = default; }
        if (DateTimeOffset.Now - _built < MaxAge) return;
        if (Interlocked.Exchange(ref _building, 1) == 1) return;
        _ = Task.Run(() =>
        {
            try
            {
                var list = Build(roots);
                if (!SameRoots(roots, NormalizeRoots(extraFolders?.Invoke() ?? []))) return;
                _roots = roots;
                _entries = list;
                _built = DateTimeOffset.Now;
                JsonStore.Save(cacheFile, new IndexFile { Built = _built, Roots = roots, Entries = list });
                Updated?.Invoke();
            }
            catch (Exception ex) { Logger.Error("app index", ex); }
            finally
            {
                Interlocked.Exchange(ref _building, 0);
                if (!SameRoots(roots, NormalizeRoots(extraFolders?.Invoke() ?? []))) Refresh();
            }
        });
    }

    internal static string[] NormalizeRoots(IEnumerable<string> roots) => roots
        .Where(r => !string.IsNullOrWhiteSpace(r))
        .Select(r => { try { return Path.TrimEndingDirectorySeparator(Path.GetFullPath(r.Trim())); } catch { return null; } })
        .OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    internal static bool SameRoots(string[]? left, string[] right) =>
        left is not null && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);

    public static List<IndexEntry> Build(IEnumerable<string> folders)
    {
        var list = new List<IndexEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // the Start menu's own app list (desktop + Store apps such as Notepad, Calculator, Terminal on Windows 11)
        foreach (var (name, id) in ShellApps())
        {
            if (IsJunk(name) || !seen.Add("app:" + name)) continue;
            list.Add(new IndexEntry(name, @"shell:AppsFolder\" + id, EntryKind.App));
        }
        foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) })
            Walk(root, 0, f =>
            {
                var ext = Path.GetExtension(f);
                if (!ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".url", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".appref-ms", StringComparison.OrdinalIgnoreCase)) return;
                var name = Path.GetFileNameWithoutExtension(f);
                if (IsJunk(name) || !seen.Add("app:" + name)) return;
                list.Add(new IndexEntry(name, f, EntryKind.App));
            }, int.MaxValue);

        var recent = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        try
        {
            if (Directory.Exists(recent))
                foreach (var f in new DirectoryInfo(recent).EnumerateFiles("*.lnk").OrderByDescending(f => f.LastWriteTimeUtc).Take(150))
                {
                    var name = Path.GetFileNameWithoutExtension(f.Name);
                    if (seen.Add("recent:" + name)) list.Add(new IndexEntry(name, f.FullName, EntryKind.Recent));
                }
        }
        catch (Exception ex) { Logger.Warn($"recent files: {ex.GetType().Name}"); }

        var budget = MaxFiles;
        foreach (var folder in folders.Where(Directory.Exists))
            Walk(folder, 0, f =>
            {
                if (budget-- <= 0) return;
                list.Add(new IndexEntry(Path.GetFileName(f), f, EntryKind.File));
            }, MaxDepth, includeDirs: true, budget: () => budget > 0);
        return list;
    }

    /// <summary>shell:AppsFolder items (name, parsing name) via Shell.Application on a short-lived STA thread.</summary>
    static List<(string Name, string Id)> ShellApps()
    {
        var result = new List<(string, string)>();
        var t = new Thread(() =>
        {
            try
            {
                var type = Type.GetTypeFromProgID("Shell.Application");
                if (type is null) return;
                dynamic shell = Activator.CreateInstance(type)!;
                dynamic folder = shell.NameSpace("shell:AppsFolder");
                if (folder is null) return;
                foreach (dynamic item in folder.Items())
                {
                    string name = item.Name;
                    string path = item.Path;
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(path) && !path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        result.Add((name, path));
                }
            }
            catch (Exception ex) { Logger.Warn($"apps folder: {ex.GetType().Name}"); }
        }) { IsBackground = true, Name = "AppsFolder" };
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join(TimeSpan.FromSeconds(20));
        return result;
    }

    static bool IsJunk(string name) =>
        name.Contains("uninstall", StringComparison.OrdinalIgnoreCase) || name.Contains("деінсталювати", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Desktop", StringComparison.OrdinalIgnoreCase) || name.Contains("readme", StringComparison.OrdinalIgnoreCase);

    static void Walk(string dir, int depth, Action<string> file, int maxDepth, bool includeDirs = false, Func<bool>? budget = null)
    {
        if (depth > maxDepth || string.IsNullOrEmpty(dir) || budget?.Invoke() == false) return;
        try
        {
            var di = new DirectoryInfo(dir);
            foreach (var f in di.EnumerateFiles())
            {
                if ((f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;
                file(f.FullName);
            }
            foreach (var d in di.EnumerateDirectories())
            {
                if ((d.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0 || SkipDirs.Contains(d.Name)) continue;
                if (includeDirs) file(d.FullName);
                Walk(d.FullName, depth + 1, file, maxDepth, includeDirs, budget);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }
}
