using System.Runtime.InteropServices;

namespace SmartWallpaper.Native;

public static class Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int MDT_EFFECTIVE_DPI = 0;

    [DllImport("user32.dll")]
    public static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(nint hwnd);

    // ---------- реестр ----------
    public static readonly nint HKEY_CURRENT_USER = unchecked((nint)0x80000001);

    /// <summary>Сразу записывает изменения куста реестра на диск (иначе Windows делает это «когда-нибудь»).</summary>
    [DllImport("advapi32.dll")]
    public static extern int RegFlushKey(nint hKey);

    // ---------- DWM ----------
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

    // ---------- окна ----------
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    public static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);
}
