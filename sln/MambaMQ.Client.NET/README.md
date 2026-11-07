# MambaMQ.Client.NET

The .NET client for connecting to a MambaMQ server. Register it with dependency injection using `AddMamba`, then inject `IMamba` to manage exchanges and queues, publish messages, and subscribe to queues.

## Install

```bash
dotnet add package MambaMQ.Client.NET
```

## Configure

```csharp
using MambaMQ.Client.NET;
using MambaMQ.Client.NET.Configure;

services.AddMamba(options =>
{
    options.Host = "localhost";
    options.Port = 24;
    options.ServiceName = "orders-service";
    options.Credentials.Username = Environment.GetEnvironmentVariable("MAMBA_USERNAME")!;
    options.Credentials.Password = Environment.GetEnvironmentVariable("MAMBA_PASSWORD")!;
});
```

The credentials must match a user configured on the server. Keep them in a secret store or environment variables rather than source code.

## Use `IMamba`

Inject `IMamba` into an application service. The client serializes published objects as UTF-8 JSON.

```csharp
public sealed class OrderPublisher(IMamba mamba)
{
    public Task PublishAsync(OrderCreated message, CancellationToken cancellationToken) =>
        mamba.PublishToQueueAsync("orders", message, cancellationToken);
}
```

The interface provides these operations:

- **Exchanges:** create, list, update, delete, bind or unbind queues, and publish with a routing key.
- **Queues:** create, list, update, and delete.
- **Messages:** publish to a queue, subscribe one at a time or in batches, read a page, and delete one or many.

Each method is documented in the IDE through XML documentation included with the package.

Create and update operations use separate option types. Update types expose only settings that can be changed:

```csharp
await mamba.UpdateQueueAsync(queueId, new QueueUpdateOptions
{
    QueueName = "orders",
    LoadBalancingAlgorithm = LoadBalancingAlgorithm.RoundRobin,
    Permissions = permissions,
    MessageRetentionPeriod = TimeSpan.FromMinutes(15)
}, cancellationToken);

await mamba.UpdateExchangeAsync(exchangeId, new ExchangeUpdateOptions
{
    Name = "orders",
    Type = ExchangeType.Direct,
    Permissions = exchangePermissions
}, cancellationToken);
```

Queue update can change the retention period, but cannot change durability or whether message persistence is enabled. The period controls cleanup timing for storage segments whose messages have already been deleted; it is not a message expiration time.

## Options reference

### `MambaClientOptions`

| Property | Description |
| --- | --- |
| `ServiceName` | Name identifying this service client to the server. |
| `Host` | Server host name or IP address. |
| `Port` | Server TCP port. Defaults to `24`. |
| `MaxMessageSizeInBytes` | Maximum accepted frame size. Defaults to `1,048,576` bytes (1 MiB). |
| `Credentials` | Authentication settings for the connection. |

### `AuthenticationOptions`

| Property | Description |
| --- | --- |
| `Username` | Username configured on the server. |
| `Password` | Password for the configured user. |

### `QueueCreateOptions`

| Property | Description |
| --- | --- |
| `QueueName` | Required queue name. |
| `IsDurable` | Whether the queue survives server restarts. Defaults to `true`. |
| `LoadBalancingAlgorithm` | How messages are distributed among consumers. Defaults to `RoundRobin`. |
| `Permissions` | User permissions assigned to the queue. |
| `MessageRetention` | Settings for saving messages to disk and cleaning up storage after messages are deleted. |

### `QueueUpdateOptions`

| Property | Description |
| --- | --- |
| `QueueName` | Required queue name. |
| `LoadBalancingAlgorithm` | How messages are distributed among consumers. Defaults to `RoundRobin`. |
| `Permissions` | User permissions assigned to the queue. |
| `MessageRetentionPeriod` | Retention period used for storage cleanup. |

### `MessageRetentionOptions`

| Property | Description |
| --- | --- |
| `Enabled` | Whether messages are saved to disk. Defaults to `false`; message persistence requires a durable queue. |
| `RetentionPeriod` | Age threshold used to clean up storage segments after all their messages have been deleted. Defaults to 10 minutes. This does not expire active messages. |

### `ExchangeCreateOptions`

| Property | Description |
| --- | --- |
| `Name` | Required exchange name. |
| `IsDurable` | Whether the exchange survives server restarts. Defaults to `true`. |
| `Type` | Routing type. Defaults to `Direct`. |
| `Permissions` | User permissions assigned to the exchange. |

### `ExchangeUpdateOptions`

| Property | Description |
| --- | --- |
| `Name` | Required exchange name. |
| `Type` | Routing type. Defaults to `Direct`. |
| `Permissions` | User permissions assigned to the exchange. |

### `BatchSubscribeOptions`

| Property | Description |
| --- | --- |
| `MaxMessages` | Maximum number of messages in a batch. |
| `MaxBytes` | Maximum serialized batch size in bytes. |
| `MaxWaitTime` | Maximum time to wait while forming a batch. |
| `Weight` | Consumer weight used by the server when distributing batches. Defaults to `1`. |

## Compatibility

The client connects to MambaMQ Server over TCP using the MambaMQ protocol. The default server port is `24`.
The package targets .NET 10.