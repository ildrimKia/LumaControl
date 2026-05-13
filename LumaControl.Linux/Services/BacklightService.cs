using System.Text.RegularExpressions;
using LumaControl.Models;

namespace LumaControl.Services;

/// <summary>
/// Wraps brightnessctl CLI to control the laptop internal backlight.
/// Only brightness is available; contrast is not possible via backlight interfaces.
/// </summary>
internal sealed partial class BacklightService
{
    private const string Tool = "brightnessctl";

    private string? _deviceName;
    private int _maxBrightness = 100;

    // Matches: Device 'intel_backlight' of class 'backlight':
    [GeneratedRegex(@"Device '([^']+)' of class '([^']+)'")]
    private static partial Regex DeviceLineRegex();

    // Matches: Max brightness: 3000
    [GeneratedRegex(@"Max brightness:\s+(\d+)")]
    private static partial Regex MaxBrightnessRegex();

    // Matches: Current brightness: 1500
    [GeneratedRegex(@"Current brightness:\s+(\d+)")]
    private static partial Regex CurrentBrightnessRegex();

    public static async Task<bool> IsAvailableAsync()
    {
        try
        {
            var result = await ProcessRunner.RunAsync(Tool, "--version");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Detects the first backlight device (laptop panel).</summary>
    public async Task<DisplayInfo?> DetectAsync()
    {
        var result = await ProcessRunner.RunAsync(Tool, "list");
        if (!result.Success && string.IsNullOrWhiteSpace(result.Output))
            return null;

        return ParseListOutput(result.Output);
    }

    private DisplayInfo? ParseListOutput(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var deviceMatch = DeviceLineRegex().Match(line);
            if (!deviceMatch.Success) continue;

            string deviceName = deviceMatch.Groups[1].Value;
            string deviceClass = deviceMatch.Groups[2].Value;

            // Only consider actual backlight devices, not LEDs/keyboards
            if (!deviceClass.Equals("backlight", StringComparison.OrdinalIgnoreCase))
                continue;

            _deviceName = deviceName;

            // Parse max brightness from the same block (next few lines)
            var maxMatch = MaxBrightnessRegex().Match(output);
            if (maxMatch.Success)
                _maxBrightness = int.Parse(maxMatch.Groups[1].Value);

            string friendlyName = FormatDeviceName(deviceName);
            return new DisplayInfo(
                Id: deviceName,
                Name: $"{friendlyName} (Built-in)",
                DisplayType: DisplayType.Backlight,
                HasContrast: false);
        }

        return null;
    }

    private static string FormatDeviceName(string raw)
    {
        if (raw.StartsWith("intel_", StringComparison.OrdinalIgnoreCase)) return "Intel Backlight";
        if (raw.StartsWith("amdgpu_", StringComparison.OrdinalIgnoreCase)) return "AMD Backlight";
        if (raw.StartsWith("acpi_", StringComparison.OrdinalIgnoreCase)) return "ACPI Backlight";
        if (raw.StartsWith("radeon_", StringComparison.OrdinalIgnoreCase)) return "Radeon Backlight";
        return raw;
    }

    /// <summary>Gets current brightness as 0–100 percent.</summary>
    public async Task<int> GetBrightnessAsync()
    {
        if (_deviceName is null) return 50;

        var result = await ProcessRunner.RunAsync(Tool, $"-d {_deviceName} --raw get");
        if (!result.Success) return 50;

        if (!int.TryParse(result.Output.Trim(), out int raw)) return 50;
        if (_maxBrightness <= 0) return raw;

        return raw * 100 / _maxBrightness;
    }

    /// <summary>Sets brightness as 0–100 percent.</summary>
    public async Task SetBrightnessAsync(int percent)
    {
        if (_deviceName is null) return;

        int clamped = Math.Clamp(percent, 0, 100);
        await ProcessRunner.RunAsync(Tool, $"-d {_deviceName} set {clamped}%");
    }
}
