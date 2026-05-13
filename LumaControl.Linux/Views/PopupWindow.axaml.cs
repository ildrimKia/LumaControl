using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace LumaControl.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Hide window when it loses focus (light-dismiss behaviour)
        Deactivated += (_, _) => Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Left-click drag anywhere that isn't a slider or button moves the window.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        // Don't steal clicks from interactive controls
        if (e.Source is Button or Slider or Thumb or Track or RepeatButton or ScrollBar) return;

        BeginMoveDrag(e);
    }

    /// <summary>
    /// Re-positions the window after it becomes visible so we can use the actual
    /// measured size rather than the estimate used before Show().
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RepositionNearTray();
    }

    private void RepositionNearTray()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        var sBounds  = screen.Bounds;
        var workArea = screen.WorkingArea;
        int w = (int)Width;
        int h = (int)Bounds.Height;
        if (h < 20) h = 420; // fallback before first layout pass

        // Right-align near the system tray
        int x = workArea.X + workArea.Width - w - 8;

        // Detect panel position:
        //   workArea.Y > sBounds.Y  → panel is at the TOP  (Ubuntu GNOME default)
        //   otherwise               → panel is at the BOTTOM (KDE, XFCE, etc.)
        bool topPanel = workArea.Y > sBounds.Y;
        int y = topPanel
            ? workArea.Y + 4                            // just below the top bar
            : workArea.Y + workArea.Height - h - 8;     // just above the bottom bar

        Position = new PixelPoint(x, y);
    }
}
