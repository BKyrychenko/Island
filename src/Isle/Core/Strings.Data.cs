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
        ["panel.empty"] = "Nothing here yet. Turn modules on in Settings.",
        ["panel.tabFailed"] = "This tab could not be opened. See log.txt.",
        ["tabs.more"] = "More tabs",
        ["tab.music"] = "Music",
        ["tab.timers"] = "Timers",
        ["tab.notes"] = "Notes",
        ["tab.clipboard"] = "Clipboard",
        ["tab.shelf"] = "Shelf",
        ["tab.control"] = "Control",
        ["tab.tools"] = "Tools",
        ["tab.weather"] = "Weather",
        ["tab.ai"] = "AI",
        ["tab.agents"] = "Agents",
        ["hotkey.toggle"] = "Show / hide island",
        ["common.undo"] = "Undo",
        ["snap.toast"] = "Note deleted",
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
        ["panel.empty"] = "Тут поки порожньо. Увімкніть модулі в налаштуваннях.",
        ["panel.tabFailed"] = "Не вдалося відкрити вкладку. Див. log.txt.",
        ["tabs.more"] = "Інші вкладки",
        ["tab.music"] = "Музика",
        ["tab.timers"] = "Таймери",
        ["tab.notes"] = "Нотатки",
        ["tab.clipboard"] = "Буфер",
        ["tab.shelf"] = "Полиця",
        ["tab.control"] = "Керування",
        ["tab.tools"] = "Інструменти",
        ["tab.weather"] = "Погода",
        ["tab.ai"] = "ШІ",
        ["tab.agents"] = "Агенти",
        ["hotkey.toggle"] = "Показати / сховати острівець",
        ["common.undo"] = "Скасувати",
        ["snap.toast"] = "Нотатку видалено",
    };
}
