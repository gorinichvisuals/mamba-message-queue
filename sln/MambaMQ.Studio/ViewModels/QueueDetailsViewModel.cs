namespace MambaMQ.Studio.ViewModels;

public partial class QueueDetailsViewModel(
    IMambaStudioClient mambaClient,
    QueueModel queue,
    Action onBack) : ViewModelBase
{
    [ObservableProperty]
    private QueueModel _queue = queue;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private void GoBack()
    {
        onBack();
    }

    public async Task<QueueOperationResult> UpdateQueue(QueueOptions options)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse<QueueModel> response =
                await mambaClient.UpdateQueueAsync(Queue.Id, options);

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                return new QueueOperationResult(false, response.ErrorMessage);
            }

            Queue = response.Data!;

            return new QueueOperationResult(true, Queue: Queue);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            
            return new QueueOperationResult(false, exception.Message);
        }
    }

    public async Task<QueueOperationResult> DeleteQueue()
    {
        try
        {
            ErrorMessage = null;

            CommandResponse response =
                await mambaClient.DeleteQueueAsync(Queue.Id);

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;
                return new QueueOperationResult(false, response.ErrorMessage);
            }

            return new QueueOperationResult(true);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            
            return new QueueOperationResult(false, exception.Message);
        }
    }
}