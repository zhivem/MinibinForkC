using System.Runtime.InteropServices;
using NotifyIcon = NotifyIconEx.NotifyIcon;

namespace RecycleBinManager
{
    public static class IconPackManager
    {
        private static string _currentPack = "default";
        private static Icon? _emptyIcon;
        private static Icon? _fullIcon;

        public static void ApplyIconPack(string packName, NotifyIcon notifyIcon)
        {
            string packPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", packName);

            string emptyIconPath = Path.Combine(packPath, "recycle-empty.ico");
            string fullIconPath = Path.Combine(packPath, "recycle-full.ico");

            if (File.Exists(emptyIconPath) && File.Exists(fullIconPath))
            {
                // Загружаем иконки только если они существуют
                _emptyIcon = new Icon(emptyIconPath);
                _fullIcon = new Icon(fullIconPath);

                bool isRecycleBinEmpty = IsRecycleBinEmpty();
                notifyIcon.Icon = isRecycleBinEmpty ? _emptyIcon : _fullIcon;

                _currentPack = packName;
                SaveCurrentPack(packName);
            }
            else
            {
                notifyIcon.Icon = SystemIcons.Application;
            }
        }

        public static void UpdateIconsBasedOnState(NotifyIcon notifyIcon, bool isEmpty)
        {
            if (isEmpty && _emptyIcon != null)
            {
                notifyIcon.Icon = _emptyIcon;
            }
            else if (!isEmpty && _fullIcon != null)
            {
                notifyIcon.Icon = _fullIcon;
            }
            else
            {
                notifyIcon.Icon = SystemIcons.Application;
            }
        }

        public static ToolStripMenuItem CreateIconPackMenuItem(string packName, NotifyIcon notifyIcon)
        {
            Icon? icon = LoadIconForPack(packName);

            return new ToolStripMenuItem(packName, icon?.ToBitmap(), (_, _) =>
            {
                ApplyIconPack(packName, notifyIcon);
                bool isRecycleBinEmpty = IsRecycleBinEmpty();
                UpdateIconsBasedOnState(notifyIcon, isRecycleBinEmpty);
            });
        }

        private static Icon? LoadIconForPack(string packName)
        {
            string packPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", packName);
            string iconPath = Path.Combine(packPath, "recycle-empty.ico");

            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            return null;
        }

        private static void SaveCurrentPack(string packName)
        {
            var settings = SettingsManager.LoadSettings();
            settings.CurrentIconPack = packName;
            SettingsManager.SaveSettings(settings);
        }

        public static string LoadCurrentPack()
        {
            var settings = SettingsManager.LoadSettings();
            return settings.CurrentIconPack ?? "default";
        }

        private static bool IsRecycleBinEmpty()
        {
            SHQUERYRBINFO rbInfo = new() { cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            SHQueryRecycleBin(null, ref rbInfo);
            return rbInfo.i64NumItems == 0;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
        }
    }
}
