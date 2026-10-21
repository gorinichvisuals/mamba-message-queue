namespace MambaMQ.Studio.ViewModels;

public partial class ExchangesViewModel(
    IMambaStudioClient mambaClient,
    Action<ExchangeModel> onExchangeSelected) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ExchangeModel> _exchanges = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _exchangeCount;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ExchangesTitle => ExchangeCount switch
    {
        0 => "Exchanges",
        1 => "1 Exchange",
        _ => $"{ExchangeCount} Exchanges"
    };

    partial void OnExchangeCountChanged(int value)
    {
        OnPropertyChanged(nameof(ExchangesTitle));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private void OpenExchange(ExchangeModel exchange)
    {
        onExchangeSelected(exchange);
    }

    [RelayCommand]
    private async Task CreateExchange(ExchangeOptions options)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse response = await mambaClient.CreateExchangeAsync(options);
            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                return;
            }

            await LoadAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            GetExchangesResponse response = await mambaClient.GetExchangesAsync();

            Exchanges = new ObservableCollection<ExchangeModel>(response.Exchanges);
            ExchangeCount = Exchanges.Count;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}