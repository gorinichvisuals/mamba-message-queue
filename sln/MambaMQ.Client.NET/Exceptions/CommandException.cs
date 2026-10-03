namespace MambaMQ.Client.NET.Exceptions;

public sealed class CommandException(
    ErrorCode errorCode, 
    string message) : Exception(message)
{
    public ErrorCode ErrorCode { get; } = errorCode;
}