using System.Runtime.CompilerServices;
using Skerry.Core;

namespace Skerry.Tests;

static class TestSetup
{
    /// <summary>Keeps tests away from the real %AppData%\Skerry.</summary>
    [ModuleInitializer]
    public static void Init() =>
        Paths.Root = Path.Combine(Path.GetTempPath(), "skerry-tests-root-" + Environment.ProcessId);
}
