namespace MambaMQ.Studio.ViewModels;

public partial class QueuesViewModel(
    IMambaStudioClient mambaClient,
    Action<QueueModel> onQueueSelected) : ViewModelBase
{
    private readonly Guid _instanceId = Guid.NewGuid();

    [ObservableProperty]
    private ObservableCollection<QueueModel> _queues = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    partial void OnQueuesChanged(ObservableCollection<QueueModel> value)
    {
        Console.WriteLine(
            $"QueuesViewModel {_instanceId}: Queues changed, count = {value.Count}");
    }
    
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

public sealed record QueueOperationResult(
    bool IsSucceed,
    string? ErrorMessage = null,
    QueueModel? Queue = null);