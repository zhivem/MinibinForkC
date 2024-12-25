namespace MinibinFork
{
    public class IconSelector
    {
        private readonly string iconsBasePath;
        private readonly string selectedIconPack;
        private readonly AppSettings appSettings;
        private readonly NotifyIcon trayIcon;
        private readonly bool isRecycleBinEmpty;

        public IconSelector(string iconsBasePath, string selectedIconPack, AppSettings appSettings, NotifyIcon trayIcon, bool isRecycleBinEmpty)
        {
            this.iconsBasePath = iconsBasePath;
            this.selectedIconPack = selectedIconPack;
            this.appSettings = appSettings;
            this.trayIcon = trayIcon;
            this.isRecycleBinEmpty = isRecycleBinEmpty;
        }

        public ToolStripMenuItem CreateChooseIconMenuItem()
        {
            ToolStripMenuItem chooseIconMenuItem = new("Выбрать иконку");

            var iconPacks = Directory.GetDirectories(iconsBasePath)
                                     .Select(Path.GetFileName)
                                     .Where(name => name != null &&
                                                Directory.Exists(Path.Combine(iconsBasePath, name)) &&
                                                File.Exists(Path.Combine(iconsBasePath, name, "minibin-fork-empty.ico")) &&
                                                File.Exists(Path.Combine(iconsBasePath, name, "minibin-fork-full.ico")))
                                     .OfType<string>() 
                                     .ToList();

            // Добавляем пакет иконок, включая "Default"
            var allIconPacks = new List<string> { "Default" };
            allIconPacks.AddRange(iconPacks);

            foreach (var pack in allIconPacks)
            {
                ToolStripMenuItem packItem = new(pack)
                {
                    Checked = pack == appSettings.SelectedIconPack,
                    CheckOnClick = true
                };

                // Загрузка иконки для предпросмотра
                string iconPath = pack == "Default"
                    ? Path.Combine(iconsBasePath, "minibin-fork-empty.ico")
                    : Path.Combine(iconsBasePath, pack, "minibin-fork-empty.ico");

                if (File.Exists(iconPath))
                {
                    try
                    {
                        using (Icon originalIcon = new(iconPath))
                        {
                            // Масштабируем иконку до 16x16 пикселей
                            Bitmap bitmap = originalIcon.ToBitmap();
                            Bitmap resizedBitmap = new(bitmap, new Size(16, 16));
                            packItem.Image = resizedBitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не удалось загрузить иконку для пакета '{pack}': {ex.Message}");
                        packItem.Image = null;
                    }
                }
                else
                {
                    packItem.Image = null;
                }

                packItem.Click += (s, e) =>
                {
                    if (pack == "Default")
                    {
                        // Устанавливаем "Default"
                        appSettings.SelectedIconPack = "Default";
                        packItem.Checked = true;
                        foreach (ToolStripMenuItem item in chooseIconMenuItem.DropDownItems)
                        {
                            if (item != packItem)
                                item.Checked = false;
                        }
                    }
                    else
                    {
                        // Проверка наличия иконок в пакете
                        string emptyIconPath = Path.Combine(iconsBasePath, pack, "minibin-fork-empty.ico");
                        string fullIconPath = Path.Combine(iconsBasePath, pack, "minibin-fork-full.ico");
                        if (!File.Exists(emptyIconPath) || !File.Exists(fullIconPath))
                        {
                            MessageBox.Show($"В пакете иконок '{pack}' отсутствуют необходимые файлы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            packItem.Checked = false;
                            return;
                        }

                        // Устанавливаем выбранный пакет
                        appSettings.SelectedIconPack = pack;
                        packItem.Checked = true;
                        foreach (ToolStripMenuItem item in chooseIconMenuItem.DropDownItems)
                        {
                            if (item != packItem)
                                item.Checked = false;
                        }
                    }

                    // Сохранение настроек
                    appSettings.Save();

                    // Обновление иконки трея
                    UpdateTrayIcon();
                };

                chooseIconMenuItem.DropDownItems.Add(packItem);
            }

            return chooseIconMenuItem;
        }

        private void UpdateTrayIcon()
        {
            bool currentState = isRecycleBinEmpty;
            trayIcon.Icon?.Dispose();
            trayIcon.Icon = new Icon(GetIconPath(currentState, appSettings.SelectedIconPack), 40, 40);
        }

        // Метод для получения пути к иконке
        private string GetIconPath(bool isEmpty, string selectedIconPack)
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
