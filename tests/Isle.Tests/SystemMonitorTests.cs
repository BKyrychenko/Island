using Isle.Modules.SystemMonitor;

namespace Isle.Tests;

public class SystemMonitorTests
{
    [Fact]
    public void Cpu_usage_from_system_times()
    {
        // kernel 600 (of which idle 400) + user 400 = 1000 ticks, busy 600
        Assert.Equal(60, SystemMath.CpuUsage(0, 0, 0, 400, 600, 400), 3);
        Assert.Equal(0, SystemMath.CpuUsage(5, 5, 5, 5, 5, 5));
        Assert.Equal(100, SystemMath.CpuUsage(0, 0, 0, 0, 500, 500));
    }

    [Fact]
    public void Gpu_usage_sums_engines_of_a_type_and_takes_the_busiest_type()
    {
        var engines = new[]
        {
            ("pid_1_luid_0x0_phys_0_eng_0_engtype_3D", 30.0),
            ("pid_2_luid_0x0_phys_0_eng_0_engtype_3D", 25.0),
            ("pid_2_luid_0x0_phys_0_eng_4_engtype_VideoDecode", 40.0),
            ("something_else", 99.0),
        };
        Assert.Equal(55, SystemMath.GpuUsage(engines));
        Assert.Equal(100, SystemMath.GpuUsage([("a_engtype_3D", 80.0), ("b_engtype_3D", 70.0)]));
        Assert.Equal(0, SystemMath.GpuUsage([]));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1000, "1000 B")]
    [InlineData(1536, "2 KB")]
    [InlineData(1_468_006, "1.4 MB")]
    [InlineData(17_179_869_184, "16 GB")]
    [InlineData(150L * 1024 * 1024, "150 MB")]
    public void Sizes(long bytes, string expected) => Assert.Equal(expected, SystemMath.Size(bytes));

    [Fact]
    public void Top_processes_by_cpu_share_of_the_machine()
    {
        var before = new Dictionary<int, (string, TimeSpan, long)> { [1] = ("a", TimeSpan.FromSeconds(1), 10), [2] = ("b", TimeSpan.FromSeconds(5), 99), [3] = ("old", TimeSpan.Zero, 1) };
        var after = new Dictionary<int, (string, TimeSpan, long)>
        {
            [1] = ("a", TimeSpan.FromSeconds(3), 10),    // 2 s of 4 cores × 1 s = 50%
            [2] = ("b", TimeSpan.FromSeconds(5.4), 99),  // 10%
            [3] = ("reused", TimeSpan.FromSeconds(9), 1), // pid reused: no history → 0
            [4] = ("new", TimeSpan.FromSeconds(9), 50),  // new: 0
        };
        var top = SystemMath.Top(before, after, TimeSpan.FromSeconds(1), cores: 4, count: 3);
        Assert.Equal(["a", "b", "new"], top.Select(p => p.Name));
        Assert.Equal(50, top[0].Cpu, 3);
        Assert.Equal(10, top[1].Cpu, 3);
    }

    [Fact]
    public void Sustained_alert_needs_the_hold_time_then_cools_down()
    {
        var a = new SustainedAlert(90, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(10));
        var t = DateTimeOffset.UnixEpoch;
        Assert.False(a.Update(95, t));
        Assert.False(a.Update(95, t.AddSeconds(20)));
        Assert.False(a.Update(50, t.AddSeconds(25)));     // dipped: start over
        Assert.False(a.Update(95, t.AddSeconds(30)));
        Assert.True(a.Update(95, t.AddSeconds(60)));
        Assert.False(a.Update(95, t.AddSeconds(120)));    // cooling down
        Assert.True(a.Update(95, t.AddMinutes(11)));
    }

    [Fact]
    public void Sampler_reads_plausible_values_from_this_machine()
    {
        using var s = new SystemSampler();
        s.Full();
        Thread.Sleep(300);
        var r = s.Full();
        Assert.InRange(r.Cpu, 0, 100);
        Assert.Equal(Environment.ProcessorCount, r.Cores.Length);
        Assert.True(r.MemTotal > 512L * 1024 * 1024);
        Assert.InRange(r.MemUsed, 1, r.MemTotal);
        s.TopProcesses(5);
        Thread.Sleep(200);
        var top = s.TopProcesses(5);
        Assert.Equal(5, top.Count);
    }
}
