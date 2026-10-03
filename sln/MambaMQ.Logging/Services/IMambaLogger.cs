namespace MambaMQ.Logging.Services;

public interface IMambaLogger
{
    ILogger Server { get; }

    ILogger Queue(string queueName);
    ILogger Exchange(string exchangeName);
}