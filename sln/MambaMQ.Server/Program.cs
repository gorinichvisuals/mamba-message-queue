HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MambaServerOptions>(builder.Configuration.GetSection(nameof(MambaServerOptions)));
MambaServerOptions options = builder.Configuration.GetSection(nameof(MambaServerOptions)).Get<MambaServerOptions>()!;

builder.Services.ConfigurePersistence(
    options.Storage.Path, 
    options.Storage.MessageSegmentSizeInBytes,
    options.Storage.MaxMessageSegments, 
    options.Storage.LogSegmentSizeInBytes);

builder.Services.AddSingleton<IQueueManager, QueueManager>();
builder.Services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddSingleton<IQueueRecoveryService, QueueRecoveryService>();
builder.Services.AddSingleton<IQueueLogService, QueueLogService>();

builder.Services.AddSingleton<ICommandHandler<CreateQueueCommand>, CreateQueueCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<PublishMessageCommand>, PublishMessageCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<SubscribeQueueCommand>, SubscribeQueueCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<DeleteMessageCommand>, DeleteMessageCommandHandler>();

builder.Services.AddSingleton<MambaServer>();

using IHost host = builder.Build();

MambaServer server = host.Services.GetRequiredService<MambaServer>();

await server.StartAsync();