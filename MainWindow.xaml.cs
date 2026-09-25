using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SmartWallpaper.Controls;
using SmartWallpaper.Native;
using SmartWallpaper.Services;
using SmartWallpaper.ViewModels;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SmartWallpaper;

public sealed partial class MainWindow : Window
{
    private const int WidthDip = 1060, HeightDip = 660;
    private readonly nint _hwnd;

    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        _hwnd = WindowNative.GetWindowHandle(this);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 820;
            presenter.PreferredMinimumHeight = 560;
        }
        CenterOnScreen();

        BuildTiles();
        ViewModel.Monitors.CollectionChanged += (_, _) => BuildTiles();
    }

    private void CenterOnScreen()
    {
        double scale = Win32.GetDpiForWindow(_hwnd) / 96d;
        int w = (int)(WidthDip * scale), h = (int)(HeightDip * scale);
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.MoveAndResize(new RectInt32(area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h));
    }

    private void BuildTiles()
    {
        LayoutPanel.Children.Clear();
        foreach (var monitor in ViewModel.Monitors)
        {
            var tile = new MonitorTile(monitor);
            tile.Tapped += (_, _) => ViewModel.Select(monitor);
            tile.DoubleTapped += async (_, _) => await PickImageAsync(monitor);
            tile.ImageDropped += (_, path) =>
            {
                ViewModel.Select(monitor);
                ViewModel.SetImage(monitor, path);
            };
            LayoutPanel.Children.Add(tile);
        }
    }

    // ---------- функции для x:Bind ----------

    private string FileLabel(string? originalName) => originalName ?? "Не выбрана — останутся текущие обои";
    private bool IsStatusOpen(string? status) => status is not null;

    // ---------- действия ----------

    private async void PickImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Selected is { } monitor) await PickImageAsync(monitor);
    }

    private async Task PickImageAsync(MonitorViewModel monitor)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
        };
        foreach (var ext in WallpaperService.SupportedExtensions) picker.FileTypeFilter.Add(ext);
        InitializeWithWindow.Initialize(picker, _hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null) ViewModel.SetImage(monitor, file.Path);
    }

    private void Identify_Click(object sender, RoutedEventArgs e) =>
        IdentifyWindow.ShowAll(ViewModel.Monitors.Select(m => m.Info));
}
