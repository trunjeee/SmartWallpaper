using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using SmartWallpaper.Services;

namespace SmartWallpaper.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings = SettingsStore.Load();

    public ObservableCollection<MonitorViewModel> Monitors { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial MonitorViewModel? Selected { get; set; }

    public bool HasSelection => Selected is not null;

    /// <summary>Индекс в переключателе «Заполнить / Вписать / Растянуть» для выбранного монитора.</summary>
    public int SelectedFitIndex
    {
        get => (int)(Selected?.Fit ?? FitMode.Fill);
        set
        {
            if (Selected is null || value < 0 || (int)Selected.Fit == value) return;
            Selected.Fit = (FitMode)value;
            OnPropertyChanged();
        }
    }

    partial void OnSelectedChanged(MonitorViewModel? value) => OnPropertyChanged(nameof(SelectedFitIndex));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool HasChanges { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty] public partial string? StatusText { get; set; }
    [ObservableProperty] public partial InfoBarSeverity StatusSeverity { get; set; }

    public bool RestoreOnLogon
    {
        get => _settings.RestoreOnLogon;
        set
        {
            if (_settings.RestoreOnLogon == value) return;
            _settings.RestoreOnLogon = value;
            SettingsStore.Save(_settings);
            // Запись в автозагрузке имеет смысл, только когда уже есть что восстанавливать.
            StartupService.SetEnabled(value && _settings.Monitors.Count > 0);
            OnPropertyChanged();
        }
    }

    public MainViewModel() => LoadMonitors();

    public void LoadMonitors()
    {
        foreach (var m in Monitors) m.PropertyChanged -= OnMonitorChanged;
        Monitors.Clear();

        foreach (var info in MonitorService.GetMonitors())
        {
            var vm = new MonitorViewModel(info);
            if (_settings.Monitors.TryGetValue(info.Id, out var saved) && File.Exists(WallpaperService.LibraryPath(saved.ImageFile)))
            {
                vm.ImageFile = saved.ImageFile;
                vm.OriginalName = saved.OriginalName;
                vm.Fit = saved.Fit;
            }
            vm.PropertyChanged += OnMonitorChanged;
            Monitors.Add(vm);
        }

        Select(Monitors.FirstOrDefault());
        // Картинки выбраны, но ещё ни разу не применены (или готовые обои пропали) — есть что применять.
        HasChanges = Monitors.Any(m => m.HasImage && _settings.Monitors.TryGetValue(m.Info.Id, out var s)
            && (s.RenderedFile is null || !File.Exists(s.RenderedFile)));
    }

    private void OnMonitorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MonitorViewModel.ImageFile) or nameof(MonitorViewModel.Fit))
            HasChanges = true;
    }

    public void Select(MonitorViewModel? monitor)
    {
        foreach (var m in Monitors) m.IsSelected = m == monitor;
        Selected = monitor;
    }

    public void SetImage(MonitorViewModel monitor, string path)
    {
        if (!WallpaperService.IsSupported(path))
        {
            ShowStatus($"Формат {Path.GetExtension(path)} не поддерживается", InfoBarSeverity.Warning);
            return;
        }
        try
        {
            monitor.OriginalName = Path.GetFileName(path);
            monitor.ImageFile = WallpaperService.ImportImage(path);
            StatusText = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowStatus($"Не удалось прочитать файл: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        if (Selected is null) return;
        Selected.ImageFile = null;
        Selected.OriginalName = null;
    }

    private bool CanApply() => HasChanges && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        IsBusy = true;
        StatusText = null;
        try
        {
            foreach (var m in Monitors)
            {
                if (m.ImageFile is null)
                {
                    _settings.Monitors.Remove(m.Info.Id);
                    continue;
                }
                if (!_settings.Monitors.TryGetValue(m.Info.Id, out var config))
                    _settings.Monitors[m.Info.Id] = config = new MonitorWallpaper();
                if (config.ImageFile != m.ImageFile || config.Fit != m.Fit)
                    config.RenderedFile = null; // картинку нужно пересчитать
                config.ImageFile = m.ImageFile;
                config.OriginalName = m.OriginalName ?? "";
                config.Fit = m.Fit;
            }

            await WallpaperService.ApplyAsync(_settings, Monitors.Select(m => m.Info).ToList());
            HasChanges = false;

            int count = Monitors.Count(m => m.HasImage);
            ShowStatus(count == Monitors.Count
                    ? "Обои установлены на все мониторы"
                    : $"Обои установлены на {count} из {Monitors.Count}. На остальных остались прежние",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus($"Не удалось установить обои: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowStatus(string text, InfoBarSeverity severity)
    {
        StatusSeverity = severity;
        StatusText = text;
    }
}
