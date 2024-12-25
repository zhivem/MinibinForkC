using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinibinFork
{
    public class AppSettings
    {
        public bool HideNotifications { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public string SelectedIconPack { get; set; } = "Default";

        [JsonIgnore]
        private static string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        // Загрузка настроек из файла
        public static AppSettings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                    return new AppSettings();
                }
            }
            else
            {
                // Если файл не существует, создаём новый экземпляр с настройками по умолчанию
                AppSettings defaultSettings = new AppSettings();
                defaultSettings.Save();
                return defaultSettings;
            }
        }

        // Сохранение настроек в файл
        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }
    }
}
