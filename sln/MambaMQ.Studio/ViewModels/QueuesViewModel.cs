namespace MambaMQ.Studio.ViewModels;

public partial class QueuesViewModel(IMambaStudioClient mambaClient) : ViewModelBase
{
    private readonly Guid _instanceId = Guid.NewGuid();

    [ObservableProperty]
    private ObservableCollection<QueueModel> _queues = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public QueuesViewModel()
        : this(null!)
    {
    }

    partial void OnQueuesChanged(ObservableCollection<QueueModel> value)
    {
        Console.WriteLine(
            $"QueuesViewModel {_instanceId}: Queues changed, count = {value.Count}");
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

    public async Task UpdateQueue(QueueModel queue, QueueOptions options)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse<QueueModel> response = await mambaClient.UpdateQueueAsync(queue.Id, options);

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                
                return;
            }

            QueueModel updatedQueue = response.Data!;

            Queues = new ObservableCollection<QueueModel>(
                Queues.Select(x => x.Id == updatedQueue.Id ? updatedQueue : x));
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteQueue(QueueModel queue)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse response = await mambaClient.DeleteQueueAsync(queue.Id);

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                return;
            }

            Queues.Remove(queue);
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