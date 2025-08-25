# Niobium.Platform.ServiceBus

Niobium.Platform.ServiceBus provides abstractions, helpers, and DI modules for integrating Azure Service Bus messaging into .NET applications. It enables secure, role-based, and signature-based message publishing and consumption, making it easy for business apps to leverage distributed messaging patterns.

## What is this project about?
- Supplies extension points and dependency modules for Service Bus queue management and message signature issuance.
- Supports role-based entitlements for sending/receiving messages.
- Provides helpers for working with Azure Service Bus messages and principal parsing.
- Used by Niobium.* and consumer projects to standardize and secure Service Bus integration.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.ServiceBus
```

### 2. Register ServiceBus Dependencies
In your DI setup, use the provided dependency module:

```csharp
using Niobium.Platform.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCodPlatformServiceBus(options =>
{
    options.ConnectionString = "<your-service-bus-connection-string>";
    // Configure additional options as needed
});
```

### 3. Use ServiceBus Helpers
- **Send/Receive Messages:**
  ```csharp
  var queueControl = serviceProvider.GetRequiredService<IDefaultServiceBusQueueControl>();
  await queueControl.SendAsync("queueName", message);
  ```
- **Signature Issuance:**
  ```csharp
  var issuer = serviceProvider.GetRequiredService<IServiceBusSignatureIssuer>();
  var signature = issuer.IssueSignature(message);
  ```
- **Role-based Entitlements:**
  ```csharp
  var descriptor = new RoleBasedSendEntitlementDescriptor("roleName");
  // Use with queue control for access checks
  ```

## How Niobium.Platform.ServiceBus is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Platform.ServiceBus to:
- Register Service Bus dependencies and helpers via DI.
- Securely send and receive messages with role-based access.
- Integrate with distributed messaging infrastructure using standardized patterns.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
