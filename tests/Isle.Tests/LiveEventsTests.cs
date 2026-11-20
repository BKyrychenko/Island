using System.Windows;
using System.Windows.Threading;
using Isle.Core;
using Isle.Modules.LiveEvents;

namespace Isle.Tests;

/// <summary>Records what a source shows; runs on a private STA dispatcher.</summary>
sealed class FakeSink(Dispatcher ui) : ILiveSink
{
    public readonly Dictionary<string, PillItem> Items = [];
    public readonly List<string> Flashes = [];
    public Dispatcher Ui => ui;
    public void Show(PillItem item) => Items[item.Id] = item;
    public void Hide(string id) => Items.Remove(id);
    public void Flash(string id, Func<FrameworkElement> view, double width, TimeSpan duration, double height = 32) => Flashes.Add(id);

    /// <summary>Runs <paramref name="body"/> on a new STA thread with a dispatcher (WPF elements need one).</summary>
    public static void Run(Action<FakeSink> body)
    {
        Exception? error = null;
        var t = new Thread(() =>
        {
            try { body(new FakeSink(Dispatcher.CurrentDispatcher)); }
            catch (Exception ex) { error = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (error is not null) throw new Xunit.Sdk.XunitException(error.ToString());
    }

    /// <summary>Pumps the dispatcher until <paramref name="done"/> or the timeout.</summary>
    public bool PumpUntil(Func<bool> done, int timeoutMs = 5000)
    {
        var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!done() && DateTime.UtcNow < end)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => frame.Continue = false, ui);
            Dispatcher.PushFrame(frame);
            timer.Stop();
        }
        return done();
    }
}

public class DownloadDetectorTests
{
    [Theory]
    [InlineData("setup.exe.crdownload", true)]
    [InlineData("Unconfirmed 123456.crdownload", true)]
    [InlineData("movie.mkv.part", true)]
    [InlineData("doc.pdf.partial", true)]
    [InlineData("pic.png.download", true)]
    [InlineData("SETUP.EXE.CRDOWNLOAD", true)]
    [InlineData(".crdownload", false)]
    [InlineData(".hidden.part", false)]
    [InlineData("setup.exe", false)]
    [InlineData("report.part1.rar", false)]
    [InlineData("notes.txt", false)]
    public void Detects_partial_files(string name, bool partial) =>
        Assert.Equal(partial, DownloadDetector.IsPartial(Path.Combine(@"C:\Users\u\Downloads", name)));

    [Theory]
    [InlineData("setup.exe.crdownload", "setup.exe")]
    [InlineData("Unconfirmed 123456.crdownload", null)]
    [InlineData("movie.mkv.part", "movie.mkv")]
    [InlineData("Xy7Ab3kd.part", null)]
    [InlineData("doc.pdf.partial", "doc.pdf")]
    public void Final_names(string name, string? expected) => Assert.Equal(expected, DownloadDetector.FinalName(name));

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2 KB")]
    [InlineData(5 * 1024 * 1024 + 512 * 1024, "5.5 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3.00 GB")]
    public void Formats_sizes(long bytes, string text) =>
        Assert.Equal(text, DownloadDetector.FormatSize(bytes).Replace(',', '.'));

    [Fact]
    public void Speed_meter_over_window()
    {
        var m = new SpeedMeter(TimeSpan.FromSeconds(4));
        var t0 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(0, m.BytesPerSecond);
        m.Add(t0, 0);
        m.Add(t0.AddSeconds(1), 1000);
        Assert.Equal(1000, m.BytesPerSecond, 3);
        m.Add(t0.AddSeconds(2), 3000);
        Assert.Equal(1500, m.BytesPerSecond, 3);
        // old samples fall out of the window
        m.Add(t0.AddSeconds(10), 3000);
        Assert.Equal(0, m.BytesPerSecond, 3);
    }

