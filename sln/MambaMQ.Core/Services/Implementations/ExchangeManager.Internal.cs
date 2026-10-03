namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager
{
    private async Task PersistExchange(MambaExchange exchange, CancellationToken cancellationToken = default)
    {
        StoredMambaExchange storedExchange = new(
            exchange.Id,
            exchange.Name,
            exchange.IsDurable,
            (byte)exchange.Type,
            exchange.Bindings
                .Select(binding => new StoredExchangeBinding(
                    binding.QueueName,
                    binding.RoutingKey))
                .ToList());

        try
        {
            await serverStorageService.SaveExchange(storedExchange, cancellationToken);
        }
        catch (Exception exception)
        {
            mambaLogger.Exchange(exchange.Name).LogError(exception, "Failed to persist exchange.");

            throw;
        }
    }
    
    private MambaExchange GetRequiredExchange(string name)
    {
        return _exchangesByName.TryGetValue(name, out MambaExchange? exchange) 
            ? exchange 
            : throw new InvalidOperationException($"Exchange '{name}' does not exist.");
    }
}