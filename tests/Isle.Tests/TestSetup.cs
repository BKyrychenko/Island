using System.Runtime.CompilerServices;
using Isle.Core;

namespace Isle.Tests;

static class TestSetup
{
    /// <summary>Keeps tests away from the real %AppData%\Isle and registers every pack, as a full build would.</summary>
    [ModuleInitializer]
    public static void Init()
    {
        Paths.Root = Path.Combine(Path.GetTempPath(), "isle-tests-root-" + Environment.ProcessId);
        Isle.Music.Pack.Register(); // the loader's order
        Isle.Home.Pack.Register();
        Isle.Dev.Pack.Register();
    }
}
