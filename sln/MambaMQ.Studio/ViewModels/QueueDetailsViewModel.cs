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

    [ObservableProperty]
    private ObservableCollection<MessageListItemModel> _messages = [];

    [ObservableProperty]
    private bool _isLoadingMessages;

    [ObservableProperty]
    private bool _hasMessages;

    [ObservableProperty]
    private string? _messagesError;

    public bool HasNoMessages => !IsLoadingMessages && string.IsNullOrEmpty(MessagesError) && !HasMessages;

    public bool HasMessagesError => !string.IsNullOrEmpty(MessagesError);
    
    [ObservableProperty]
    private bool _hasSelectedMessages;

    [ObservableProperty]
    private bool _isDeletingMessages;

    private ObservableCollection<MessageListItemModel>? _observedMessages;

    [RelayCommand]
    private void GoBack()
    {
        onBack();
    }

    [RelayCommand]
    private async Task RefreshMessages()
    {
        await LoadMessagesAsync();
    }

    partial void OnMessagesChanged(ObservableCollection<MessageListItemModel> value)
    {
        if (_observedMessages is not null)
            foreach (MessageListItemModel item in _observedMessages)
                item.PropertyChanged -= OnMessagePropertyChanged;

        _observedMessages = value;

        foreach (MessageListItemModel item in value)
            item.PropertyChanged += OnMessagePropertyChanged;

        HasMessages = value.Count > 0;
        HasSelectedMessages = value.Any(item => item.IsSelected);

        OnPropertyChanged(nameof(HasNoMessages));
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MessageListItemModel.IsSelected))
            return;

        HasSelectedMessages = Messages.Any(item => item.IsSelected);
    }

    partial void OnHasMessagesChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMessages));
    }

    partial void OnIsLoadingMessagesChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMessages));
    }

    partial void OnMessagesErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasNoMessages));
        OnPropertyChanged(nameof(HasMessagesError));
    }

    public async Task LoadMessagesAsync()
    {
        try
        {
            IsLoadingMessages = true;
            MessagesError = null;

            CommandResponse<GetMessagesResponse> response = await mambaClient.GetMessagesAsync(Queue.Name, skip: 0, take: 20);

            if (!response.IsSucceed || response.Data is null)
            {
                MessagesError = response.ErrorMessage ?? "Failed to load messages.";

                return;
            }

            Messages = new ObservableCollection<MessageListItemModel>(
                response.Data.Messages.Select(message => new MessageListItemModel
                {
                    Message = message
                }));
        }
        catch (Exception exception)
        {
            MessagesError = exception.Message;
        }
        finally
        {
            IsLoadingMessages = false;
        }
    }

    public async Task<QueueOperationResult> UpdateQueue(QueueOptions options)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse<QueueModel> response = await mambaClient.UpdateQueueAsync(Queue.Id, options);

            if (!response.IsSucceed || response.Data is null)
            {
                ErrorMessage = response.ErrorMessage;

                return new QueueOperationResult(false, response.ErrorMessage);
            }

            Queue = response.Data;

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

            CommandResponse response = await mambaClient.DeleteQueueAsync(Queue.Id);

            if (response.IsSucceed)
                return new QueueOperationResult(true);

            ErrorMessage = response.ErrorMessage;

            return new QueueOperationResult(false, response.ErrorMessage);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;

            return new QueueOperationResult(false, exception.Message);
        }
    }

    public async Task<QueueOperationResult> PublishMessage(string message)
    {
        try
        {
            ErrorMessage = null;

            CommandResponse response = await mambaClient.PublishMessageToQueueAsync(Queue.Name, Encoding.UTF8.GetBytes(message));

            if (!response.IsSucceed)
            {
                ErrorMessage = response.ErrorMessage;

                return new QueueOperationResult(false, response.ErrorMessage);
            }

            try
            {
                GetQueuesResponse queuesResponse = await mambaClient.GetQueuesAsync();

                QueueModel? updatedQueue = queuesResponse.Queues.FirstOrDefault(item => item.Id == Queue.Id);

                if (updatedQueue is not null)
                    Queue = updatedQueue;
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Message published, but queue statistics could not be refreshed: {exception.Message}";
            }

            await LoadMessagesAsync();

            return new QueueOperationResult(true);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;

            return new QueueOperationResult(false, exception.Message);
        }
    }
    
    [RelayCommand]
    private async Task DeleteSelectedMessages()
    {
        Guid[] messageIds = Messages
            .Where(item => item.IsSelected)
            .Select(item => item.MessageId)
            .ToArray();

        if (messageIds.Length is 0 || IsDeletingMessages)
            return;

        try
        {
            IsDeletingMessages = true;
            MessagesError = null;

            CommandResponse response = await mambaClient.DeleteMessagesAsync(Queue.Name, messageIds);

            if (!response.IsSucceed)
            {
                MessagesError = response.ErrorMessage ?? "Failed to delete messages.";
                
                return;
            }

            await LoadMessagesAsync();

            GetQueuesResponse queuesResponse = await mambaClient.GetQueuesAsync();

            QueueModel? updatedQueue = queuesResponse.Queues.FirstOrDefault(item => item.Id == Queue.Id);

            if (updatedQueue is not null)
                Queue = updatedQueue;
        }
        catch (Exception exception)
        {
            MessagesError = exception.Message;
        }
        finally
        {
            IsDeletingMessages = false;
        }
    }
}