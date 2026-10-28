namespace MambaMQ.Studio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnWindowDragPointerPressed(object? sender, PointerPressedEventArgs e)
        => WindowDragHelper.TryBeginMoveDrag(this, e);

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

        UpdateOverlayVisuals();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsConnectOverlayOpen)
            or nameof(MainViewModel.ActiveDashboard)
            or nameof(MainViewModel.HasActiveDashboard))
            UpdateOverlayVisuals();
    }

    private void UpdateOverlayVisuals()
    {
        bool shouldBlur = DataContext is MainViewModel { IsConnectOverlayOpen: true, HasActiveDashboard: true };
        DashboardHost.Effect = shouldBlur ? new BlurEffect { Radius = 8 } : null;

        if (DataContext is MainViewModel { HasActiveDashboard: false })
            SettingsButton.Flyout?.Hide();
    }
}