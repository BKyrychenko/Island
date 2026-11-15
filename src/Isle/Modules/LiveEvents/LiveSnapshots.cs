using Isle.Core;
using Isle.Views.Live;

namespace Isle.Modules.LiveEvents;

/// <summary>--snapshot states for the live-event pills (fixed content; nothing on the machine is touched).</summary>
static class LiveSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["live-download"] = w =>
        {
            var v = new IndicatorPill("Icon.Download", "ubuntu-26.04.iso", "1.2 GB · 18.4 MB/s");
            w.Pill.Set(new PillItem { Id = "download", Priority = PillPriority.Download, View = () => v, Width = 290 });
        };
        states["live-steam"] = w =>
        {
            var v = new IndicatorPill("Icon.Download", "Starfall Tactics", "42% · 31.0 MB/s");
            v.SetProgress(0.42);
            w.Pill.Set(new PillItem { Id = "steam", Priority = PillPriority.Download, View = () => v, Width = 290 });
        };
        states["live-miccam"] = w => w.Pill.Set(new PillItem
        {
            Id = "miccam", Priority = PillPriority.MicCamera, Width = 190,
            View = () => new IndicatorPill("Icon.Camera", "Teams +1", "", "OkBrush"),
        });
        states["live-rec-shared"] = w =>
        {
            w.Pill.Set(new PillItem
            {
                Id = "miccam", Priority = PillPriority.MicCamera, Width = 190, View = () => new IndicatorPill("Icon.Mic", "Discord", "", "WarnBrush"),
                Compact = () => IndicatorPill.Dots(true, true), CompactWidth = 34,
            });
            w.Pill.Set(new PillItem { Id = "obs", Priority = PillPriority.Recording, Width = 150, View = () => new IndicatorPill(null, "REC", "12:07", "DangerBrush") });
        };
        states["live-layout"] = w => w.Pill.Set(new PillItem { Id = "l", Priority = PillPriority.Transient, Width = 104, View = () => new IndicatorPill("Icon.Keyboard", "UK", "", center: true) });
        states["live-caps"] = w => w.Pill.Set(new PillItem { Id = "c", Priority = PillPriority.Transient, Width = 170, View = () => new IndicatorPill("Icon.CapsLock", Strings.T("live.capsOn"), "", "AccentBrush", center: true) });
        states["live-vpn"] = w => w.Pill.Set(new PillItem { Id = "v", Priority = PillPriority.Transient, Width = 260, View = () => new IndicatorPill("Icon.Shield", "Tailscale", Strings.T("live.vpnOn"), "OkBrush") });
    }
}
