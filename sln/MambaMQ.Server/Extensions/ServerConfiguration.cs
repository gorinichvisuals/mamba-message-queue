namespace MambaMQ.Server.Extensions;

public static class ServerConfiguration
{
    public static void ConfigureServerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MambaServerOptions>(configuration.GetSection(nameof(MambaServerOptions)));

        MambaServerOptions options = configuration.GetSection(nameof(MambaServerOptions)).Get<MambaServerOptions>()!;

        services.ConfigurePersistence(
            options.QueueStorage.Path,
            options.QueueStorage.MessageSegmentSizeInBytes,
            options.QueueStorage.MaxMessageSegments,
            options.QueueStorage.LogSegmentSizeInBytes,
            options.ServerLogging.SegmentSizeInBytes);

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(options.ServerLogging.MinimumLevel);

            if (options.ServerLogging.ConsoleEnabled)
                logging.AddConsole();

            if (options.ServerLogging.FileEnabled)
                logging.Services.AddSingleton<
                    ILoggerProvider,
                    ServerFileLoggerProvider>();
        });

        services.AddSingleton<IQueueLogger, QueueLogger>();
        services.AddSingleton<IQueueManager, QueueManager>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueueRecoveryService, QueueRecoveryService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();

        AddCommandHandlers(services);

        services.AddSingleton<MambaServer>();
    }

    private static void AddCommandHandlers(IServiceCollection services)
    {
        Assembly assembly = typeof(ICommandHandler<>).Assembly;

        IEnumerable<Type> handlerTypes = assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.GetInterfaces().Any(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() ==
                    typeof(ICommandHandler<>)));

        foreach (Type handlerType in handlerTypes)
        {
            Type handlerInterface = handlerType
                .GetInterfaces()
                .Single(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() ==
                    typeof(ICommandHandler<>));

            services.AddSingleton(handlerInterface, handlerType);
        }
    }
}