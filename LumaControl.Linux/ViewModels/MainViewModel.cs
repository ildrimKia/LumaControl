using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LumaControl.Models;
using LumaControl.Services;

namespace LumaControl.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
    private readonly DdcutilService _ddcutil = new();
    private readonly BacklightService _backlight = new();

    [ObservableProperty]
    private ObservableCollection<DisplayViewModel> _displays = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Loading displays…";

    [RelayCommand]
    public async Task LoadDisplaysAsync()
    {
        IsLoading = true;
        StatusMessage = "Detecting displays…";

        // Dispose previous view models
        foreach (var d in Displays) d.Dispose();
        Displays.Clear();

        var newDisplays = new List<DisplayViewModel>();

        // Detect backlight (laptop internal panel) first
        try
        {
            var backlightInfo = await _backlight.DetectAsync();
            if (backlightInfo is not null)
            {
                var vm = new DisplayViewModel(backlightInfo, _backlight);
                await vm.LoadValuesAsync();
                newDisplays.Add(vm);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backlight error: {ex.Message}";
        }

        // Detect external DDC/CI monitors
        try
        {
            var ddcDisplays = await _ddcutil.DetectAsync();
            foreach (var info in ddcDisplays)
            {
                var vm = new DisplayViewModel(info, _ddcutil);
                await vm.LoadValuesAsync();
                newDisplays.Add(vm);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"DDC error: {ex.Message}";
        }

        foreach (var vm in newDisplays)
            Displays.Add(vm);

        StatusMessage = Displays.Count == 0
            ? "No displays found. Check ddcutil/brightnessctl installation."
            : string.Empty;

        IsLoading = false;
    }
}
