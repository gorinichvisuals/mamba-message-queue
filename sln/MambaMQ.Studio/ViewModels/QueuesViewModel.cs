namespace MambaMQ.Studio.ViewModels;

public partial class QueuesViewModel(
    IMambaStudioClient mambaClient,
    Action<QueueModel> onQueueSelected) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<QueueModel> _queues = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _queueCount;

    public string QueuesTitle => QueueCount switch
    {
        0 => "Queues",
        1 => "1 Queue",
        _ => $"{QueueCount} Queues"
    };

    partial void OnQueueCountChanged(int value)
    {
        OnPropertyChanged(nameof(QueuesTitle));
    }

    [ObservableProperty]
    private string? _errorMessage;
    
    [RelayCommand]
    private void OpenQueue(QueueModel queue)
    {
        onQueueSelected(queue);
    }

    [RelayCommand]
    private async Task CreateQueue(QueueOptions options)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse<QueueModel> response = await mambaClient.CreateQueueAsync(options);

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                
                return;
            }
            
            Queues.Insert(0, response.Data!);
            QueueCount = Queues.Count;
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

            GetQueuesResponse response = await mambaClient.GetQueuesAsync();

            Queues = new ObservableCollection<QueueModel>(response.Queues);
            QueueCount = Queues.Count;
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