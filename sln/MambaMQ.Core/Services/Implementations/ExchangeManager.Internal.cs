namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class ExchangeManager
{
    private async Task<CommandResponse> PersistExchange(MambaExchange exchange, CancellationToken cancellationToken = default)
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

            return CommandResponse.Success();
        }
        catch (Exception exception)
        {
            mambaLogger.Exchange(exchange.Name).LogError(exception, "Failed to persist exchange.");

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to persist exchange.");
        }
    }
}