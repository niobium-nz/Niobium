# Cod.Abstractions

Cod.Abstractions provides the foundational interfaces, base types, and contracts for the Cod framework. It enables modular, testable, and extensible business application development by defining the core abstractions used throughout the Cod ecosystem.

## What is this project about?
- Defines essential interfaces (e.g., `IEntity`, `IUserInput`, `IDomainEvent`) and base types for domain-driven design, error handling, and extensibility.
- Used as the lowest-level dependency by all other Cod.* framework projects and consumer projects.
- Ensures a consistent contract for business entities, events, and error payloads.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.Abstractions
```

### 2. Use the Abstractions in Your Code
Implement the interfaces and use the base types in your domain and infrastructure code:

```csharp
using Cod;

public class Invoice : IEntity
{
    public Guid ID { get; set; }
    // ... other properties ...
}

public class MyInput : IUserInput
{
    public void Sanitize() { /* ... */ }
}
```

### 3. Integrate with Other Cod Packages
Cod.Abstractions is automatically referenced by other Cod.* packages. Just add the relevant Cod.* packages to your project and use the shared contracts.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
