# Niobium.Channel

Niobium.Channel is the core domain-driven and event-based business logic framework in the Niobium ecosystem. It provides abstractions, base classes, and services for building modular, scalable, and testable business applications in .NET and Blazor.

## What is this project about?
- Implements channel/domain abstractions, event handling, and repository patterns for business logic.
- Enables domain-driven design (DDD) and event-driven architecture (EDA) in your applications.
- Used by consumer projects to rapidly implement business domains, event handlers, and data access.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Channel
```

### 2. Register Services in Dependency Injection
Add Niobium.Channel to your DI container (typically in `Program.cs` or your own module):

```csharp
using Niobium.Channel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddChannel();
```

### 3. Define Your Domain and Events
Implement your domain logic by inheriting from Niobium.Channel base classes:

```csharp
using Niobium.Channel;

public class InvoiceDomain : ChannelDomain<Invoice, InvoiceBuilt>
{
    // Implement domain logic here
}

public class InvoiceBuilt : DomainEvent
{
    public Guid InvoiceId { get; set; }
    // ...
}
```

### 4. Use in Consumer Projects
Reference Niobium.Channel in your consumer project and register your domains and event handlers:

```csharp
services.AddChannelDomain<InvoiceDomain, Invoice>();
services.AddDomainEventHandler<InvoiceBuiltHandler, Invoice>();
```

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
