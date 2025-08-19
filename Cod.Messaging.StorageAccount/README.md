# Cod.Messaging.StorageAccount

Cod.Messaging.StorageAccount provides abstractions, dependency injection modules, and helpers for integrating Azure Storage Queues into .NET applications. It enables secure, role-based, and scalable message queuing for event-driven and distributed business workflows.

## What is this project about?

- Implements DI modules for registering Azure Storage Queue brokers and controls.
- Provides options for queue configuration, message encoding, and interactive identity.
- Supplies helpers for SAS token issuance and secure resource control.
- Used by Cod.* and consumer projects to standardize, secure, and extend Azure Storage Queue integration.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:

```
dotnet add package Cod.Messaging.StorageAccount
```

### 2. Register Storage Queue Services

In your DI setup, use the provided dependency module:

```csharp
using Cod.Messaging.StorageAccount;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

services.AddMessaging(
    new StorageQueueOptions
    {
        ServiceEndpoint = "<your-queue-endpoint-or-connection-string>",
        EnableInteractiveIdentity = true,
        Base64MessageEncoding = false
    }
);
```

Or, using an `IConfiguration` section:

```csharp
services.AddMessaging(configuration.GetSection("AzureQueue"));
```

### 3. Use the Messaging Broker

```csharp
using Cod.Messaging;

public class MyService
{
    private readonly IMessagingBroker<MyMessage> _broker;
    public MyService(IMessagingBroker<MyMessage> broker)
    {
        _broker = broker;
    }

    public async Task SendAsync(MyMessage message)
    {
        await _broker.EnqueueAsync(new[] { new MessagingEntry<MyMessage> { Body = message } });
    }

    public async Task<MessagingEntry<MyMessage>?> ReceiveAsync()
    {
        return await _broker.DequeueAsync();
    }
}

public class MyMessage
{
    public string Text { get; set; }
}
```

### 4. SAS Token Issuance

The project provides a signature issuer for generating SAS tokens for secure queue access, supporting role-based and resource-based permissions.

## How Cod.Messaging.StorageAccount is Consumed

Consumer projects use Cod.Messaging.StorageAccount to:

- Register and use Azure Storage Queue brokers via DI.
- Secure access to queues with role-based and personalized entitlements.
- Issue SAS tokens for controlled access to queue resources.
- Integrate queue-based messaging into distributed and event-driven workflows.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
