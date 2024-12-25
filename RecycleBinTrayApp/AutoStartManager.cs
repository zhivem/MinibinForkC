using Microsoft.Win32;
using System.Reflection;

namespace MinibinFork
{
    public static class AutoStartManager
    {
        private const string RunRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "RecycleBinTrayApp";

        /// Проверяет, добавлено ли приложение в автозапуск.
        public static bool IsAutoStartEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            if (key == null)
                return false;

            if (key.GetValue(AppName) is not string value)
                return false;

            string exePath = Assembly.GetExecutingAssembly().Location;
            return string.Equals(value, $"\"{exePath}\"", StringComparison.InvariantCultureIgnoreCase);
        }

        /// Включает автозапуск приложения.
        public static void EnableAutoStart()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true) ??
                                           Registry.CurrentUser.CreateSubKey(RunRegistryKey);
                if (key == null)
                    return;

                string exePath = Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при включении автозапуска: {ex.Message}");
            }
        }

        /// Отключает автозапуск приложения.
        public static void DisableAutoStart()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null)
                    return;

                key.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отключении автозапуска: {ex.Message}");
            }
        }
    }
}
