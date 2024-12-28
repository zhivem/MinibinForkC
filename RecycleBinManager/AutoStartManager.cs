using Microsoft.Win32;

namespace RecycleBinManager
{
    public static class AutoStartManager
    {
        private const string AppName = "RecycleBinManager";
        private static readonly string AppPath = Application.ExecutablePath;

        public static bool IsAutoStartEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue(AppName) as string == AppPath;
            }
        }

        public static void EnableAutoStart()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                key?.SetValue(AppName, AppPath);
            }
        }

        public static void DisableAutoStart()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                key?.DeleteValue(AppName, false);
            }
        }
    }
}
