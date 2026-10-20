namespace MambaMQ.Studio.Models;

public sealed record PublishToExchangeOptions(string RoutingKey, string Message);
