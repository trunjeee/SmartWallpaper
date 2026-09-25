using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartWallpaper.Services;

public enum FitMode
{
    /// <summary>Заполнить экран, лишнее обрезать по краям.</summary>
    Fill,
    /// <summary>Картинка целиком, по краям чёрные поля.</summary>
    Fit,
    /// <summary>Растянуть на весь экран без сохранения пропорций.</summary>
    Stretch,
}

public sealed class MonitorWallpaper
{
    /// <summary>Копия исходной картинки в папке library (оригинал можно удалять/переносить).</summary>
    public string ImageFile { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public FitMode Fit { get; set; } = FitMode.Fill;

    /// <summary>Готовые обои под разрешение монитора, которые отданы Windows.</summary>
    public string? RenderedFile { get; set; }
    public int RenderedWidth { get; set; }
    public int RenderedHeight { get; set; }
}

public sealed class AppSettings
{
    /// <summary>Ключ — путь устройства монитора (как в IDesktopWallpaper).</summary>
    public Dictionary<string, MonitorWallpaper> Monitors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool RestoreOnLogon { get; set; } = true;
}

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Всё хранится в %LOCALAPPDATA%\SmartWallpaper — не во временной папке, которую чистит Windows.</summary>
public static class AppPaths
{
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartWallpaper");
    public static readonly string Library = Path.Combine(Root, "library");
    public static readonly string Rendered = Path.Combine(Root, "rendered");
    public static readonly string SettingsFile = Path.Combine(Root, "settings.json");
}

public static class SettingsStore
{
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var settings = JsonSerializer.Deserialize(File.ReadAllText(AppPaths.SettingsFile), SettingsJsonContext.Default.AppSettings);
                if (settings is not null)
                {
                    // После десериализации словарь чувствителен к регистру — возвращаем нечувствительный.
                    settings.Monitors = new(settings.Monitors, StringComparer.OrdinalIgnoreCase);
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return new();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppPaths.Root);
        // Пишем во временный файл и подменяем — при отключении питания не останется «половины» настроек.
        string temp = AppPaths.SettingsFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
        File.Move(temp, AppPaths.SettingsFile, overwrite: true);
    }
}
