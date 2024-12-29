using System.Diagnostics;
using System.Runtime.InteropServices;
using NotifyIcon = NotifyIconEx.NotifyIcon;

namespace RecycleBinManager
{
    internal static class Program
    {
        private static NotifyIcon _notifyIcon = new();
        private static bool _showNotifications = true;
        private static bool _showRecycleBinOnDesktop = RecycleBinVisibilityManager.IsRecycleBinVisibleOnDesktop();
        private static bool _previousRecycleBinState = true;

        [STAThread]
        public static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Загрузка настроек
            var settings = SettingsManager.LoadSettings();
            _showNotifications = settings.ShowNotifications;
            _showRecycleBinOnDesktop = settings.ShowRecycleBinOnDesktop;

            // Устанавливаем NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Text = "Менеджер Корзины",
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
                Visible = true
            };

            // Добавляем обработчик события двойного клика
            _notifyIcon.MouseDoubleClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) 
                {
                    OpenRecycleBin();
                }
            };

            // Инициализация переменных меню
            ToolStripMenuItem? showNotificationsMenu = null;
            ToolStripMenuItem? autoStartMenu = null;
            ToolStripMenuItem? showRecycleBinOnDesktopMenu = null;

            // Меню для уведомлений
            showNotificationsMenu = new ToolStripMenuItem("Показывать уведомления", null, (_, _) =>
            {
                _showNotifications = !_showNotifications;
                settings.ShowNotifications = _showNotifications;
                SettingsManager.SaveSettings(settings);

                if (showNotificationsMenu != null)
                {
                    showNotificationsMenu.Checked = _showNotifications;
                }

                ShowBalloonNotification(
                    "Уведомления",
                    _showNotifications ? "Уведомления включены." : "Уведомления отключены.",
                    ToolTipIcon.Info
                );
            })
            {
                Checked = _showNotifications
            };

            // Меню автозапуска
            autoStartMenu = new ToolStripMenuItem("Автозапуск", null, (_, _) =>
            {
                bool wasAutoStartEnabled = AutoStartManager.IsAutoStartEnabled();

                if (wasAutoStartEnabled)
                {
                    AutoStartManager.DisableAutoStart();
                    settings.AutoStartEnabled = false;
                    ShowBalloonNotification("Автозапуск", "Автозапуск отключен.", ToolTipIcon.Info);
                }
                else
                {
                    AutoStartManager.EnableAutoStart();
                    settings.AutoStartEnabled = true;
                    ShowBalloonNotification("Автозапуск", "Автозапуск включен.", ToolTipIcon.Info);
                }

                if (autoStartMenu != null)
                {
                    autoStartMenu.Checked = AutoStartManager.IsAutoStartEnabled();
                }
            })
            {
                Checked = AutoStartManager.IsAutoStartEnabled()
            };

            // Меню отображения корзины на рабочем столе
            showRecycleBinOnDesktopMenu = new ToolStripMenuItem("Отображать 🗑️ на рабочем столе", null, (_, _) =>
            {
                _showRecycleBinOnDesktop = !_showRecycleBinOnDesktop;
                settings.ShowRecycleBinOnDesktop = _showRecycleBinOnDesktop;
                SettingsManager.SaveSettings(settings);

                RecycleBinVisibilityManager.SetRecycleBinVisibilityOnDesktop(_showRecycleBinOnDesktop);

                if (showRecycleBinOnDesktopMenu != null)
                {
                    showRecycleBinOnDesktopMenu.Checked = _showRecycleBinOnDesktop;
                }
            })
            {
                Checked = _showRecycleBinOnDesktop
            };

            // Добавляем пункты меню
            _notifyIcon.AddMenu("Открыть корзину", (_, _) => OpenRecycleBin());
            _notifyIcon.AddMenu("Очистить корзину", (_, _) =>
            {
                EmptyRecycleBin();
                UpdateTrayIcon();
            });
            _notifyIcon.AddMenu("-");
            _notifyIcon.AddMenu(showNotificationsMenu);
            _notifyIcon.AddMenu(autoStartMenu);
            _notifyIcon.AddMenu("-");
            _notifyIcon.AddMenu(showRecycleBinOnDesktopMenu);
            _notifyIcon.AddMenu("-");
            _notifyIcon.AddMenu("Выбрать иконку", null!, CreateIconPackMenuItems(_notifyIcon));
            _notifyIcon.AddMenu("-");
            _notifyIcon.AddMenu("Выход", (_, _) => Application.Exit());

            // Таймер для проверки состояния корзины
            var timer = new System.Windows.Forms.Timer { Interval = 1700 };  
            timer.Tick += (_, _) => UpdateTrayIcon(); 
            timer.Start();

            // Устанавливаем начальный набор иконок
            IconPackManager.ApplyIconPack(IconPackManager.LoadCurrentPack(), _notifyIcon);

            Application.Run();
        }

        private static void UpdateTrayIcon()
        {
            bool isRecycleBinEmpty = IsRecycleBinEmpty();

            // Проверяем, изменилось ли состояние корзины
            if (isRecycleBinEmpty != _previousRecycleBinState)
            {
                _previousRecycleBinState = isRecycleBinEmpty;
                IconPackManager.UpdateIconsBasedOnState(_notifyIcon, isRecycleBinEmpty);
            }

            UpdateTrayText(); 
        }

        private static bool IsRecycleBinEmpty()
        {
            SHQUERYRBINFO rbInfo = new() { cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            SHQueryRecycleBin(null, ref rbInfo);
            return rbInfo.i64NumItems == 0;
        }

        private static void UpdateTrayText()
        {
            SHQUERYRBINFO rbInfo = new() { cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            SHQueryRecycleBin(null, ref rbInfo);

            string text = $"Менеджер Корзины\n" +
                          $"Элементов: {rbInfo.i64NumItems}\n" +
                          $"Занято: {FormatFileSize(rbInfo.i64Size)}";
            _notifyIcon.Text = text;
        }

        private static string FormatFileSize(long sizeInBytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double len = sizeInBytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private static void OpenRecycleBin()
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true });
        }

        private static void EmptyRecycleBin()
        {
            const uint SHERB_NOCONFIRMATION = 0x00000001;
            const uint SHERB_NOPROGRESSUI = 0x00000002;
            const uint SHERB_NOSOUND = 0x00000004;

            int result = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

            if (result == 0)
            {
                ShowBalloonNotification("Корзина", "Корзина успешно очищена.", ToolTipIcon.Info);
                UpdateTrayIcon(); 
            }
            else
            {
                ShowBalloonNotification("Ошибка", "Не удалось очистить корзину.", ToolTipIcon.Error);
            }
        }

        private static ToolStripMenuItem[] CreateIconPackMenuItems(NotifyIcon notifyIcon)
        {
            string iconDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
            if (!Directory.Exists(iconDirectory))
            {
                Directory.CreateDirectory(iconDirectory);
            }

            var iconPacks = Directory.GetDirectories(iconDirectory)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();

            return iconPacks
                .Select(packName => IconPackManager.CreateIconPackMenuItem(packName!, notifyIcon))
                .ToArray();
        }

        private static void ShowBalloonNotification(string title, string message, ToolTipIcon iconType)
        {
            if (!_showNotifications) return;

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = iconType;
            _notifyIcon.ShowBalloonTip(3000);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

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
