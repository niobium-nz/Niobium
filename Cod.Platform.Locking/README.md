# Cod.Platform.Locking

Cod.Platform.Locking provides abstractions, policies, and helpers for distributed locking, impediment management, and resource control in .NET applications. It enables robust, extensible, and testable mechanisms for business process locking, impediment tracking, and policy-driven access control.

## What is this project about?

- Defines interfaces and base types for impediment (lock/block) management.
- Implements distributed locking and impediment policies for business resources.
- Provides extension methods for impediment operations (impede, unimpede, query).
- Integrates with Cod.Platform and Cod.Database.StorageTable for scalable, persistent locking.
- Used by Cod.* and consumer projects to standardize distributed locking and impediment management.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:

```
dotnet add package Cod.Platform.Locking
```

### 2. Register Locking Services

In your DI setup, use the provided dependency module:

```csharp
using Cod.Platform.Locking;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddPlatformLocking();
```

### 3. Implement and Use Impedable Resources

Implement the `IImpedable` interface for your business resource:

```csharp
using Cod.Platform.Locking;

public class MyResource : IImpedable
{
    public string GetImpedementID() => "resource-id";
    public IEnumerable<IImpedimentPolicy> ImpedimentPolicies { get; }
}
```

### 4. Impede and Unimpede Resources

Use the extension methods to impede (lock) or unimpede (unlock) resources:

```csharp
await myResource.ImpedeAsync("CategoryA", 1, "PolicyInput");
await myResource.UnimpedeAsync("CategoryA", 1, "PolicyInput");
```

### 5. Query Impediments

Query impediments (locks) for a resource:

```csharp
await foreach (var impediment in myResource.GetImpedimentsByCategoryAsync("CategoryA"))
{
    // Process impediment
}
```

## How Cod.Platform.Locking is Consumed

Consumer projects use Cod.Platform.Locking to:

- Register and use distributed locking and impediment policies via DI.
- Standardize lock/impediment management for business resources.
- Integrate with persistent storage for scalable, reliable locking.
- Extend or implement custom impediment policies for advanced scenarios.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
