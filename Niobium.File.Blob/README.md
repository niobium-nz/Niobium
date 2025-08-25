# Niobium.File.Blob

Niobium.File.Blob provides Azure Blob Storage integration for file storage and retrieval in .NET applications. It enables secure, scalable, and efficient file operations using Azure's cloud storage, and is designed for use with Niobium.File abstractions.

## What is this project about?
- Implements file storage and retrieval using Azure Blob Storage.
- Provides services for uploading, downloading, and managing files in the cloud.
- Integrates with Niobium.File for shared contracts and extensibility.
- Suitable for web, Blazor, and server-side .NET applications needing cloud file storage.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.File.Blob
```

### 2. Register Services in Dependency Injection
In your `Program.cs` or `Startup.cs`:

```csharp
using Niobium.File.Blob;

builder.Services.AddBlobFileStorage(options =>
{
    options.ConnectionString = "<your-azure-blob-connection-string>";
    options.ContainerName = "mycontainer";
});
```

### 3. Use File Storage Services in Your Application

```csharp
using Niobium.File;

public class MyService
{
    private readonly IFileStorage _fileStorage;
    public MyService(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }
    // Use _fileStorage to upload/download files
}
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
