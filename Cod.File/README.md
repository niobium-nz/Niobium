# Cod.File

Cod.File provides core abstractions and interfaces for file storage and management in .NET applications. It enables pluggable file storage backends and is designed for extensibility and integration with cloud and local storage providers.

## What is this project about?
- Defines interfaces and contracts for file storage operations (e.g., `IFileStorage`).
- Enables implementation of custom file storage providers (cloud, local, etc.).
- Used by Cod.File.Blob for Azure Blob Storage integration.
- Suitable for web, Blazor, and server-side .NET applications needing file storage abstraction.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.File
```

### 2. Implement or Use a File Storage Provider
You can implement `IFileStorage` for your own backend, or use a ready-made provider like Cod.File.Blob.

```csharp
using Cod.File;

public class MyFileStorage : IFileStorage
{
    // Implement required methods for file operations
}
```

### 3. Register and Use in Dependency Injection

```csharp
builder.Services.AddSingleton<IFileStorage, MyFileStorage>();
```

Or, if using Cod.File.Blob:

```csharp
using Cod.File.Blob;
builder.Services.AddBlobFileStorage(options => { /* ... */ });
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
