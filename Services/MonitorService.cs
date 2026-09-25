using SmartWallpaper.Native;

namespace SmartWallpaper.Services;

public sealed record MonitorInfo(
    string Id,           // путь устройства монитора — стабилен между перезагрузками
    int Number,          // наш номер: слева направо, сверху вниз
    string Name,         // название из EDID, например «XF253Q»
    Win32.RECT Bounds,   // физические пиксели рабочего стола
    int ScalePercent,
    bool IsPrimary)
{
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
}

public static class MonitorService
{
    /// <summary>Мониторы, у которых есть свой рабочий стол (дубли вроде выключенного ТВ сюда не попадают).</summary>
    public static List<MonitorInfo> GetMonitors()
    {
        var names = DisplayConfig.GetMonitorNames();
        var wallpaper = (IDesktopWallpaper)new DesktopWallpaperComObject();

        var raw = new List<(string Id, Win32.RECT Rect)>();
        uint count = wallpaper.GetMonitorDevicePathCount();
        for (uint i = 0; i < count; i++)
        {
            string id = wallpaper.GetMonitorDevicePathAt(i);
            Win32.RECT rect;
            try { rect = wallpaper.GetMonitorRECT(id); }
            catch (System.Runtime.InteropServices.COMException) { continue; } // монитор отключён, но ещё числится
            if (rect.Width > 0 && rect.Height > 0) raw.Add((id, rect));
        }

        return raw
            .OrderBy(m => m.Rect.Top >= RowBreak(raw) ? 1 : 0) // сначала верхний ряд
            .ThenBy(m => m.Rect.Left)
            .Select((m, index) => new MonitorInfo(
                m.Id,
                index + 1,
                names.TryGetValue(m.Id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : $"Монитор {index + 1}",
                m.Rect,
                GetScalePercent(m.Rect),
                m.Rect.Left == 0 && m.Rect.Top == 0))
            .ToList();

        // Граница между «верхним» и «нижним» рядом — середина высоты самого верхнего монитора.
        static int RowBreak(List<(string Id, Win32.RECT Rect)> list)
        {
            var top = list.MinBy(m => m.Rect.Top).Rect;
            return top.Top + top.Height / 2;
        }
    }

    private static int GetScalePercent(Win32.RECT rect)
    {
        var center = new Win32.POINT { X = rect.Left + rect.Width / 2, Y = rect.Top + rect.Height / 2 };
        nint monitor = Win32.MonitorFromPoint(center, Win32.MONITOR_DEFAULTTONEAREST);
        return Win32.GetDpiForMonitor(monitor, Win32.MDT_EFFECTIVE_DPI, out uint dpi, out _) == 0
            ? (int)Math.Round(dpi * 100 / 96.0)
            : 100;
    }
}
