using System.Runtime.CompilerServices;
using Isle.Core;

namespace Isle.Tests;

static class TestSetup
{
    /// <summary>Keeps tests away from the real %AppData%\Isle.</summary>
    [ModuleInitializer]
    public static void Init() =>
        Paths.Root = Path.Combine(Path.GetTempPath(), "isle-tests-root-" + Environment.ProcessId);
}
