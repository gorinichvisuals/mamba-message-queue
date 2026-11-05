namespace MambaMQ.Server.Extensions;

public static class ServerConfiguration
{
    public static void ConfigureServerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MambaServerOptions>(configuration.GetSection(nameof(MambaServerOptions)));
        services.Configure<AuthenticationOptions>(configuration.GetSection(nameof(AuthenticationOptions)));
        services.Configure<QueueStorageOptions>(configuration.GetSection(nameof(QueueStorageOptions)));
        services.Configure<ServerLoggingOptions>(configuration.GetSection(nameof(ServerLoggingOptions)));
        
        MambaServerOptions serverOptions = configuration.GetSection(nameof(MambaServerOptions)).Get<MambaServerOptions>()!;
        QueueStorageOptions queueStorageOptions = configuration.GetSection(nameof(QueueStorageOptions)).Get<QueueStorageOptions>()!;
        ServerLoggingOptions serverLoggingOptions = configuration.GetSection(nameof(ServerLoggingOptions)).Get<ServerLoggingOptions>()!;
        
        services.ConfigurePersistence(
            queueStorageOptions.Path,
            queueStorageOptions.MessageSegmentSizeInBytes,
            queueStorageOptions.MaxMessageSegments);
        
        services.AddServerLogging(serverLoggingOptions);
        services.AddAuthentication();
        services.AddCoreExtensions(serverOptions.AuthorizationEnabled);
        
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueueRecoveryService, QueueRecoveryService>();

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