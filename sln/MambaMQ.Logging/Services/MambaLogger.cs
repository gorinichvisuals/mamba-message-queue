namespace MambaMQ.Logging.Services;

internal sealed class MambaLogger(ILoggerFactory loggerFactory) : IMambaLogger
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Mamba");

    public ILogger Server
        => new MambaSourceLogger(_logger, "Server");

    public ILogger Queue(string queueName)
        => new MambaSourceLogger(_logger, "Queue", queueName);

    public ILogger Exchange(string exchangeName)
        => new MambaSourceLogger(_logger, "Exchange", exchangeName);
}