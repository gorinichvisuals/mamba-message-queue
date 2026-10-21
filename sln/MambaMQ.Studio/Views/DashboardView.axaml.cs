using Avalonia.Threading;

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
        if (e.PropertyName is nameof(DashboardViewModel.IsConnected) or nameof(DashboardViewModel.IsReconnecting))
            UpdateConnectionVisuals();
    }

    private void UpdateConnectionVisuals()
    {
        bool isConnectionLost = _observedViewModel?.IsConnectionLost ?? false;

        DashboardContent.Effect = isConnectionLost
            ? new BlurEffect { Radius = 3 }
            : null;

        if (isConnectionLost)
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
