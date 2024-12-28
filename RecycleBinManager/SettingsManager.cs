using System.Text.Json;

namespace RecycleBinManager
{
    internal static class SettingsManager
    {
        private static readonly string SettingsFilePath = "settings.json";

        public static AppSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    // Если чтение настроек не удалось, возвращаем настройки по умолчанию.
                }
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Обработка ошибок при записи настроек (логирование, уведомление и т.д.)
            }
        }
    }

    public class AppSettings
    {
        public bool ShowNotifications { get; set; } = true;
        public bool ShowRecycleBinOnDesktop { get; set; } = true;
        public bool AutoStartEnabled { get; set; } = false;
        public string CurrentIconPack { get; set; } = "default";
    }
}
