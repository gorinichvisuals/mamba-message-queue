namespace MambaMQ.Studio.Controls;

internal static class WindowDragHelper
{
    public static void TryBeginMoveDrag(Control dragRegion, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(dragRegion).Properties.IsLeftButtonPressed)
            return;

        Control? source = e.Source as Control;
        
        while (source is not null && source != dragRegion)
        {
            if (source is Button or TextBox or ListBox or ComboBox or ScrollBar)
                return;

            source = source.GetVisualParent() as Control;
        }

        if (TopLevel.GetTopLevel(dragRegion) is Window window)
            window.BeginMoveDrag(e);
    }
}