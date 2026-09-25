using Microsoft.Win32;

namespace SmartWallpaper.Services;

/// <summary>
/// Тихое восстановление обоев при входе в Windows: запись в автозагрузке текущего пользователя
/// запускает «SmartWallpaper.exe --restore» — без окна, пара секунд работы и выход.
/// </summary>
public static class StartupService
{
    public const string RestoreArgument = "--restore";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "SmartWallpaper";

    private static string Command => $"\"{Environment.ProcessPath}\" {RestoreArgument}";

    public static void SetEnabled(bool enabled)
    {
        using (var run = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (enabled) run.SetValue(ValueName, Command);
            else run.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        // Сбрасываем отметку «отключено» из Диспетчера задач, иначе Windows проигнорирует запись.
        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
