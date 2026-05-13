using System.Text.RegularExpressions;
using LumaControl.Models;

namespace LumaControl.Services;

/// <summary>
/// Wraps ddcutil CLI to detect external monitors and read/write VCP brightness (0x10) and contrast (0x12).
/// Requires the user to be in the 'i2c' group: sudo usermod $USER -aG i2c
/// </summary>
internal sealed partial class DdcutilService
{
    private const string Tool = "ddcutil";

    // Matches "Display 1" lines in 'ddcutil detect --brief' output
    [GeneratedRegex(@"^Display\s+(\d+)", RegexOptions.Multiline)]
    private static partial Regex DisplayNumberRegex();

    // Matches "   Monitor:   MFG:Model:Serial" lines
    [GeneratedRegex(@"^\s+Monitor:\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex MonitorLineRegex();

    // Matches terse getvcp output: "VCP 0A C <current> <max>"
    [GeneratedRegex(@"^VCP\s+([0-9A-Fa-f]+)\s+\S+\s+(\d+)\s+(\d+)", RegexOptions.Multiline)]
    private static partial Regex VcpValueRegex();

    /// <summary>Returns whether ddcutil is installed and accessible.</summary>
    public static async Task<bool> IsAvailableAsync()
    {
        try
        {
            var result = await ProcessRunner.RunAsync(Tool, "--version");
            return result.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Detects connected DDC/CI capable monitors.</summary>
    public async Task<IReadOnlyList<DisplayInfo>> DetectAsync()
    {
        var result = await ProcessRunner.RunAsync(Tool, "detect --brief");

        if (!result.Success && string.IsNullOrWhiteSpace(result.Output))
            return [];

        return ParseDetectOutput(result.Output);
    }

    private static IReadOnlyList<DisplayInfo> ParseDetectOutput(string output)
    {
        var displays = new List<DisplayInfo>();
        var displayMatches = DisplayNumberRegex().Matches(output);
        var monitorMatches = MonitorLineRegex().Matches(output);

        for (int i = 0; i < displayMatches.Count; i++)
        {
            var numStr = displayMatches[i].Groups[1].Value;
            if (!int.TryParse(numStr, out int num)) continue;

            string name = i < monitorMatches.Count
                ? FormatMonitorName(monitorMatches[i].Groups[1].Value)
                : $"Display {num}";

            displays.Add(new DisplayInfo(
                Id: numStr,
                Name: name,
                DisplayType: DisplayType.Ddc,
                HasContrast: true));
        }

        return displays;
    }

    private static string FormatMonitorName(string raw)
    {
        // Format: "MFG:Model:Serial" — show "MFG Model"
        var parts = raw.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
            return $"{parts[0]} {parts[1]}";
        return raw.Trim();
    }

    /// <summary>Gets the current brightness (0–100) for a display.</summary>
    public async Task<int> GetBrightnessAsync(string displayId)
    {
        var (cur, _) = await GetVcpAsync(displayId, 0x10);
        return cur;
    }

    /// <summary>Gets the current contrast (0–100) for a display.</summary>
    public async Task<int> GetContrastAsync(string displayId)
    {
        var (cur, _) = await GetVcpAsync(displayId, 0x12);
        return cur;
    }

    /// <summary>Gets current and max values for a VCP feature.</summary>
    private async Task<(int Current, int Max)> GetVcpAsync(string displayId, int vcpCode)
    {
        string args = $"getvcp {vcpCode:X2} --display {displayId} --terse";
        var result = await ProcessRunner.RunAsync(Tool, args);

        if (!result.Success) return (50, 100);

        var match = VcpValueRegex().Match(result.Output);
        if (!match.Success) return (50, 100);

        int cur = int.Parse(match.Groups[2].Value);
        int max = int.Parse(match.Groups[3].Value);

        // Normalize to 0–100
        if (max <= 0) return (cur, 100);
        return (cur * 100 / max, max);
    }

    /// <summary>Sets brightness (0–100) for a display.</summary>
    public async Task SetBrightnessAsync(string displayId, int value)
    {
        await SetVcpAsync(displayId, 0x10, value);
    }

    /// <summary>Sets contrast (0–100) for a display.</summary>
    public async Task SetContrastAsync(string displayId, int value)
    {
        await SetVcpAsync(displayId, 0x12, value);
    }

    private static async Task SetVcpAsync(string displayId, int vcpCode, int value)
    {
        int clamped = Math.Clamp(value, 0, 100);
        string args = $"setvcp {vcpCode:X2} {clamped} --display {displayId}";
        await ProcessRunner.RunAsync(Tool, args);
    }
}
