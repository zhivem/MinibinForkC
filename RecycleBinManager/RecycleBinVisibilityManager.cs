using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace RecycleBinManager
{
    public static class RecycleBinVisibilityManager
    {
        private const string DesktopKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        private const string RecycleBinValue = "{645FF040-5081-101B-9F08-00AA002F954E}";

        public static bool IsRecycleBinVisibleOnDesktop()
        {
            return Convert.ToInt32(Registry.GetValue(DesktopKey, RecycleBinValue, 0)) == 0;
        }

        public static void SetRecycleBinVisibilityOnDesktop(bool isVisible)
        {
            int value = isVisible ? 0 : 1;
            Registry.SetValue(DesktopKey, RecycleBinValue, value, RegistryValueKind.DWord);
            SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
