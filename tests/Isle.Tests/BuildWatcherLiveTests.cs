using System.Diagnostics;
using Isle.Modules.Projects;

namespace Isle.Tests;

public class BuildWatcherLiveTests
{
    /// <summary>A real "cmake --build"-shaped command line is not available everywhere, so a tiny cmd.exe stands in:
    /// it is relabelled as a build through a copy named after a candidate ("make.exe").</summary>
    [Fact]
    public async Task Reports_a_finished_build_with_its_exit_code_and_duration()
    {
        var dir = Directory.CreateTempSubdirectory("isle-build-");
        try
        {
            var make = Path.Combine(dir.FullName, "make.exe");
            File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), make);
            using var w = new BuildWatcher { MinDuration = TimeSpan.Zero };
            var results = new List<BuildResult>();
            w.Finished += results.Add;
            w.Poll();
            using var p = Process.Start(new ProcessStartInfo(make, "/c ping -n 2 127.0.0.1 >nul & exit /b 3") { UseShellExecute = false, CreateNoWindow = true })!;
            await Task.Delay(300);
            w.Poll();
            await p.WaitForExitAsync();
            w.Poll();
            var r = Assert.Single(results);
            Assert.Equal("make", r.Label);
            Assert.Equal(3, r.ExitCode);
            Assert.InRange(r.Duration.TotalSeconds, 0.5, 30);
        }
        finally
        {
            try { dir.Delete(recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
