using Microsoft.Win32;
using System;
using System.Reflection;

namespace RecycleBinTrayApp
{
    public static class AutoStartManager
    {
        private const string RunRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "RecycleBinTrayApp";

        /// <summary>
        /// Проверяет, добавлено ли приложение в автозапуск.
        /// </summary>
        /// <returns>True, если добавлено, иначе false.</returns>
        public static bool IsAutoStartEnabled()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
            {
                if (key == null)
                    return false;

                // Приведение значения к string? и проверка на null
                string? value = key.GetValue(AppName) as string;
                if (value == null)
                    return false;

                string exePath = Assembly.GetExecutingAssembly().Location;
                return string.Equals(value, $"\"{exePath}\"", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Включает автозапуск приложения.
        /// </summary>
        public static void EnableAutoStart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true) ??
                                           Registry.CurrentUser.CreateSubKey(RunRegistryKey))
                {
                    if (key == null)
                        return;

                    string exePath = Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок при добавлении в автозапуск
                Console.WriteLine($"Ошибка при включении автозапуска: {ex.Message}");
            }
        }

        /// <summary>
        /// Отключает автозапуск приложения.
        /// </summary>
        public static void DisableAutoStart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null)
                        return;

                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок при удалении из автозапуска
                Console.WriteLine($"Ошибка при отключении автозапуска: {ex.Message}");
            }
        }
    }
}
