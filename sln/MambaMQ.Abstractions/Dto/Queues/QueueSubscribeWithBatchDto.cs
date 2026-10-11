namespace MambaMQ.Abstractions.Dto.Queues;

public sealed record QueueSubscribeWithBatchDto(
    string QueueName,
    int MaxMessages,
    int MaxBytes,
    TimeSpan MaxWaitTime,
    int Weight);