    [Fact]
    public void Downloads_source_follows_a_growing_partial_file_then_flashes_when_renamed()
    {
        var dir = Path.Combine(Path.GetTempPath(), "isle-dl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            FakeSink.Run(sink =>
            {
                using var src = new DownloadsSource(sink, dir);
                src.Start();
                var partial = Path.Combine(dir, "big.zip.crdownload");
                File.WriteAllBytes(partial, new byte[1000]);
                Assert.True(sink.PumpUntil(() => sink.Items.ContainsKey("download")), "pill shown while downloading");
                Assert.Equal(PillPriority.Download, sink.Items["download"].Priority);
                File.Move(partial, Path.Combine(dir, "big.zip"));
                Assert.True(sink.PumpUntil(() => !sink.Items.ContainsKey("download")), "pill hidden when finished");
                Assert.Contains("download.done", sink.Flashes);
            });
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Downloads_source_ignores_old_leftover_partial_files()
    {
        var dir = Path.Combine(Path.GetTempPath(), "isle-dl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "stale.iso.crdownload"), new byte[10]);
        try
        {
            FakeSink.Run(sink =>
            {
                using var src = new DownloadsSource(sink, dir);
                src.Start();
                Assert.False(src.Sampling);
                Assert.False(sink.PumpUntil(() => sink.Items.ContainsKey("download"), 1500));
                var partial = Path.Combine(dir, "stale.iso.crdownload");
                File.AppendAllText(partial, "growth");
                Assert.True(sink.PumpUntil(() => src.Sampling && sink.Items.ContainsKey("download")));
                src.Sample(DateTimeOffset.Now.AddMinutes(2));
                Assert.False(src.Sampling);
                src.Dispose();
                Assert.False(src.Sampling);
            });
        }
        finally { Directory.Delete(dir, true); }
    }
}

public class SteamParserTests
{
    [Fact]
    public void ActiveManifestDeletionAndRenameRemovePill()
    {
        using var dir = new TempDir();
        FakeSink.Run(sink =>
        {
            using var source = new SteamSource(sink);
            var file = Path.Combine(dir.Path, "appmanifest_1.acf");
            File.WriteAllText(file, Manifest(1026, 250, 1000));
            source.WatchLibrary(dir.Path);
            Assert.True(sink.Items.ContainsKey("steam"));
            File.Move(file, file + ".old");
            Assert.True(sink.PumpUntil(() => !sink.Items.ContainsKey("steam")));
            File.WriteAllText(file, Manifest(1026, 250, 1000));
            Assert.True(sink.PumpUntil(() => sink.Items.ContainsKey("steam")));
            File.Delete(file);
            Assert.True(sink.PumpUntil(() => !sink.Items.ContainsKey("steam")));
        });
    }

    const string LibraryFoldersNew = """
        "libraryfolders"
        {
            "0"
            {
                "path"		"C:\\Program Files (x86)\\Steam"
                "label"		""
                "apps" { "228980" "123" }
            }
            "1"
            {
                "path"		"D:\\SteamLibrary"
            }
        }
        """;

    const string LibraryFoldersOld = """
        "LibraryFolders"
        {
            "TimeNextStatsReport"		"1600000000"
            "ContentStatsID"		"-123"
            "1"		"E:\\Games\\Steam"
        }
        """;

    static string Manifest(long flags, long done, long total, string name = "Half-Life 3") => $$"""
        "AppState"
        {
            "appid"		"999"
            "Universe"		"1"
            "name"		"{{name}}"
            "StateFlags"		"{{flags}}"
            "BytesToDownload"		"{{total}}"
            "BytesDownloaded"		"{{done}}"
            "UserConfig" { "language" "english" }
        }
        """;

    [Fact]
    public void Libraries_new_format()
    {
        var libs = Steam.Libraries(@"C:\Program Files (x86)\Steam", LibraryFoldersNew);
        Assert.Equal([@"C:\Program Files (x86)\Steam\steamapps", @"D:\SteamLibrary\steamapps"], libs);
    }

    [Fact]
    public void Libraries_old_format_and_missing_file()
    {
        Assert.Equal([@"C:\Steam\steamapps", @"E:\Games\Steam\steamapps"], Steam.Libraries(@"C:\Steam", LibraryFoldersOld));
        Assert.Equal([@"C:\Steam\steamapps"], Steam.Libraries(@"C:\Steam", null));
    }

    [Fact]
    public void Downloading_manifest()
    {
        var dl = Steam.FromManifest(Manifest(1026, 250, 1000));
        Assert.NotNull(dl);
        Assert.Equal("Half-Life 3", dl.Name);
        Assert.Equal("999", dl.AppId);
        Assert.Equal(0.25, dl.Progress, 3);
    }

    [Theory]
    [InlineData(4, 0, 0)]          // fully installed
    [InlineData(4, 1000, 1000)]    // installed, counters left over
    [InlineData(1026, 1000, 1000)] // finished
    [InlineData(6, 10, 1000)]      // update required but not running (queued) — not shown
    public void Not_downloading(long flags, long done, long total) => Assert.Null(Steam.FromManifest(Manifest(flags, done, total)));

    [Fact]
    public void Vdf_escapes_comments_and_garbage()
    {
        var root = Vdf.Parse("// comment\n\"A\" { \"q\" \"say \\\"hi\\\"\" \"path\" \"C:\\\\x\" }");
        Assert.Equal("say \"hi\"", root.Child("a")!.Str("Q"));
        Assert.Equal(@"C:\x", root.Child("A")!.Str("path"));
        Assert.Null(Steam.FromManifest("not a manifest {{{"));
        Assert.Null(Steam.FromManifest(""));
    }
}

public class LiveMiscTests
{
    [Theory]
    [InlineData(100, 0, true)]
    [InlineData(200, 100, true)]
    [InlineData(100, 200, false)]
    [InlineData(0, 0, false)]
    public void Consent_in_use(long start, long stop, bool inUse) => Assert.Equal(inUse, ConsentStore.InUse(start, stop));

    [Theory]
    [InlineData("C:#Program Files#Zoom#bin#Zoom.exe", "Zoom")]
    [InlineData("C:#Users#u#AppData#Local#Discord#app-1.0#Discord.exe", "Discord")]
    [InlineData("Microsoft.WindowsCamera_8wekyb3d8bbwe", "Camera")]
    [InlineData("Microsoft.SkypeApp_kzf8qxf38zg5c", "SkypeApp")]
    public void Consent_app_names(string key, string name) => Assert.Equal(name, ConsentStore.AppName(key));

    [Fact]
    public void Obs_auth_matches_protocol_example()
    {
        // obs-websocket v5 protocol: secret = b64(sha256(password + salt)); auth = b64(sha256(secret + challenge))
        var secret = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData("supersecretpasswordlSbt3Hj4Ld38oaz7n2zqU7pUrbrjHnuoIBtbm40TEoc="u8.ToArray()));
        var expected = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret + "+IxH4CnCiqpX1rM9scsNynZzbOe4KhDeYcTNS3PDaeY=")));
        Assert.Equal(expected, ObsAuth.Response("supersecretpassword", "lSbt3Hj4Ld38oaz7n2zqU7pUrbrjHnuoIBtbm40TEoc=", "+IxH4CnCiqpX1rM9scsNynZzbOe4KhDeYcTNS3PDaeY="));
    }

    [Theory]
    [InlineData(0x04090409L, "EN")]
    [InlineData(0x04220422L, "UK")]
    [InlineData(0xF0A80422L, "UK")]
    [InlineData(0x04070407L, "DE")]
    public void Keyboard_layout_codes(long hkl, string code) => Assert.Equal(code, KeyboardLayoutSource.LayoutCode(hkl));

    [Fact]
    public void Secret_roundtrip()
    {
        var stored = Secret.Protect("hunter2");
        Assert.StartsWith("dpapi:", stored);
        Assert.DoesNotContain("hunter2", stored);
        Assert.Equal("hunter2", Secret.Unprotect(stored));
        Assert.Equal("plain", Secret.Unprotect("plain"));
        Assert.Equal("", Secret.Unprotect("dpapi:not-base64!"));
        Assert.Equal("", Secret.Protect(""));
    }

    [Fact]
    public void Live_priorities_follow_the_spec_order()
    {
        Assert.True(PillPriority.Finished > PillPriority.AgentWaiting);
        Assert.True(PillPriority.AgentWaiting > PillPriority.Recording);
        Assert.True(PillPriority.Recording > PillPriority.MicCamera);
        Assert.True(PillPriority.MicCamera > PillPriority.TimerRunning);
        Assert.True(PillPriority.TimerRunning > PillPriority.Download);
        Assert.True(PillPriority.Download > PillPriority.Music);
    }
}

public class ObsRetryTests
{
    [Theory]
    [InlineData(0, 60)]
    [InlineData(1, 120)]
    [InlineData(2, 240)]
    [InlineData(3, 300)]
    [InlineData(20, 300)]
    public void RetryBackoffIsBounded(int failures, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), ObsSource.RetryDelay(failures));
}
