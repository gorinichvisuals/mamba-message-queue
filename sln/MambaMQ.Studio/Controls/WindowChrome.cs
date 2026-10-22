namespace MambaMQ.Studio.Controls;

internal static class WindowChrome
{
    public static void Apply(Window window)
    {
        if (window.Content is not Control content)
            return;

        window.WindowDecorations = WindowDecorations.None;
        window.Content = null;

        Grid root = new();
        
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        Grid titleBar = new();
        
        titleBar.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#181B202B")),
            BorderBrush = new SolidColorBrush(Color.Parse("#30FFFFFF")),
            BorderThickness = new Thickness(0, 0, 0, 1)
        });
        
        titleBar.Children.Add(new WindowChromeButtons
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        });
        
        titleBar.PointerPressed += (_, e) => WindowDragHelper.TryBeginMoveDrag(titleBar, e);

        Grid.SetRow(content, 1);
        
        root.Children.Add(titleBar);
        root.Children.Add(content);
        
        window.Content = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#FF4B5668")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Child = root
        };
    }
}