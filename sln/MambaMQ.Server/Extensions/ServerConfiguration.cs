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
            options.QueueStorage.MaxMessageSegments);
        
        services.AddServerLogging(options.ServerLogging);
        services.AddAuthentication();
        services.AddCoreExtensions();
        
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