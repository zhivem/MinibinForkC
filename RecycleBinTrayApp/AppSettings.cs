using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinibinFork
{
    public class AppSettings
    {
        public bool HideNotifications { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public string SelectedIconPack { get; set; } = "Default"; // Новое свойство

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
                    // Обработка ошибок десериализации
                    Console.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                    return new AppSettings();
                }
            }
            else
            {
                // Если файл не существует, создаём новый экземпляр с настройками по умолчанию
                AppSettings defaultSettings = new AppSettings();
                defaultSettings.Save(); // Сохраняем настройки по умолчанию
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
                // Обработка ошибок сериализации
                Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }
    }
}
