using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using SmartWallpaper.Services;

namespace SmartWallpaper;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (Environment.GetCommandLineArgs().Contains(StartupService.RestoreArgument))
        {
            await RestoreSilentlyAsync();
            Exit();
            return;
        }

        _window = new MainWindow();
        _window.Activate();
    }

    /// <summary>Запуск из автозагрузки: без окна ставим обои заново и выходим.</summary>
    private static async Task RestoreSilentlyAsync()
    {
        var settings = SettingsStore.Load();
        if (settings.Monitors.Count == 0) return;

        // Сразу после входа Проводник может ещё не подняться — пробуем несколько раз.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                await WallpaperService.RestoreAsync(settings);
                return;
            }
            catch (COMException)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
