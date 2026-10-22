namespace MambaMQ.Studio.Views;

public partial class WindowChromeButtons : UserControl
{
    public WindowChromeButtons()
    {
        InitializeComponent();
    }

    private Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is { } window)
            window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is not { } window)
            return;

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => OwnerWindow?.Close();
}