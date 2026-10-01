namespace MambaMQ.Logging.Extensions;

public static class LoggingExtensions
{
    public static void AddServerLogging(this IServiceCollection services, ServerLoggingOptions serverLoggingOptions)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(serverLoggingOptions.MinimumLevel);

            if (!serverLoggingOptions.ConsoleEnabled) 
                return;
            
            logging.AddConsole(options =>
            {
                options.FormatterName = "Mamba";
            });
            
            logging.AddConsoleFormatter<MambaConsoleFormatter, ConsoleFormatterOptions>();
        });
        
        services.AddSingleton<IMambaLogger, MambaLogger>();
    }
}