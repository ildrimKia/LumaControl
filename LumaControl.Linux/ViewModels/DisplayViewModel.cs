using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LumaControl.Models;
using LumaControl.Services;

namespace LumaControl.ViewModels;

internal sealed partial class DisplayViewModel : ObservableObject, IDisposable
{
    private readonly DisplayInfo _info;
    private readonly DdcutilService? _ddcutil;
    private readonly BacklightService? _backlight;

    private readonly DispatcherTimer _brightnessDebounce;
    private readonly DispatcherTimer _contrastDebounce;

    private bool _isLoading;

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private int _contrast;

    [ObservableProperty]
    private bool _isBusy;

    public string Name => _info.Name;
    public bool HasContrast => _info.HasContrast;

    // DDC display constructor
    public DisplayViewModel(DisplayInfo info, DdcutilService ddcutil)
    {
        _info = info;
        _ddcutil = ddcutil;
        _brightnessDebounce = CreateDebounceTimer(ApplyBrightnessAsync);
        _contrastDebounce = CreateDebounceTimer(ApplyContrastAsync);
    }

    // Backlight constructor
    public DisplayViewModel(DisplayInfo info, BacklightService backlight)
    {
        _info = info;
        _backlight = backlight;
        _brightnessDebounce = CreateDebounceTimer(ApplyBrightnessAsync);
        _contrastDebounce = CreateDebounceTimer(ApplyContrastAsync);
    }

    private static DispatcherTimer CreateDebounceTimer(Func<Task> callback)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            await callback();
        };
        return timer;
    }

    /// <summary>Loads current brightness/contrast values from the hardware.</summary>
    public async Task LoadValuesAsync()
    {
        _isLoading = true;
        IsBusy = true;
        try
        {
            if (_info.DisplayType == DisplayType.Ddc && _ddcutil is not null)
            {
                Brightness = await _ddcutil.GetBrightnessAsync(_info.Id);
                if (_info.HasContrast)
                    Contrast = await _ddcutil.GetContrastAsync(_info.Id);
            }
            else if (_info.DisplayType == DisplayType.Backlight && _backlight is not null)
            {
                Brightness = await _backlight.GetBrightnessAsync();
            }
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
        }
    }

    partial void OnBrightnessChanged(int value)
    {
        if (_isLoading) return;
        _brightnessDebounce.Stop();
        _brightnessDebounce.Start();
    }

    partial void OnContrastChanged(int value)
    {
        if (_isLoading) return;
        _contrastDebounce.Stop();
        _contrastDebounce.Start();
    }

    private async Task ApplyBrightnessAsync()
    {
        IsBusy = true;
        try
        {
            if (_info.DisplayType == DisplayType.Ddc && _ddcutil is not null)
                await _ddcutil.SetBrightnessAsync(_info.Id, Brightness);
            else if (_info.DisplayType == DisplayType.Backlight && _backlight is not null)
                await _backlight.SetBrightnessAsync(Brightness);
        }
        finally { IsBusy = false; }
    }

    private async Task ApplyContrastAsync()
    {
        if (!_info.HasContrast || _ddcutil is null) return;
        IsBusy = true;
        try
        {
            await _ddcutil.SetContrastAsync(_info.Id, Contrast);
        }
        finally { IsBusy = false; }
    }

    public void Dispose()
    {
        _brightnessDebounce.Stop();
        _contrastDebounce.Stop();
    }
}
