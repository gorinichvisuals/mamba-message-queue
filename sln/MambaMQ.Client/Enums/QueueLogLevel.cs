namespace MambaMQ.Client.Enums;

[Flags]
public enum QueueLogLevel
{
    None = 0,
    Errors = 1 << 0,
    Warnings = 1 << 1,
    Info = 1 << 2,
    All = Errors | Warnings | Info
}