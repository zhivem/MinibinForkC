using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MinibinFork
{
    static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetFolderPath(IntPtr hwnd, int csidl, IntPtr hToken, uint dwFlags, StringBuilder pszPath);

        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const int CSIDL_BITBUCKET = 0x0005;

        [StructLayout(LayoutKind.Sequential)]
        struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        static bool hideNotifications = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Загрузка настроек
            AppSettings appSettings = AppSettings.Load();
            hideNotifications = appSettings.HideNotifications;
            bool autoStartEnabled = appSettings.AutoStart;

            // Получение информации о корзине
            (long numItems, long size) = GetRecycleBinInfo();
            string sizeText = FormatSize(size);
            string tooltipText = $"Корзина: {numItems} элементов, {sizeText}";

            // Создание NotifyIcon
            NotifyIcon trayIcon = new()
            {
                Text = tooltipText,
                Icon = new Icon(GetIconPath(IsRecycleBinEmpty(), appSettings.SelectedIconPack), 40, 40),
                Visible = true
            };

            // Создание контекстного меню
            ContextMenuStrip trayMenu = new ContextMenuStrip();

            // Пункт меню "Открыть корзину"
            trayMenu.Items.Add("Открыть корзину", null, (s, e) => OpenRecycleBin());

            // Пункт меню "Очистить корзину"
            trayMenu.Items.Add("Очистить корзину", null, (s, e) => EmptyRecycleBin(trayIcon, appSettings));

            // Добавляем разделитель
            trayMenu.Items.Add(new ToolStripSeparator());

            // Создание и добавление меню выбора иконок через IconSelector
            string iconsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
            IconSelector iconSelector = new(iconsBasePath, appSettings.SelectedIconPack, appSettings, trayIcon, IsRecycleBinEmpty());
            ToolStripMenuItem chooseIconMenuItem = iconSelector.CreateChooseIconMenuItem();
            trayMenu.Items.Add(chooseIconMenuItem);

            // Добавляем разделитель
            trayMenu.Items.Add(new ToolStripSeparator());

            // Двойной клик, чтобы открыть корзину           
            trayIcon.DoubleClick += (s, e) => OpenRecycleBin();

            // Пункт меню "Отображать корзину на рабочем столе"
            ToolStripMenuItem showDesktopIconItem = new("Отображать корзину на рабочем столе")
            {
                CheckOnClick = true,
                Checked = DesktopIconManager.IsRecycleBinVisible()
            };

            showDesktopIconItem.CheckedChanged += (s, e) =>
            {
                DesktopIconManager.ToggleRecycleBinIcon(showDesktopIconItem.Checked);
            };

            trayMenu.Items.Add(showDesktopIconItem);

            // Добавляем разделитель
            trayMenu.Items.Add(new ToolStripSeparator());

            // Пункт меню "Показывать уведомления" с флажком
            ToolStripMenuItem hideNotificationsItem = new("Скрыть уведомления")
            {
                CheckOnClick = true,
                Checked = hideNotifications
            };
            hideNotificationsItem.CheckedChanged += (s, e) =>
            {
                hideNotifications = hideNotificationsItem.Checked;
                appSettings.HideNotifications = hideNotifications;
                appSettings.Save();
            };
            trayMenu.Items.Add(hideNotificationsItem);

            // Пункт меню "Автозапуск" с флажком
            ToolStripMenuItem autoStartItem = new ToolStripMenuItem("Автозапуск")
            {
                CheckOnClick = true,
                Checked = autoStartEnabled
            };
            autoStartItem.CheckedChanged += (s, e) =>
            {
                if (autoStartItem.Checked)
                {
                    AutoStartManager.EnableAutoStart();
                    appSettings.AutoStart = true;
                }
                else
                {
                    AutoStartManager.DisableAutoStart();
                    appSettings.AutoStart = false;
                }
                appSettings.Save();
            };
            trayMenu.Items.Add(autoStartItem);

            // Добавляем разделитель
            trayMenu.Items.Add(new ToolStripSeparator());

            // Пункт меню "Выход"
            trayMenu.Items.Add("Выход", null, (s, e) => Application.Exit());

            trayIcon.ContextMenuStrip = trayMenu;

            // Таймер для обновления иконки и информации в тултипе
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 3000; // 3 секунды
            timer.Tick += (s, e) =>
            {
                bool isEmpty = IsRecycleBinEmpty();
                Icon newIcon = new(GetIconPath(isEmpty, appSettings.SelectedIconPack), 40, 40);
                if (!trayIcon.Icon.Equals(newIcon))
                {
                    trayIcon.Icon.Dispose();
                    trayIcon.Icon = newIcon;
                }

                // Обновление информации о корзине
                (long currentNumItems, long currentSize) = GetRecycleBinInfo();
                string currentSizeText = FormatSize(currentSize);
                string currentTooltipText = $"Корзина: {currentNumItems} элементов, {currentSizeText}";
                if (trayIcon.Text != currentTooltipText)
                {
                    trayIcon.Text = currentTooltipText;
                }
            };
            timer.Start();

            // Установка начальной информации в тултипе
            trayIcon.Text = tooltipText;

            Application.Run();

            // Очистка ресурсов при выходе
            trayIcon.Icon?.Dispose();
            trayIcon.Dispose();
        }

        // Метод для проверки, пуста ли корзина
        static bool IsRecycleBinEmpty()
        {
            SHQUERYRBINFO rbInfo = new()
            {
                cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO))
            };
            int result = SHQueryRecycleBin(null, ref rbInfo);
            if (result != 0)
            {
                return false;
            }
            return rbInfo.i64NumItems == 0;
        }

        // Метод для получения информации о корзине
        static (long numItems, long size) GetRecycleBinInfo()
        {
            SHQUERYRBINFO rbInfo = new()
            {
                cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO))
            };
            int result = SHQueryRecycleBin(null, ref rbInfo);
            if (result != 0)
            {
                return (0, 0);
            }
            return (rbInfo.i64NumItems, rbInfo.i64Size);
        }

        // Метод для форматирования размера в удобочитаемый формат
        static string FormatSize(long bytes)
        {
            double size = bytes;
            string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.##} {units[unit]}";
        }

        // Метод для очистки корзины
        static void EmptyRecycleBin(NotifyIcon trayIcon, AppSettings appSettings)
        {
            int result = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION);
            if (result == 0 || result == -2147418113) // S_OK или S_FALSE
            {
                ShowNotification(trayIcon, "Корзина", "Корзина успешно очищена.", ToolTipIcon.Info);
            }
            else
            {
                ShowNotification(trayIcon, "Корзина", $"Произошла ошибка при очистке корзины. Код ошибки: {result}", ToolTipIcon.Error);
            }
            // Обновление иконки и тултипа
            trayIcon.Icon?.Dispose();
            trayIcon.Icon = new Icon(GetIconPath(IsRecycleBinEmpty(), appSettings.SelectedIconPack), 40, 40);

            (long numItems, long size) = GetRecycleBinInfo();
            string sizeText = FormatSize(size);
            trayIcon.Text = $"Корзина: {numItems} элементов, {sizeText}";
        }

        // Метод для открытия корзины
        static void OpenRecycleBin()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:RecycleBinFolder",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть корзину: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод для отображения уведомлений
        static void ShowNotification(NotifyIcon trayIcon, string title, string message, ToolTipIcon icon)
        {
            if (hideNotifications)
            {
                return;
            }

            trayIcon.BalloonTipTitle = title;
            trayIcon.BalloonTipText = message;
            trayIcon.BalloonTipIcon = icon;
            trayIcon.ShowBalloonTip(5000);
        }

        // Метод для получения пути к иконке
        static string GetIconPath(bool isEmpty, string selectedIconPack)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(selectedIconPack) || selectedIconPack == "Default")
            {
                return isEmpty
                    ? Path.Combine(basePath, "icons", "minibin-fork-empty.ico")
                    : Path.Combine(basePath, "icons", "minibin-fork-full.ico");
            }
            else
            {
                return isEmpty
                    ? Path.Combine(basePath, "icons", selectedIconPack, "minibin-fork-empty.ico")
                    : Path.Combine(basePath, "icons", selectedIconPack, "minibin-fork-full.ico");
            }
        }
    }
}
