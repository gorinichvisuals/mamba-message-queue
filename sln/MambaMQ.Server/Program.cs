HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MambaServerOptions>(
    builder.Configuration.GetSection(nameof(MambaServerOptions)));

builder.Services.AddSingleton<IQueueManager, QueueManager>();
builder.Services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

builder.Services.AddSingleton<ICommandHandler<PublishMessageCommand>, PublishMessageCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<SubscribeQueueCommand>, SubscribeQueueCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<DeleteMessageCommand>, DeleteMessageCommandHandler>();

builder.Services.AddSingleton<MambaServer>();

builder.Services.AddHostedService<MessageExpirationWorker>();

IHost app = builder.Build();

MambaServer server = app.Services.GetRequiredService<MambaServer>();

await server.StartAsync();