using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MinibinFork
{
    public static class DesktopIconManager
    {
        private const string CLSID_RecycleBin = "{645FF040-5081-101B-9F08-00AA002F954E}";

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x8000000;
        private const uint SHCNF_FLUSH = 0x1000;

        public static void ToggleRecycleBinIcon(bool show)
        {
            string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
            string recycleBinKey = CLSID_RecycleBin;

            try
            {
                if (show)
                {
                    Registry.SetValue(keyPath, recycleBinKey, 0, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(keyPath, recycleBinKey, 1, RegistryValueKind.DWord);
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось изменить отображение корзины: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsRecycleBinVisible()
        {
            string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
            string recycleBinKey = CLSID_RecycleBin;

            try
            {
                object? value = Registry.GetValue(keyPath, recycleBinKey, null);
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return true;
            }
        }
    }
}
