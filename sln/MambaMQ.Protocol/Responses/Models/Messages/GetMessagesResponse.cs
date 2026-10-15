namespace MambaMQ.Protocol.Responses.Models.Messages;

public sealed class GetMessagesResponse
{
    public required IReadOnlyList<QueueMessageModel> Messages { get; init; } = [];
    public required int TotalCount { get; init; }
}