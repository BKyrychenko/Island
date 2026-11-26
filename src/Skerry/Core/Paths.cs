using System.IO;

namespace Skerry.Core;

/// <summary>All on-disk locations. Set SKERRY_DATA_DIR (or <see cref="Root"/> in tests) to redirect.</summary>
public static class Paths
{
    static string? _root;

    public static string Root
    {
        get
        {
            if (_root is null)
            {
                var env = Environment.GetEnvironmentVariable("SKERRY_DATA_DIR");
                _root = string.IsNullOrWhiteSpace(env) ? DefaultRoot() : env;
            }
            Directory.CreateDirectory(_root);
            return _root;
        }
        set => _root = value;
    }

    /// <summary>The data folder of the app's former name (Isle), moved to the new one on first start.</summary>
    public static string LegacyRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Isle");

    static string DefaultRoot()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Skerry");
        if (Directory.Exists(root) || !Directory.Exists(LegacyRoot)) return root;
        try
        {
            Directory.Move(LegacyRoot, root);
            return root;
        }
        // An old copy may still be running and holding files: keep using its folder and try again next start.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return LegacyRoot; }
    }

    public static string File(string name) => Path.Combine(Root, name);

    public static string Dir(string name)
    {
        var d = Path.Combine(Root, name);
        Directory.CreateDirectory(d);
        return d;
    }

    public static string SettingsFile => File("settings.json");
    public static string LogFile => File("log.txt");
}
