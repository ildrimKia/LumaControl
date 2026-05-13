# LumaControl

A Linux system tray application for controlling display brightness and contrast. Supports external DDC/CI monitors via `ddcutil` and laptop backlight via `brightnessctl`.

## Features

- Control brightness and contrast on external DDC/CI monitors
- Control brightness on laptop internal backlight
- System tray icon with popup panel for quick access
- Auto-detects connected displays
- Debounced slider updates to avoid flooding hardware commands

## Requirements

- .NET 10.0 SDK
- [`ddcutil`](https://www.ddcutil.com/) — for external monitor control
- [`brightnessctl`](https://github.com/Hummer12007/brightnessctl) — for laptop backlight control

## Installation

### 1. Install dependencies

```bash
sudo apt install ddcutil brightnessctl
```

### 2. Grant DDC/CI access

External monitor control requires access to the i2c bus:

```bash
sudo usermod $USER -aG i2c
```

Log out and back in for the group change to take effect.

### 3. Build and run

```bash
git clone <repo-url>
cd LumaControl

dotnet run --project LumaControl.Linux/LumaControl.Linux.csproj
```

### Build a release binary

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

The output binary will be in `LumaControl.Linux/bin/Release/net10.0/linux-x64/publish/`.

## Usage

After launching, LumaControl appears as a tray icon. Click it to open the control panel.

- **Brightness / Contrast sliders** — adjust values per display
- **Refresh displays** — re-detect connected monitors
- **Quit** — exit the application

Contrast control is only available for DDC/CI monitors, not for laptop backlight.

## Tech Stack

- C# 13 / .NET 10.0
- [Avalonia UI](https://avaloniaui.net/) 11 — cross-platform desktop UI
- CommunityToolkit.Mvvm — MVVM bindings
- `ddcutil` / `brightnessctl` — external CLI tools for hardware control

## Project Structure

```
LumaControl.Linux/
├── Models/          # DisplayInfo record
├── Services/        # ddcutil, brightnessctl, and process runner wrappers
├── ViewModels/      # MainViewModel (display list), DisplayViewModel (per-display controls)
└── Views/           # PopupWindow UI
```
