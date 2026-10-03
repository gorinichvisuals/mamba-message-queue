namespace MambaMQ.Protocol.Responses;

public class CommandResponse
{
    public bool IsSucceed { get; }
    public ErrorCode ErrorCode { get; }
    public string? ErrorMessage { get; }

    internal CommandResponse(bool isSucceed , ErrorCode errorCode, string? errorMessage)
    {
        IsSucceed = isSucceed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static CommandResponse Success()
        => new(true, ErrorCode.None, null);

    public static CommandResponse Fail(ErrorCode errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}

public sealed class CommandResponse<T> : CommandResponse
{
    public T? Data { get; }

    private CommandResponse(bool isSucceed, ErrorCode errorCode, string? errorMessage, T? data)
        : base(isSucceed, errorCode, errorMessage)
    {
        Data = data;
    }

    public static CommandResponse<T> Success(T data)
        => new(true, ErrorCode.None, null, data);

    public new static CommandResponse<T> Fail(ErrorCode errorCode, string errorMessage)
        => new(false, errorCode, errorMessage, default);
}