using Avalonia.Threading;
using MambaMQ.Studio.Controls;

namespace MambaMQ.Studio.Views;

public partial class DashboardView : UserControl
{
    private readonly DispatcherTimer _spinnerTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(110)
    };

    private DashboardViewModel? _observedViewModel;

    public DashboardView()
    {
        InitializeComponent();

        _spinnerTimer.Tick += OnSpinnerTimerTick;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnWindowDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control dragRegion)
            WindowDragHelper.TryBeginMoveDrag(dragRegion, e);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_observedViewModel is not null)
            _observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _observedViewModel = DataContext as DashboardViewModel;

        if (_observedViewModel is not null)
            _observedViewModel.PropertyChanged += OnViewModelPropertyChanged;

        UpdateConnectionVisuals();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardViewModel.IsNavigationLoading)
            or nameof(DashboardViewModel.HasNavigationError)
            or nameof(DashboardViewModel.IsNavigationOverlayVisible))
            UpdateConnectionVisuals();
    }

    private void UpdateConnectionVisuals()
    {
        bool showOverlay = _observedViewModel?.IsNavigationOverlayVisible ?? false;
        bool isLoading = _observedViewModel?.IsNavigationLoading ?? false;

        DashboardContent.Effect = showOverlay
            ? new BlurEffect { Radius = 3 }
            : null;

        if (isLoading)
        {
            ReconnectSpinner.Step = 0;
            _spinnerTimer.Start();
        }
        else
        {
            _spinnerTimer.Stop();
            ReconnectSpinner.Step = 0;
        }
    }

    private void OnSpinnerTimerTick(object? sender, EventArgs e)
    {
        ReconnectSpinner.Step = (ReconnectSpinner.Step + 1) % 8;
    }
}
