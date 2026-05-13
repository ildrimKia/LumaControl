using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LumaControl.ViewModels;
using LumaControl.Views;

namespace LumaControl;

public partial class App : Application
{
    private PopupWindow? _popup;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // No main window — tray-only app
            desktop.MainWindow = null;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Wire up the tray icon click
            var icons = TrayIcon.GetIcons(this);
            if (icons is { Count: > 0 })
            {
                icons[0].Clicked += OnTrayIconClicked;
                icons[0].Menu = BuildTrayMenu();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (_popup is null)
        {
            var vm = new MainViewModel();
            _popup = new PopupWindow { DataContext = vm };
            _ = vm.LoadDisplaysAsync();
        }

        if (_popup.IsVisible)
        {
            _popup.Hide();
            return;
        }

        PositionPopupAtTray(_popup);
        _popup.Show();
        _popup.Activate();
    }

    private static void PositionPopupAtTray(Window window)
    {
        var screen = window.Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        // Estimate window size before it is measured; actual size set once visible
        const int estimatedW = 340;
        const int estimatedH = 500;

        int x = workArea.X + workArea.Width - estimatedW - 8;
        int y = workArea.Y + workArea.Height - estimatedH - 8;

        window.Position = new PixelPoint(x, y);
    }

    private NativeMenu BuildTrayMenu()
    {
        var menu = new NativeMenu();

        // "Open" is the primary action — on many Linux DEs left-click doesn't
        // fire TrayIcon.Clicked, so this menu item is the reliable entry point.
        var openItem = new NativeMenuItem("Open LumaControl");
        openItem.Click += (_, _) => OnTrayIconClicked(null, EventArgs.Empty);
        menu.Add(openItem);

        menu.Add(new NativeMenuItemSeparator());

        var refreshItem = new NativeMenuItem("Refresh displays");
        refreshItem.Click += (_, _) =>
        {
            if (_popup?.DataContext is MainViewModel vm)
                _ = vm.LoadDisplaysAsync();
            // Also open the window so the user can see the refreshed values
            OnTrayIconClicked(null, EventArgs.Empty);
        };
        menu.Add(refreshItem);

        menu.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        };
        menu.Add(quitItem);

        return menu;
    }
}
