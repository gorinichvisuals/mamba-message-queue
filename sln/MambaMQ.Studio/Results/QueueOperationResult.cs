namespace MambaMQ.Studio.Results;

public sealed record QueueOperationResult(
    bool IsSucceed,
    string? ErrorMessage = null,
    QueueModel? Queue = null);