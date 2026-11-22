using Isle.Core;
using Isle.Views.SettingsPanel;

namespace Isle.Music;

/// <summary>Entry point of the Music pack (media session + music pill). Isle's <c>Core.Packs</c> calls <see cref="Register"/> first.</summary>
public static class Pack
{
    public static void Register()
    {
        Strings.Add(
            new Dictionary<string, string> { ["mod.media"] = "Music", ["mod.media.hint"] = "Now playing in the pill, album colour", ["music.playPause"] = "Play / pause" },
            new Dictionary<string, string> { ["mod.media"] = "Музика", ["mod.media.hint"] = "Що грає — у пілюлі, колір обкладинки", ["music.playPause"] = "Відтворити / пауза" });
        App.Host.Register("media", () => new Modules.Media.MediaModule(App.Island!));
        SettingsView.AddModules("agents", "media");
        Modules.Media.MediaSnapshots.Register(IslandWindow.SnapshotStates);
    }
}
