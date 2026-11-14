namespace Isle.Core;

public sealed partial class Strings
{
    static readonly Dictionary<string, string> En = new()
    {
        ["app.name"] = "Isle",
        ["app.alreadyRunning"] = "Isle is already running.",
        ["tray.show"] = "Show island",
        ["tray.hide"] = "Hide island",
        ["tray.settings"] = "Settings",
        ["tray.autostart"] = "Start with Windows",
        ["tray.quit"] = "Quit",
    };

    static readonly Dictionary<string, string> Uk = new()
    {
        ["app.name"] = "Isle",
        ["app.alreadyRunning"] = "Isle вже запущено.",
        ["tray.show"] = "Показати острівець",
        ["tray.hide"] = "Сховати острівець",
        ["tray.settings"] = "Налаштування",
        ["tray.autostart"] = "Запускати з Windows",
        ["tray.quit"] = "Вийти",
    };
}
