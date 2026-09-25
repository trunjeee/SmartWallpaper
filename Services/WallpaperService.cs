using System.Security.Cryptography;
using System.Text;
using SmartWallpaper.Native;

namespace SmartWallpaper.Services;

/// <summary>Установка обоев на каждый монитор отдельно и их восстановление после сбоев.</summary>
public static class WallpaperService
{
    public static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic", ".jfif", ".tif", ".tiff", ".gif"];

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Копирует выбранную картинку в свою папку — обои не пропадут, если оригинал удалят или переместят.</summary>
    public static string ImportImage(string sourcePath)
    {
        Directory.CreateDirectory(AppPaths.Library);
        string name = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath).ToLowerInvariant()}";
        File.Copy(sourcePath, Path.Combine(AppPaths.Library, name));
        return name;
    }

    public static string LibraryPath(string imageFile) => Path.Combine(AppPaths.Library, imageFile);

    /// <summary>Готовит обои под каждый монитор, отдаёт их Windows и сохраняет настройки.</summary>
    public static async Task ApplyAsync(AppSettings settings, IReadOnlyList<MonitorInfo> monitors)
    {
        // Сначала готовим все картинки: если что-то не получится, рабочий стол останется нетронутым.
        var ready = new List<(MonitorInfo Monitor, MonitorWallpaper Config, string Rendered)>();
        foreach (var monitor in monitors)
        {
            if (!settings.Monitors.TryGetValue(monitor.Id, out var config)) continue;
            string source = LibraryPath(config.ImageFile);
            if (!File.Exists(source)) continue;

            // Каждый раз новое имя файла: Windows кэширует обои по пути и может не заметить новую картинку.
            string rendered = Path.Combine(AppPaths.Rendered, $"{MonitorKey(monitor.Id)}-{DateTime.Now:yyyyMMddHHmmssfff}.png");
            await WallpaperRenderer.RenderAsync(source, rendered, monitor.Width, monitor.Height, config.Fit);
            ready.Add((monitor, config, rendered));
        }

        var wallpaper = (IDesktopWallpaper)new DesktopWallpaperComObject();
        // Картинки уже ровно под разрешение монитора, «Заполнение» ничего не исказит.
        wallpaper.SetPosition(DesktopWallpaperPosition.Fill);
        foreach (var (monitor, config, rendered) in ready)
        {
            wallpaper.SetWallpaper(monitor.Id, rendered);
            string? previous = config.RenderedFile;
            (config.RenderedFile, config.RenderedWidth, config.RenderedHeight) = (rendered, monitor.Width, monitor.Height);
            TryDelete(previous);
        }

        Persist(settings);
    }

    /// <summary>
    /// Режим «--restore» при входе в Windows: ставит обои заново. Если после отключения питания
    /// Windows откатилась или потеряла кэш — всё вернётся. Если сменилось разрешение — картинка пересчитается.
    /// </summary>
    public static async Task RestoreAsync(AppSettings settings)
    {
        var monitors = MonitorService.GetMonitors();
        var wallpaper = (IDesktopWallpaper)new DesktopWallpaperComObject();
        wallpaper.SetPosition(DesktopWallpaperPosition.Fill);
        bool changed = false;

        foreach (var monitor in monitors)
        {
            if (!settings.Monitors.TryGetValue(monitor.Id, out var config)) continue;

            bool renderedOk = config.RenderedFile is not null && File.Exists(config.RenderedFile)
                && config.RenderedWidth == monitor.Width && config.RenderedHeight == monitor.Height;
            if (!renderedOk)
            {
                string source = LibraryPath(config.ImageFile);
                if (!File.Exists(source)) continue;
                string rendered = Path.Combine(AppPaths.Rendered, $"{MonitorKey(monitor.Id)}-{DateTime.Now:yyyyMMddHHmmssfff}.png");
                await WallpaperRenderer.RenderAsync(source, rendered, monitor.Width, monitor.Height, config.Fit);
                TryDelete(config.RenderedFile);
                (config.RenderedFile, config.RenderedWidth, config.RenderedHeight) = (rendered, monitor.Width, monitor.Height);
                changed = true;
            }

            wallpaper.SetWallpaper(monitor.Id, config.RenderedFile!);
        }

        if (changed) SettingsStore.Save(settings);
        Win32.RegFlushKey(Win32.HKEY_CURRENT_USER);
    }

    private static void Persist(AppSettings settings)
    {
        SettingsStore.Save(settings);
        CleanupLibrary(settings);
        StartupService.SetEnabled(settings.RestoreOnLogon);
        // Windows пишет реестр на диск с задержкой; при внезапном отключении питания
        // выбор обоев мог теряться — принудительно сбрасываем на диск прямо сейчас.
        Win32.RegFlushKey(Win32.HKEY_CURRENT_USER);
    }

    /// <summary>Удаляет из library картинки, которые больше ни на одном мониторе не используются.</summary>
    private static void CleanupLibrary(AppSettings settings)
    {
        if (!Directory.Exists(AppPaths.Library)) return;
        var used = settings.Monitors.Values.Select(m => m.ImageFile).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(AppPaths.Library))
            if (!used.Contains(Path.GetFileName(file))) TryDelete(file);

        var rendered = settings.Monitors.Values.Select(m => m.RenderedFile).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(AppPaths.Rendered))
            foreach (var file in Directory.EnumerateFiles(AppPaths.Rendered))
                if (!rendered.Contains(file)) TryDelete(file);
    }

    private static string MonitorKey(string id) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(id.ToUpperInvariant())))[..8].ToLowerInvariant();

    private static void TryDelete(string? path)
    {
        if (path is null) return;
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
