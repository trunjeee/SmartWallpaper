using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Input;



namespace SmartWallpaperWPF
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        private const int SPI_SETDESKWALLPAPER = 0x14;
        private const int SPIF_UPDATEINIFILE = 0x1;
        private const int SPIF_SENDCHANGE = 0x2;

        private Dictionary<Forms.Screen, string> wallpaperMap = new Dictionary<Forms.Screen, string>();

        public MainWindow()
        {
            InitializeComponent();
            LoadMonitors();
        }

        private void LoadMonitors()
        {
            foreach (var screen in Forms.Screen.AllScreens)
            {
                int index = MonitorsPanel.Items.Count;

                // 🔹 Создаём Grid вместо StackPanel
                var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // тянущийся текст
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // фиксированная кнопка

                var label = new TextBlock
                {
                    Text = $"Display {index + 1}: ({screen.Bounds.Width}x{screen.Bounds.Height}) @ ({screen.Bounds.X},{screen.Bounds.Y})",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Left,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);

                var button = new Button
                {
                    Content = "Select wallpaper",
                    Tag = new Tuple<string, int>(screen.DeviceName, index),
                    Margin = new Thickness(40, 0, 0, 0), // ⬅️ отступ между текстом и кнопкой
                    Padding = new Thickness(16, 6, 16, 6),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 48, 48)),
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                Grid.SetColumn(button, 1);

                // 🔸 Создаём шаблон кнопки со скруглением
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));

                borderFactory.AppendChild(contentPresenterFactory);
                button.Template = new ControlTemplate(typeof(Button)) { VisualTree = borderFactory };

                button.Click += SelectWallpaper_Click;

                // Добавляем элементы в Grid
                grid.Children.Add(label);
                grid.Children.Add(button);

                // Вставляем в список
                MonitorsPanel.Items.Add(grid);
                wallpaperMap[screen] = "";
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove(); // позволяет двигать окно мышкой
        }


        private void SelectWallpaper_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var data = (Tuple<string, int>)button.Tag;

            var deviceName = data.Item1;
            var index = data.Item2;

            var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                var screen = Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
                if (screen == null) return;

                wallpaperMap[screen] = dlg.FileName;

                AddStatusMessage($"Selected file for display {index + 1}: {Path.GetFileName(dlg.FileName)}", true);
            }
        }




        private void AddStatusMessage(string message, bool isSuccess)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = isSuccess ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 87, 0)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(83, 20, 20)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 4, 0, 0),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = System.Windows.Media.Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            StatusPanel.Children.Add(border);
        }
        private void ApplyWallpaper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var allBounds = wallpaperMap.Keys.Select(s => s.Bounds).ToList();
                int minX = allBounds.Min(b => b.Left);
                int minY = allBounds.Min(b => b.Top);
                int maxX = allBounds.Max(b => b.Right);
                int maxY = allBounds.Max(b => b.Bottom);
                int width = maxX - minX;
                int height = maxY - minY;

                using var finalImage = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    g.Clear(System.Drawing.Color.Black);

                    foreach (var screen in wallpaperMap.Keys)
                    {
                        string file = wallpaperMap[screen];
                        if (!File.Exists(file)) continue;
                        using var img = System.Drawing.Image.FromFile(file);
                        Rectangle destRect = new Rectangle(
                            screen.Bounds.X - minX,
                            screen.Bounds.Y - minY,
                            screen.Bounds.Width,
                            screen.Bounds.Height
                        );
                        g.DrawImage(img, destRect); // учитывает масштаб
                    }
                }

                string tempPath = Path.Combine(Path.GetTempPath(), "smart_wallpaper_v3.bmp");
                finalImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);
                // Если мониторов больше 1 — меняем стиль обоев на Tile
                if (wallpaperMap.Keys.Count > 1)
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("WallpaperStyle", "0"); // 0 = центрировать
                            key.SetValue("TileWallpaper", "1");  // 1 = замостить
                        }
                    }
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, tempPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                AddStatusMessage("✅ Wallpaper applied successfully.", true);
            }
            catch (Exception ex)
            {
                AddStatusMessage($"❌ Failed to apply wallpaper: {ex.Message}", false);
                
            }

        }
    }
}