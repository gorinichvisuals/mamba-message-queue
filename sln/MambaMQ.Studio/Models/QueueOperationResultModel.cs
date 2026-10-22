namespace MambaMQ.Studio.Models;

public sealed record QueueOperationResultModel(
    bool IsSucceed,
    string? ErrorMessage = null,
    QueueModel? Queue = null);