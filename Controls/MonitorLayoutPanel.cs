using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace SmartWallpaper.Controls;

/// <summary>Раскладывает плитки мониторов так же, как мониторы стоят в «Параметры → Дисплей», с сохранением пропорций.</summary>
public sealed partial class MonitorLayoutPanel : Panel
{
    private const double Padding = 24;
    private const double Gap = 6;

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = Compute(availableSize);
        foreach (var (tile, rect) in layout) tile.Measure(new Size(rect.Width, rect.Height));
        return new Size(
            double.IsInfinity(availableSize.Width) ? 600 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 400 : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var (tile, rect) in Compute(finalSize)) tile.Arrange(rect);
        return finalSize;
    }

    private List<(MonitorTile Tile, Rect Rect)> Compute(Size size)
    {
        var tiles = Children.OfType<MonitorTile>().Where(t => t.Monitor is not null).ToList();
        if (tiles.Count == 0) return [];

        var bounds = tiles.Select(t => t.Monitor!.Info.Bounds).ToList();
        double minX = bounds.Min(b => b.Left), minY = bounds.Min(b => b.Top);
        double totalW = bounds.Max(b => b.Right) - minX, totalH = bounds.Max(b => b.Bottom) - minY;

        double availW = Math.Max(1, (double.IsInfinity(size.Width) ? 600 : size.Width) - Padding * 2);
        double availH = Math.Max(1, (double.IsInfinity(size.Height) ? 400 : size.Height) - Padding * 2);
        double scale = Math.Min(availW / totalW, availH / totalH);
        double offsetX = Padding + (availW - totalW * scale) / 2;
        double offsetY = Padding + (availH - totalH * scale) / 2;

        return tiles.Select(t =>
        {
            var b = t.Monitor!.Info.Bounds;
            var rect = new Rect(
                offsetX + (b.Left - minX) * scale + Gap / 2,
                offsetY + (b.Top - minY) * scale + Gap / 2,
                Math.Max(0, b.Width * scale - Gap),
                Math.Max(0, b.Height * scale - Gap));
            return (t, rect);
        }).ToList();
    }
}
