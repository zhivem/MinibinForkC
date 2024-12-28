using System.Text.Json;

namespace RecycleBinManager
{
    internal static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RecycleBinManager",
            "settings.json"
        );

        public static AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    // Создаем файл с настройками по умолчанию, если его нет
                    SaveSettings(new AppSettings());
                }

                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return new AppSettings(); // Настройки по умолчанию
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
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
