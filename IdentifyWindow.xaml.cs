using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SmartWallpaper.Native;
using SmartWallpaper.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace SmartWallpaper;

/// <summary>Крупный номер по центру реального монитора — как «Определить» в параметрах Windows.</summary>
public sealed partial class IdentifyWindow : Window
{
    private const int SizeDip = 280;

    public IdentifyWindow(MonitorInfo monitor)
    {
        InitializeComponent();
        NumberText.Text = monitor.Number.ToString();
        NameText.Text = monitor.Name;
        DetailsText.Text = $"{monitor.Width} × {monitor.Height} · {monitor.ScalePercent}%";

        var presenter = OverlappedPresenter.Create();
        AppWindow.SetPresenter(presenter);
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.IsShownInSwitchers = false;

        nint hwnd = WindowNative.GetWindowHandle(this);
        // Не забирает фокус у главного окна и не появляется в панели задач.
        nint ex = Win32.GetWindowLongPtrW(hwnd, Win32.GWL_EXSTYLE);
        Win32.SetWindowLongPtrW(hwnd, Win32.GWL_EXSTYLE, ex | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE);
        int corner = Win32.DWMWCP_ROUND;
        Win32.DwmSetWindowAttribute(hwnd, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        int size = (int)Math.Round(SizeDip * monitor.ScalePercent / 100.0);
        var b = monitor.Bounds;
        var target = new RectInt32(b.Left + (b.Width - size) / 2, b.Top + (b.Height - size) / 2, size, size);
        // Первый вызов переносит окно на монитор (Windows при этом масштабирует его под DPI экрана),
        // второй задаёт точный размер уже в масштабе целевого монитора.
        AppWindow.MoveAndResize(target);
        AppWindow.MoveAndResize(target);
    }

    /// <summary>Показывает номера на всех мониторах на несколько секунд.</summary>
    public static void ShowAll(IEnumerable<MonitorInfo> monitors)
    {
        foreach (var monitor in monitors)
        {
            var window = new IdentifyWindow(monitor);
            window.AppWindow.Show(activateWindow: false);
            var timer = window.DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.IsRepeating = false;
            timer.Tick += (t, _) => { t.Stop(); window.Close(); };
            timer.Start();
        }
    }
}
