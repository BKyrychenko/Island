using System.Reflection;

namespace Isle.Core;

/// <summary>
/// Loads the optional developer pack: exactly <c>Isle.Dev.dll</c> next to Isle.exe, only when it was built from the
/// same commit, then calls <c>Isle.Dev.Pack.Register()</c>. Any failure is logged and Isle runs without the pack.
/// </summary>
static class DevPack
{
    public static void Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Isle.Dev.dll");
            if (!File.Exists(path)) return;
            var pack = Assembly.LoadFrom(path);
            string? Version(Assembly a) => a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string? mine = Version(typeof(DevPack).Assembly), theirs = Version(pack);
            if (mine != theirs) { Logger.Warn($"developer pack skipped: built from {theirs}, Isle is {mine}"); return; }
            pack.GetType("Isle.Dev.Pack", throwOnError: true)!.GetMethod("Register")!.Invoke(null, null);
            Logger.Info("developer pack loaded");
        }
        catch (Exception ex) { Logger.Error("developer pack", ex); }
    }
}
