# Niobium.Database.StorageTable

Niobium.Database.StorageTable provides a flexible, high-level abstraction for working with Azure Table Storage in .NET and Blazor applications. It enables secure, efficient, and testable access to table storage with support for dependency injection, caching, and advanced configuration.

## What is this project about?
- Implements repository patterns for Azure Table Storage with async CRUD operations.
- Provides dependency injection modules for easy integration in .NET and Blazor projects.
- Supports memory-cached repositories for performance.
- Integrates with Niobium.Identity for secure, token-based access.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET or Blazor project:

```
dotnet add package Niobium.Database.StorageTable
```

### 2. Register Services in Dependency Injection
Add Niobium.Database.StorageTable to your DI container (typically in `Program.cs`):

```csharp
using Niobium.Database.StorageTable;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDatabase(options =>
{
    options.FullyQualifiedDomainName = "youraccount.table.core.windows.net";
    options.Key = "your-access-key";
    // ...other options...
});
```

### 3. Use the Repository in Your Application
Inject and use the repository in your services or components:

```csharp
@inject IRepository<MyEntity> Repository

var entities = await Repository.GetAsync(100);
```

- Use `AddMemoryCachedRepository<T>()` for in-memory caching.
- Supports advanced scenarios with custom options and authentication.

### 4. Integration with Other Niobium Packages
Niobium.Database.StorageTable is designed to work seamlessly with:
- `Niobium.Identity` for authentication and resource tokens
- All other Niobium.* packages

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
