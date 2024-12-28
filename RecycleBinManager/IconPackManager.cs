using System.Runtime.InteropServices;
using NotifyIcon = NotifyIconEx.NotifyIcon;

namespace RecycleBinManager
{
    public static class IconPackManager
    {
        public static void ApplyIconPack(string packName, NotifyIcon notifyIcon)
        {
            string packPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", packName);

            string emptyIconPath = Path.Combine(packPath, "recycle-empty.ico");
            string fullIconPath = Path.Combine(packPath, "recycle-full.ico");

            if (File.Exists(emptyIconPath) && File.Exists(fullIconPath))
            {
                notifyIcon.Icon = new Icon(IsRecycleBinEmpty() ? emptyIconPath : fullIconPath);
                SaveCurrentPack(packName);
            }
        }

        public static ToolStripMenuItem CreateIconPackMenuItem(string packName, NotifyIcon notifyIcon)
        {
            var icon = LoadIconForPack(packName);

            return new ToolStripMenuItem(packName, icon?.ToBitmap(), (_, _) => ApplyIconPack(packName, notifyIcon));
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
            return settings.CurrentIconPack ?? "Iconpack1";
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
