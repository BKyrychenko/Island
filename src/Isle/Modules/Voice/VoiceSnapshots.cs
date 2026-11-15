using Isle.Core;
using Isle.Views.Live;
using Isle.Views.Voice;

namespace Isle.Modules.Voice;

/// <summary>--snapshot states for the hold-to-talk pills (fixed content; the microphone is not touched).</summary>
static class VoiceSnapshots
{
    public static void Register(Dictionary<string, Action<IslandWindow>> states)
    {
        states["voice-recording"] = w =>
        {
            var v = new VoicePill();
            v.Demo();
            w.Pill.Set(new PillItem { Id = "voice", Priority = PillPriority.Recording + 1, Width = 230, View = () => v });
        };
        states["voice-transcribing"] = w =>
        {
            var v = new VoicePill();
            v.SetBusy(Strings.T("voice.transcribing"));
            w.Pill.Set(new PillItem { Id = "voice", Priority = PillPriority.Recording + 1, Width = 230, View = () => v });
        };
        states["voice-download"] = w =>
        {
            var v = new IndicatorPill("Icon.Download", Strings.T("voice.dlTitle"), "37 %");
            v.SetProgress(0.37);
            w.Pill.Set(new PillItem { Id = "voice.dl", Priority = PillPriority.Download, Width = 260, View = () => v });
        };
        states["voice-nomic"] = w =>
        {
            var text = Strings.T("voice.noMic");
            w.Pill.Set(new PillItem { Id = "voice.msg", Priority = PillPriority.Transient, Width = Math.Clamp(60 + text.Length * 6.6, 200, 420), View = () => new IndicatorPill("Icon.Mic", text) });
        };
    }
}
