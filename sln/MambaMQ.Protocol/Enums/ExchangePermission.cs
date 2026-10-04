namespace MambaMQ.Protocol.Enums;

[Flags]
public enum ExchangePermission : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4
}