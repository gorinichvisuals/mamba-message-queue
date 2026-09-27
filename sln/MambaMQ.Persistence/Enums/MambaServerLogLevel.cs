namespace MambaMQ.Persistence.Enums;

[Flags]
public enum MambaServerLogLevel
{
    None = 0,
    Errors = 1 << 0,
    Warnings = 1 << 1,
    Info = 1 << 2,
    All = Errors | Warnings | Info
}