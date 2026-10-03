namespace MambaMQ.Client.NET.Options;

public class ExchangeOptions
{
    public required string Name { get; set; }
    public bool IsDurable { get; set; } = true;
    public ExchangeType Type { get; set; } = ExchangeType.Direct;
}