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
    options.Credentials.ServerKey = Environment.GetEnvironmentVariable("MAMBA_SERVER_KEY")!;
});
```

`ServerKey` must match the key configured on the server. Keep it in a secret store or environment variable rather than source code.

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
| `ServerKey` | Key used to authenticate the service client with the server. |

### `QueueCreateOptions`

| Property | Description |
| --- | --- |
| `QueueName` | Required queue name. |
| `IsDurable` | Whether the queue survives server restarts. Defaults to `true`. |
| `LoadBalancingAlgorithm` | How messages are distributed among consumers. Defaults to `RoundRobin`. |
| `Permissions` | User permissions assigned to the queue. |
| `MessageRetention` | Settings for optional message expiration. |

### `MessageRetentionOptions`

| Property | Description |
| --- | --- |
| `Enabled` | Whether expiration cleanup is enabled. Defaults to `false`. |
| `RetentionPeriod` | How long messages are retained when enabled. Defaults to 10 minutes. |

### `ExchangeCreateOptions`

| Property | Description |
| --- | --- |
| `Name` | Required exchange name. |
| `IsDurable` | Whether the exchange survives server restarts. Defaults to `true`. |
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
