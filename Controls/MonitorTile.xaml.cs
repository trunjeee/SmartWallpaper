using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartWallpaper.Services;
using SmartWallpaper.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SmartWallpaper.Controls;

public sealed partial class MonitorTile : UserControl
{
    private static readonly Brush IdleBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A));
    private static readonly Brush HoverBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x6E, 0x6E, 0x6E));
    private bool _isHovered;
    private bool _isDragOver;

    public MonitorViewModel? Monitor { get; }

    /// <summary>Пользователь бросил на плитку файл картинки.</summary>
    public event Action<MonitorTile, string>? ImageDropped;

    public MonitorTile(MonitorViewModel monitor)
    {
        Monitor = monitor;
        InitializeComponent();
        monitor.PropertyChanged += OnMonitorChanged;
        Unloaded += (_, _) => monitor.PropertyChanged -= OnMonitorChanged;
        UpdateFrame();
    }

    private void OnMonitorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitorViewModel.IsSelected)) UpdateFrame();
    }

    private void UpdateFrame() =>
        Frame.BorderBrush = Monitor?.IsSelected == true || _isDragOver
            ? (Brush)Application.Current.Resources["AccentLimeBrush"]
            : _isHovered ? HoverBrush : IdleBrush;

    // ---------- функции для x:Bind ----------

    private Stretch PreviewStretch(FitMode fit) => fit switch
    {
        FitMode.Fit => Stretch.Uniform,
        FitMode.Stretch => Stretch.Fill,
        _ => Stretch.UniformToFill,
    };

    private Visibility EmptyVisibility(bool hasImage) => hasImage ? Visibility.Collapsed : Visibility.Visible;
    private Visibility PrimaryVisibility(bool isPrimary) => isPrimary ? Visibility.Visible : Visibility.Collapsed;

    // ---------- наведение ----------

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e) { _isHovered = true; UpdateFrame(); }
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) { _isHovered = false; UpdateFrame(); }

    // ---------- перетаскивание файла ----------

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = $"Поставить на монитор {Monitor?.Number}";
        _isDragOver = true;
        UpdateFrame();
    }

    private void OnDragLeave(object sender, DragEventArgs e) { _isDragOver = false; UpdateFrame(); }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        _isDragOver = false;
        UpdateFrame();
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var file = items.OfType<StorageFile>().FirstOrDefault(f => WallpaperService.IsSupported(f.Path));
        if (file is not null) ImageDropped?.Invoke(this, file.Path);
    }
}
