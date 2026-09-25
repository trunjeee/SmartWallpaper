using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SmartWallpaper.Services;

namespace SmartWallpaper.ViewModels;

public sealed partial class MonitorViewModel(MonitorInfo info) : ObservableObject
{
    public MonitorInfo Info { get; } = info;
    public int Number => Info.Number;
    public string Name => Info.Name;
    public bool IsPrimary => Info.IsPrimary;
    public string ResolutionText => $"{Info.Width} × {Info.Height} · {Info.ScalePercent}%";

    [ObservableProperty] public partial bool IsSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage), nameof(Preview))]
    public partial string? ImageFile { get; set; }

    [ObservableProperty] public partial string? OriginalName { get; set; }
    [ObservableProperty] public partial FitMode Fit { get; set; } = FitMode.Fill;

    public bool HasImage => ImageFile is not null;

    /// <summary>Уменьшенная копия для превью — не держим в памяти 4K-картинки целиком.</summary>
    public ImageSource? Preview => ImageFile is null
        ? null
        : new BitmapImage(new Uri(WallpaperService.LibraryPath(ImageFile))) { DecodePixelWidth = 640 };
}
