HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.ConfigureServerServices(builder.Configuration);

using IHost host = builder.Build();

MambaServer server = host.Services.GetRequiredService<MambaServer>();

await server.Start();