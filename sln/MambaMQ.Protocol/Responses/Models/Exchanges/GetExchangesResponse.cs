namespace MambaMQ.Protocol.Responses.Models.Exchanges;

public sealed class GetExchangesResponse
{
    public IReadOnlyList<ExchangeModel> Exchanges { get; init; } = [];
}