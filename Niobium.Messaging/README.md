# Niobium.Messaging

Niobium.Messaging provides core abstractions and base types for event-driven and message-based communication in the Niobium ecosystem. It defines contracts for domain events, event handlers, and messaging patterns for business applications.

## What is this project about?
- Defines interfaces and base types for domain events, event handlers, and messaging infrastructure.
- Used as a foundational dependency by Niobium.Channel, Niobium.Channel.Profile, and other Niobium.* packages.
- Enables event-driven architecture and decoupled business logic.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Messaging
```

### 2. Use the Abstractions in Your Code
Implement the interfaces and use the base types in your domain and infrastructure code:

```csharp
using Niobium.Messaging;

public class InvoiceIssued : DomainEvent
{
    public Guid Biller { get; set; }
    public long Invoice { get; set; }
}
```

### 3. Integrate with Other Niobium Packages
Niobium.Messaging is automatically referenced by Niobium.Channel and other Niobium.* packages. Just add the relevant Niobium.* packages to your project and use the shared contracts.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
