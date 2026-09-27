HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MambaServerOptions>(builder.Configuration.GetSection(nameof(MambaServerOptions)));
MambaServerOptions options = builder.Configuration.GetSection(nameof(MambaServerOptions)).Get<MambaServerOptions>()!;

builder.Services.ConfigurePersistence(
    options.QueueStorage.Path, 
    options.QueueStorage.MessageSegmentSizeInBytes,
    options.QueueStorage.MaxMessageSegments, 
    options.QueueStorage.LogSegmentSizeInBytes,
    options.ServerLogging.SegmentSizeInBytes);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(options.ServerLogging.MinimumLevel);
    
    if (options.ServerLogging.ConsoleEnabled)
        logging.AddConsole();

    if (options.ServerLogging.FileEnabled)
        logging.Services.AddSingleton<ILoggerProvider, ServerFileLoggerProvider>();
});

builder.Services.AddSingleton<IQueueManager, QueueManager>();
builder.Services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddSingleton<IQueueRecoveryService, QueueRecoveryService>();
builder.Services.AddSingleton<IQueueLogger, QueueLogger>();

builder.Services.AddSingleton<ICommandHandler<CreateQueueCommand>, CreateQueueCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<PublishMessageCommand>, PublishMessageCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<SubscribeQueueCommand>, SubscribeQueueCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<DeleteMessageCommand>, DeleteMessageCommandHandler>();

builder.Services.AddSingleton<MambaServer>();

using IHost host = builder.Build();

MambaServer server = host.Services.GetRequiredService<MambaServer>();

await server.Start();