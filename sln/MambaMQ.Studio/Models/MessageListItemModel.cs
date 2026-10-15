namespace MambaMQ.Studio.Models;

public partial class MessageListItemModel : ObservableObject
{
    public required QueueMessageModel Message { get; init; }

    public Guid MessageId => Message.MessageId;

    public int SizeInBytes => Message.SizeInBytes;

    public DateTimeOffset ReceivedAt => Message.ReceivedAt;

    [ObservableProperty]
    private bool _isSelected;
}