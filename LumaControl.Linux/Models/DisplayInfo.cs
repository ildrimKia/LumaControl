namespace LumaControl.Models;

public enum DisplayType
{
    Ddc,       // External monitor via DDC/CI (ddcutil)
    Backlight  // Laptop internal panel (brightnessctl)
}

public sealed record DisplayInfo(
    string Id,
    string Name,
    DisplayType DisplayType,
    bool HasContrast);
