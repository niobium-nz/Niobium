# Niobium.Messaging.ServiceBus

Niobium.Messaging.ServiceBus provides messaging abstractions, queue brokers, and helpers for integrating Azure Service Bus with the Niobium.Messaging event-driven framework. It enables secure, scalable, and flexible message publishing and consumption for distributed .NET applications.

## What is this project about?
- Implements Service Bus queue brokers and message entry types for Niobium.Messaging.
- Provides DI modules and options for configuring Service Bus integration.
- Includes helpers for message conversion, authentication, and string utilities.
- Used by Niobium.* and consumer projects to enable event-driven and message-based workflows over Azure Service Bus.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Messaging.ServiceBus
```

### 2. Register Messaging ServiceBus Dependencies
In your DI setup, use the provided dependency module:

```csharp
using Niobium.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCodMessagingServiceBus(options =>
{
    options.ConnectionString = "<your-service-bus-connection-string>";
    // Configure additional options as needed
});
```

### 3. Use Messaging ServiceBus Brokers
- **Publish/Consume Messages:**
  ```csharp
  var broker = serviceProvider.GetRequiredService<ServiceBusQueueBroker>();
  await broker.PublishAsync(new ServiceBusMessageEntry { ... });
  var messages = await broker.ReceiveAsync("queueName");
  ```
- **Authentication-based Queue Factory:**
  ```csharp
  var factory = serviceProvider.GetRequiredService<AuthenticationBasedQueueFactory>();
  var queue = factory.CreateQueue("queueName", credentials);
  ```

## How Niobium.Messaging.ServiceBus is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Messaging.ServiceBus to:
- Register Service Bus messaging brokers and helpers via DI.
- Publish and consume domain events and messages over Azure Service Bus.
- Integrate with Niobium.Messaging for event-driven workflows.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
