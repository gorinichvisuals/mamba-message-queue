
namespace MambaMQ.Server.Logging.Implementations;

internal sealed class ServerFileLoggerProvider(IServerStorageService storage) : ILoggerProvider
{    
    public ILogger CreateLogger(string categoryName)
        => new ServerFileLogger(storage);

    public void Dispose()
    {
    }
}